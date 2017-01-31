using Daves.DeepDataDuplicator.Helpers;
using Daves.DeepDataDuplicator.Metadata;
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
            string primaryKeyParameterName = null,
            IReadOnlyDictionary<Column, Parameter> updateParameters = null,
            ReferenceGraph referenceGraph = null)
        {
            Catalog = catalog;
            RootTable = rootTable;
            ReferenceGraph = referenceGraph ?? new ReferenceGraph(catalog, rootTable);
            PrimaryKeyParameterName = Parameter.ValidateName(primaryKeyParameterName ?? RootTable.DefaultPrimaryKeyParameterName);
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
        protected StringBuilder ProcedureBody { get; } = new StringBuilder();
        protected IDictionary<Table, string> TableVariableNames { get; } = new Dictionary<Table, string>();

        protected virtual void GenerateTableVariables()
        {
            var relevantTables = ReferenceGraph.Vertices
                .Where(v => v.Table.HasIdentityColumnAsPrimaryKey)
                .Where(v => v.IsReferenced() || v.NonDependentReferences.Any())
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
                // on something copied, so if there's only one dependency we might as well inner join on it regardless.
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
                .Select((r, i) => $"{(r.UseLeftJoin ? "LEFT " : "")}JOIN {TableVariableNames[r.ReferencedTable]} j{i}{Separators.Nlw12}ON copy.[{r.ParentColumn.Name}] = j{i}.ExistingID");
            var dependentInsertColumnNames = dependentReferences
                .Select(r => $"[{r.ParentColumn.Name}]");
            var dependentInsertColumnValues = dependentReferences
                // A potential left join should leave InsertedID null only if the original value was null, since this is a root copy.
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
            // Since this is a root copy, all non-null original values should have corresponding InsertedIDs. It's not necessary
            // to worry about what to do with references (between tables in the subgraph of the database discovered from whatever the
            // root table is) to non-copied data, because there shouldn't be any such references. So if there's only one dependency,
            // inner join, otherwise left join WITH a coalesce--because triggers could've already updated these references underneath us!
            // Added side benefit: coalescing here makes this code exactly what we want in the deep copy case.
            bool useLeftJoin = nonDependentReferences.Count > 1;
            var setStatements = useLeftJoin ? nonDependentReferences
                .Select((r, i) => $"copy.[{r.ParentColumn.Name}] = COALESCE(j{i}.InsertedID, copy.[{r.ParentColumn.Name}])") : nonDependentReferences
                .Select((r, i) => $"copy.[{r.ParentColumn.Name}] = j{i}.InsertedID");
            var joinClauses = nonDependentReferences
                .Select((r, i) => $"{(useLeftJoin ? "LEFT " : "")}JOIN {TableVariableNames[r.ReferencedTable]} j{i}{Separators.Nlw8}ON copy.[{r.ParentColumn.Name}] = j{i}.ExistingID");

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
            primaryKeyParameterName = Parameter.ValidateName(primaryKeyParameterName ?? rootTable.DefaultPrimaryKeyParameterName);
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
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
{GenerateProcedureBody(catalog, rootTable, primaryKeyParameterName, updateParameters, referenceGraph)}
    COMMIT TRAN;
END;";
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
