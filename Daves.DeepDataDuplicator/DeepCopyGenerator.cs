using Daves.DeepDataDuplicator.Helpers;
using Daves.DeepDataDuplicator.Metadata;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Daves.DeepDataDuplicator
{
    public class DeepCopyGenerator : RootCopyGenerator
    {
        protected DeepCopyGenerator(
            Catalog catalog,
            Table rootTable,
            string primaryKeyParameterName,
            IReadOnlyDictionary<Column, string> updateParameterNames = null,
            string primaryKeyOutputParameterName = null,
            IReadOnlyList<Column> excludedColumns = null,
            IReadOnlyList<Table> excludedTables = null,
            ReferenceGraph referenceGraph = null)
            : base(catalog, rootTable, primaryKeyParameterName, updateParameterNames, primaryKeyOutputParameterName, excludedColumns, excludedTables, referenceGraph)
        { }

        protected override void GenerateDependentTableCopy(ReferenceGraph.Vertex vertex)
        {
            var table = vertex.Table;
            var dependentReferences = vertex.DependentReferences;
            // Rows needs to be dependent on something copied, so if there's only one dependency we might as well inner join on it.
            bool useLeftJoin = dependentReferences.Count > 1;
            var selectColumnNames = dependentReferences
                .Select((r, i) => $"j{i}.InsertedID j{i}InsertedID");
            // If necessary (it often is), make sure something in the row is dependent on something copied. Faster to
            // or together where-ins than it is to coalesce on all left joined columns and perform a not null check.
            string fromClause = useLeftJoin
? $@"FROM (
            SELECT *
            FROM [{table.Schema}].[{table.Name}]
            WHERE {string.Join($"{Separators.Nlw16} OR ", dependentReferences.Select(r => $"[{r.ParentColumn.Name}] IN (SELECT ExistingID FROM {TableVariableNames[r.ReferencedTable]})"))}
        ) AS copy"
: $@"FROM [{table.Schema.Name}].[{table.Name}] copy";
            var joinClauses = dependentReferences 
                .Select((r, i) => new { r.ParentColumn, r.ReferencedTable, JoinString = $"{(useLeftJoin ? "LEFT " : "")}JOIN" })
                .Select((r, i) => $"{r.JoinString} {TableVariableNames[r.ReferencedTable]} j{i}{Separators.Nlw12}ON copy.[{r.ParentColumn.Name}] = j{i}.ExistingID");
            var dependentInsertColumnNames = dependentReferences
                .Select(r => $"[{r.ParentColumn.Name}]");
            var dependentInsertColumnValues = dependentReferences
                .Select((r, i) => useLeftJoin ? $"COALESCE(j{i}InsertedID, [{r.ParentColumn.Name}])" : $"j{i}InsertedID");
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

        public static new string GenerateProcedure(
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

        public static new string GenerateProcedure(
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

        public static new string GenerateProcedure(
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

        public static new string GenerateProcedureBody(
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

        public static new string GenerateProcedureBody(
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

        public static new string GenerateProcedureBody(
            Catalog catalog,
            Table rootTable,
            string primaryKeyParameterName,
            IReadOnlyDictionary<Column, string> updateParameterNames = null,
            string primaryKeyOutputParameterName = null,
            IReadOnlyList<Column> excludedColumns = null,
            IReadOnlyList<Table> excludedTables = null,
            ReferenceGraph referenceGraph = null)
            => new DeepCopyGenerator(catalog, rootTable, primaryKeyParameterName, updateParameterNames, primaryKeyOutputParameterName, excludedColumns, excludedTables, referenceGraph)
            .ProcedureBody
            .ToString();
    }
}
