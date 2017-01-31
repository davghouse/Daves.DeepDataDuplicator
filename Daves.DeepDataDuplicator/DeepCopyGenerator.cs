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
            string primaryKeyParameterName = null,
            IReadOnlyDictionary<Column, Parameter> updateParameters = null,
            ReferenceGraph referenceGraph = null)
            : base(catalog, rootTable, primaryKeyParameterName, updateParameters, referenceGraph: referenceGraph)
        { }

        protected override void GenerateDependentTableCopy(ReferenceGraph.Vertex vertex)
        {
            var table = vertex.Table;
            var dependentReferences = vertex.DependentReferences;
            // Rows needs to be dependent on something copied, so if there's only one dependency we might as well inner join on it.
            bool useLeftJoin = dependentReferences.Count > 1;
            var selectColumnNames = dependentReferences
                .Select((r, i) => $"j{i}.InsertedID j{i}InsertedID");
            // If necessary (it often is), make sure something in the row is dependent on something copied.
            string fromClause = useLeftJoin
? $@"
        FROM (
            SELECT *
            FROM [{table.Schema}].[{table.Name}]
            WHERE {string.Join($"{Separators.Nlw16} OR ", dependentReferences.Select(r => $"[{r.ParentColumn.Name}] IN (SELECT ExistingID FROM {TableVariableNames[r.ReferencedTable]})"))}
        ) AS copy"
: $@"
        FROM [{table.Schema.Name}].[{table.Name}] copy";
            var joinClauses = dependentReferences 
                .Select((r, i) => new { r.ParentColumn, r.ReferencedTable, JoinString = $"{(useLeftJoin ? "LEFT " : "")}JOIN" })
                .Select((r, i) => $"{r.JoinString} {TableVariableNames[r.ReferencedTable]} j{i}{Separators.Nlw12}ON copy.[{r.ParentColumn.Name}] = j{i}.ExistingID");
            var dependentInsertColumnNames = dependentReferences
                .Select(r => $"[{r.ParentColumn.Name}]");
            var dependentInsertColumnValues = dependentReferences
                .Select((r, i) => useLeftJoin ? $"COALESCE(j{i}InsertedID, [{r.ParentColumn.Name}])" : $"j{i}InsertedID");
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

        public static new string GenerateProcedure(
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

        public static new string GenerateProcedure(
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

        public static new string GenerateProcedureBody(
            IDbConnection connection,
            string rootTableName,
            IDbTransaction transaction = null,
            string primaryKeyParameterName = null,
            string schemaName = null)
        {
            var catalog = new Catalog(connection, transaction);

            return GenerateProcedureBody(catalog, catalog.FindTable(rootTableName, schemaName), primaryKeyParameterName);
        }

        public static new string GenerateProcedureBody(
            Catalog catalog,
            Table rootTable,
            string primaryKeyParameterName = null,
            IReadOnlyDictionary<Column, Parameter> updateParameters = null,
            ReferenceGraph referenceGraph = null)
            => new DeepCopyGenerator(catalog, rootTable, primaryKeyParameterName, updateParameters, referenceGraph).ProcedureBody.ToString();
    }
}
