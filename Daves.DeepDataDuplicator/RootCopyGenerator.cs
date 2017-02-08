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
            string primaryKeyParameterName,
            IReadOnlyDictionary<Column, string> updateParameterNames = null,
            string primaryKeyOutputParameterName = null,
            IReadOnlyList<Column> excludedColumns = null,
            IReadOnlyList<Table> excludedTables = null,
            ReferenceGraph referenceGraph = null)
        {
            Catalog = catalog;
            RootTable = rootTable;
            PrimaryKeyParameterName = Parameter.ValidateName(primaryKeyParameterName ?? RootTable.DefaultPrimaryKeyParameterName);
            UpdateParameterNames = updateParameterNames?.ToDictionary(kvp => kvp.Key, kvp => Parameter.ValidateName(kvp.Value)) ?? new Dictionary<Column, string>();
            PrimaryKeyOutputParameterName = Parameter.ValidateName(primaryKeyOutputParameterName);
            ExcludedColumns = excludedColumns ?? new Column[0];
            ReferenceGraph = referenceGraph ?? new ReferenceGraph(catalog, rootTable, excludedTables);

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
        protected string PrimaryKeyParameterName { get; }
        protected IReadOnlyDictionary<Column, string> UpdateParameterNames { get; }
        protected string PrimaryKeyOutputParameterName { get; }
        protected IReadOnlyList<Column> ExcludedColumns { get; }
        protected ReferenceGraph ReferenceGraph { get; }
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
                .Where(c => c.IsCopyable)
                .Where(c => !ExcludedColumns.Contains(c));
            var insertColumnNames = insertColumns
                .Select(c => $"[{c.Name}]");
            var insertColumnValues = insertColumns
                .Select(c => UpdateParameterNames.ContainsKey(c) ? UpdateParameterNames[c] : $"Source.[{c.Name}]");
            string insertClause = !insertColumns.Any()
? @"INSERT DEFAULT VALUES"
: $@"INSERT (
        {string.Join(Separators.Cnlw8, insertColumnNames)})
    VALUES (
        {string.Join(Separators.Cnlw8, insertColumnValues)})";
            string setStatement = PrimaryKeyOutputParameterName != null
? $@"
    SET {PrimaryKeyOutputParameterName} = SCOPE_IDENTITY();"
: "";

            ProcedureBody.AppendLine($@"
    MERGE INTO [{RootTable.Schema.Name}].[{RootTable.Name}] AS Target
    USING (
        SELECT *
        FROM [{RootTable.Schema.Name}].[{RootTable.Name}]
        WHERE [{RootTable.PrimaryKey.Column.Name}] = {PrimaryKeyParameterName}
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    {insertClause}{GenerateOutputClause(RootTable)}{setStatement}");
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
            // If necessary (it usually isn't), make sure something in the row is dependent on something copied. Faster to
            // or together where-ins than it is to coalesce on all left joined columns and perform a not null check.
            string fromClause = dependentReferences.All(r => r.UseLeftJoin)
? $@"FROM (
            SELECT *
            FROM [{table.Schema}].[{table.Name}]
            WHERE {string.Join($"{Separators.Nlw16} OR ", dependentReferences.Select(r => $"[{r.ParentColumn.Name}] IN (SELECT ExistingID FROM {TableVariableNames[r.ReferencedTable]})"))}
        ) AS copy"
: $@"FROM [{table.Schema.Name}].[{table.Name}] copy";
            var joinClauses = dependentReferences
                .Select((r, i) => $"{(r.UseLeftJoin ? "LEFT " : "")}JOIN {TableVariableNames[r.ReferencedTable]} j{i}{Separators.Nlw12}ON copy.[{r.ParentColumn.Name}] = j{i}.ExistingID");
            var dependentInsertColumnNames = dependentReferences
                .Select(r => $"[{r.ParentColumn.Name}]");
            var dependentInsertColumnValues = dependentReferences
                // A potential left join should leave InsertedID null only if the original value was null, since this is a root copy.
                .Select((r, i) => $"j{i}InsertedID");
            var nonDependentInsertColumns = table.Columns
                .Where(c => c.IsCopyable)
                .Where(c => !ExcludedColumns.Contains(c))
                .Where(c => !dependentReferences.Select(r => r.ParentColumn).Contains(c));
            var nonDependentInsertColumnNames = nonDependentInsertColumns
                .Select(c => $"[{c.Name}]");
            var nonDependentInsertColumnValues = nonDependentInsertColumns
                .Select(c => UpdateParameterNames.ContainsKey(c) ? UpdateParameterNames[c] : $"Source.[{c.Name}]");

            ProcedureBody.AppendLine($@"
    MERGE INTO [{table.Schema.Name}].[{table.Name}] AS Target
    USING (
        SELECT
            copy.*,
            {string.Join(Separators.Cnlw12, selectColumnNames)}
        {fromClause}
        {string.Join(Separators.Nlw8, joinClauses)}
    ) AS Source
    ON 1 = 0
    WHEN NOT MATCHED BY TARGET THEN
    INSERT (
        {string.Join(Separators.Cnlw8, dependentInsertColumnNames.Concat(nonDependentInsertColumnNames))})
    VALUES (
        {string.Join(Separators.Cnlw8, dependentInsertColumnValues.Concat(nonDependentInsertColumnValues))}){GenerateOutputClause(table)}");
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

        protected virtual string GenerateOutputClause(Table table)
            => TableVariableNames.ContainsKey(table)
? $@"
    OUTPUT Source.[{table.PrimaryKey.Column.Name}], Inserted.[{table.PrimaryKey.Column.Name}]
    INTO {TableVariableNames[table]};"
: ";";

        public static string GenerateProcedure(
            IDbConnection connection,
            string rootTableName,
            string procedureName = null,
            string primaryKeyParameterName = null,
            string primaryKeyOutputParameterName = null,
            string rootTableSchemaName = null)
        {
            var catalog = new Catalog(connection);
            var rootTable = catalog.FindTable(rootTableName, rootTableSchemaName);

            return GenerateProcedure(catalog, rootTable, procedureName, primaryKeyParameterName);
        }

        public static string GenerateProcedure(
            IDbConnection connection,
            IDbTransaction transaction,
            string rootTableName,
            string procedureName = null,
            string primaryKeyParameterName = null,
            string primaryKeyOutputParameterName = null,
            string rootTableSchemaName = null)
        {
            var catalog = new Catalog(connection, transaction);
            var rootTable = catalog.FindTable(rootTableName, rootTableSchemaName);

            return GenerateProcedure(catalog, rootTable, procedureName, primaryKeyParameterName);
        }

        public static string GenerateProcedure(
            Catalog catalog,
            Table rootTable,
            string procedureName = null,
            string primaryKeyParameterName = null,
            IReadOnlyDictionary<Column, Parameter> updateParameters = null,
            string primaryKeyOutputParameterName = null,
            IReadOnlyList<Column> excludedColumns = null,
            IReadOnlyList<Table> excludedTables = null,
            ReferenceGraph referenceGraph = null)
        {
            procedureName = procedureName ?? $"Copy{rootTable.SingularSpacelessName}";
            primaryKeyParameterName = primaryKeyParameterName ?? rootTable.DefaultPrimaryKeyParameterName;
            var parameters = (updateParameters ?? new Dictionary<Column, Parameter>())
                .Select(kvp => kvp.Value)
                .Prepend(new Parameter(primaryKeyParameterName, "INT"));
            parameters = primaryKeyOutputParameterName == null ? parameters
                : parameters.Append(new Parameter(primaryKeyOutputParameterName, "INT = NULL OUTPUT"));
            var updateParameterNames = updateParameters?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Name);

            return
$@"CREATE PROCEDURE [{rootTable.Schema.Name}].[{procedureName}]
    {string.Join(Separators.Cnlw4, parameters.Select(p => $"{p.Name} {p.DataTypeDescription}"))}
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRAN;
{GenerateProcedureBody(catalog, rootTable, primaryKeyParameterName, updateParameterNames, primaryKeyOutputParameterName, excludedColumns, excludedTables, referenceGraph)}
    COMMIT TRAN;
END;";
        }

        public static string GenerateProcedureBody(
            IDbConnection connection,
            string rootTableName,
            string primaryKeyParameterName,
            string primaryKeyOutputParameterName = null,
            string rootTableSchemaName = null)
        {
            var catalog = new Catalog(connection);
            var rootTable = catalog.FindTable(rootTableName, rootTableSchemaName);

            return GenerateProcedureBody(catalog, rootTable, primaryKeyParameterName, null, primaryKeyOutputParameterName);
        }

        public static string GenerateProcedureBody(
            IDbConnection connection,
            IDbTransaction transaction,
            string rootTableName,
            string primaryKeyParameterName,
            string primaryKeyOutputParameterName = null,
            string rootTableSchemaName = null)
        {
            var catalog = new Catalog(connection, transaction);
            var rootTable = catalog.FindTable(rootTableName, rootTableSchemaName);

            return GenerateProcedureBody(catalog, rootTable, primaryKeyParameterName, null, primaryKeyOutputParameterName);
        }

        public static string GenerateProcedureBody(
            Catalog catalog,
            Table rootTable,
            string primaryKeyParameterName,
            IReadOnlyDictionary<Column, string> updateParameterNames = null,
            string primaryKeyOutputParameterName = null,
            IReadOnlyList<Column> excludedColumns = null,
            IReadOnlyList<Table> excludedTables = null,
            ReferenceGraph referenceGraph = null)
            => new RootCopyGenerator(catalog, rootTable, primaryKeyParameterName, updateParameterNames, primaryKeyOutputParameterName, excludedColumns, excludedTables, referenceGraph)
            .ProcedureBody
            .ToString();
    }
}
