using Daves.DeepDataDuplicator.Helpers;
using Daves.DeepDataDuplicator.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Daves.DeepDataDuplicator
{
    public class RootCopyGenerator
    {
        protected RootCopyGenerator(
            Catalog catalog,
            Table rootTable,
            string primaryKeyParameterName,
            IReadOnlyDictionary<Column, Parameter> updateParameters = null,
            ReferenceGraph referenceGraph = null)
        {
            Catalog = catalog;
            RootTable = rootTable;
            ReferenceGraph = referenceGraph ?? new ReferenceGraph(catalog, rootTable);
            PrimaryKeyParameterName = primaryKeyParameterName;
            UpdateParameters = updateParameters ?? new Dictionary<Column, Parameter>();

            GenerateTableVariables();
            GenerateRootTableCopy();

            foreach (var vertex in ReferenceGraph.Skip(1))
            {
                GenerateDependentTableCopy(vertex);
            }

            foreach (var vertex in ReferenceGraph
                .Where(v => v.NonDependentReferences.Any()))
            {
                GenerateNonDependentReferenceUpdates(vertex);
            }
        }

        protected Catalog Catalog { get; }
        protected Table RootTable { get; }
        protected ReferenceGraph ReferenceGraph { get; }
        protected string PrimaryKeyParameterName { get; }
        protected IReadOnlyDictionary<Column, Parameter> UpdateParameters { get; }
        protected StringBuilder ProcedureBody { get; } = new StringBuilder($"    SET NOCOUNT ON;{Environment.NewLine}");
        protected IDictionary<Table, string> TableVariableNames { get; } = new Dictionary<Table, string>();

        protected virtual void GenerateTableVariables()
        {
            var relevantTables = ReferenceGraph.Vertices
                .Where(v => v.Table.HasIdentityColumnAsPrimaryKey)
                .Where(v => v.IsReferenced())
                .Select(v => v.Table);
            bool tableNamesAreDistinct = relevantTables
                .Select(t => t.Name)
                .Distinct()
                .Count() == relevantTables.Count();
            foreach (var table in relevantTables)
            {
                string tableVariableName = $"@{(tableNamesAreDistinct ? "" : table.Schema.SpacelessName)}{table.SingularSpacelessName}IDPairs";
                TableVariableNames.Add(table, tableVariableName);

                ProcedureBody.AppendLine($@"
    DECLARE {tableVariableName} TABLE (
        ExistingID INT NOT NULL UNIQUE,
        InsertedID INT NOT NULL UNIQUE
    );");
            }
        }

        protected virtual void GenerateRootTableCopy()
        {
            var insertColumns = RootTable.Columns
                .Where(c => c.IsCopyable);
            var insertColumnNames = insertColumns
                .Select(c => $"[{c.Name}]");
            var insertColumnValues = insertColumns
                .Select(c => UpdateParameters.ContainsKey(c) ? UpdateParameters[c].Name : $"Source.[{c.Name}]");
            string insertString = !insertColumns.Any()
? @"
    INSERT DEFAULT VALUES"
: $@"
    INSERT (
        {string.Join(Separators.Cnlw8, insertColumnNames)})
    VALUES (
        {string.Join(Separators.Cnlw8, insertColumnValues)})";

            ProcedureBody.AppendLine($@"
    MERGE INTO [{RootTable.Schema.Name}].[{RootTable.Name}] AS Target
    USING (
        SELECT *
        FROM [{RootTable.Schema.Name}].[{RootTable.Name}]
        WHERE [{RootTable.PrimaryKey.Column.Name}] = {PrimaryKeyParameterName}
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN{insertString}{GenerateOutputString(RootTable)}");
        }

        protected virtual void GenerateDependentTableCopy(ReferenceGraph.Vertex vertex)
        {
            var table = vertex.Table;
            var dependentReferences = vertex.DependentReferences
                // Check constraints can lead to nullable dependent reference columns. However, rows needs to be dependent
                // on something copied, so if there's only one dependency we might as well inner join on it even if nullable.
                .Select(r => new { r.ParentColumn, r.ReferencedTable, UseLeftJoin = r.ParentColumn.IsNullable && vertex.DependentReferences.Count > 1 })
                .ToReadOnlyList();
            var selectColumnNames = dependentReferences
                .Select((r, i) => $"j{i}.InsertedID j{i}InsertedID");
            // If necessary (it usually isn't), make sure something in the row is dependent on something copied.
            string fromClause = dependentReferences.All(r => r.UseLeftJoin)
? $@"
        FROM (
            SELECT *
            FROM [{table.Schema}].[{table.Name}]
            WHERE {string.Join($"{Separators.Nlw16} OR ", dependentReferences.Select(r => $"[{r.ParentColumn.Name}] IN (SELECT ExistingID FROM {TableVariableNames[r.ReferencedTable]})"))}
        ) AS copy"
: $@"
        FROM [{table.Schema.Name}].[{table.Name}] copy";
            var joinClauses = dependentReferences 
                .Select((r, i) => new { r.ParentColumn, r.ReferencedTable, JoinString = $"{(r.UseLeftJoin ? "LEFT " : "")}JOIN" })
                .Select((r, i) => $"{r.JoinString} {TableVariableNames[r.ReferencedTable]} j{i}{Separators.Nlw12}ON copy.[{r.ParentColumn.Name}] = j{i}.ExistingID");
            var dependentInsertColumnNames = dependentReferences
                .Select(r => $"[{r.ParentColumn.Name}]");
            var dependentInsertColumnValues = dependentReferences
                // The left join should leave InsertedID null only if the original value was null, since this is a root copy.
                .Select((r, i) => $"j{i}InsertedID");
            var nonDependentInsertColumns = table.Columns
                .Where(c => c.IsCopyable)
                .Where(c => !dependentReferences.Select(r => r.ParentColumn).Contains(c));
            var nonDependentInsertColumnNames = nonDependentInsertColumns
                .Select(c => $"[{c.Name}]");
            var nonDependentInsertColumnValues = nonDependentInsertColumns
                .Select(c => UpdateParameters.ContainsKey(c) ? UpdateParameters[c].Name : $"Source.[{c.Name}]");

            ProcedureBody.AppendLine($@"
    MERGE INTO [{table.Schema.Name}].[{table.Name}] AS Target
    USING (
        SELECT
            copy.*,
            {string.Join(Separators.Cnlw12, selectColumnNames)}{fromClause}
        {string.Join(Separators.Nlw8, joinClauses)}
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        {string.Join(Separators.Cnlw8, dependentInsertColumnNames.Concat(nonDependentInsertColumnNames))})
    VALUES (
        {string.Join(Separators.Cnlw8, dependentInsertColumnValues.Concat(nonDependentInsertColumnValues))}){GenerateOutputString(table)}");
        }

        protected virtual void GenerateNonDependentReferenceUpdates(ReferenceGraph.Vertex vertex)
        {
            var table = vertex.Table;
            var nonDependentReferences = vertex.NonDependentReferences;
            var setStatements = nonDependentReferences
                // No coalescing as the left join should leave InsertedID null only if the original value was null, since this is a root copy.
                .Select((r, i) => $"copy.[{r.ParentColumn.Name}] = j{i}.InsertedID");
            var joinClauses = nonDependentReferences
                .Select((r, i) => $"LEFT JOIN {TableVariableNames[r.ReferencedTable]} j{i}{Separators.Nlw8}ON copy.[{r.ParentColumn.Name}] = j{i}.ExistingID");

            ProcedureBody.AppendLine($@"
    UPDATE copy
    SET
        {string.Join(Separators.Cnlw8, setStatements)}
    FROM [{table.Schema.Name}].[{table.Name}] copy
    {string.Join(Separators.Nlw4, joinClauses)}
    WHERE copy.[{table.PrimaryKey.Column.Name}] IN (SELECT InsertedID FROM {TableVariableNames[table]});");
        }

        protected virtual string GenerateOutputString(Table table)
            => TableVariableNames.ContainsKey(table)
? $@"
    OUTPUT Source.[{table.PrimaryKey.Column.Name}], Inserted.[{table.PrimaryKey.Column.Name}]
    INTO {TableVariableNames[table]};"
: ";";

        public static string GenerateProcedure(
            IDbConnection connection,
            string rootTableName,
            IDbTransaction transaction = null,
            string procedureName = null,
            string primaryKeyParameterName = null,
            string schemaName = null)
        {
            var catalog = new Catalog(connection, transaction);

            return GenerateProcedure(catalog, catalog.FindTable(rootTableName, schemaName), procedureName, primaryKeyParameterName);
        }

        public static string GenerateProcedure(
            Catalog catalog,
            Table rootTable,
            string procedureName = null,
            string primaryKeyParameterName = null,
            IReadOnlyDictionary<Column, Parameter> updateParameters = null,
            ReferenceGraph referenceGraph = null)
        {
            procedureName = procedureName ?? $"Copy{rootTable.SingularSpacelessName}";
            primaryKeyParameterName = primaryKeyParameterName ?? $"@{rootTable.PrimaryKey.Column.LowercaseSpacelessName}";
            string parameterDefinitions = !updateParameters?.Any() ?? true
? $@"
    {primaryKeyParameterName} INT"
: $@"
    {primaryKeyParameterName} INT,
    {string.Join(Separators.Cnlw4, updateParameters.Select(p => $"{p.Value.Name} {p.Value.DataTypeName}"))}";

            return
$@"CREATE PROCEDURE [{rootTable.Schema.Name}].[{procedureName}]{parameterDefinitions}
AS
BEGIN
{GenerateProcedureBody(catalog, rootTable, primaryKeyParameterName, updateParameters, referenceGraph)}END;";
        }

        public static string GenerateProcedureBody(
            IDbConnection connection,
            string rootTableName,
            IDbTransaction transaction = null,
            string primaryKeyParameterName = null,
            string schemaName = null)
        {
            var catalog = new Catalog(connection, transaction);

            return GenerateProcedureBody(catalog, catalog.FindTable(rootTableName, schemaName), primaryKeyParameterName);
        }

        public static string GenerateProcedureBody(
            Catalog catalog,
            Table rootTable,
            string primaryKeyParameterName = null,
            IReadOnlyDictionary<Column, Parameter> updateParameters = null,
            ReferenceGraph referenceGraph = null)
            => new RootCopyGenerator(catalog, rootTable, primaryKeyParameterName, updateParameters, referenceGraph).ProcedureBody.ToString();
    }
}
