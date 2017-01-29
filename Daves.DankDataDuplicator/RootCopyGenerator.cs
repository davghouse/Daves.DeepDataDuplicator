using Daves.DankDataDuplicator.Metadata;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Daves.DankDataDuplicator
{
    public class RootCopyGenerator
    {
        private readonly static string nlw4 = $"{Environment.NewLine}    ";
        private readonly static string nlw8 = $"{Environment.NewLine}        ";
        private readonly static string nlw12 = $"{Environment.NewLine}            ";
        private readonly static string nlw16 = $"{Environment.NewLine}                ";
        private readonly static string cnlw4 = $",{Environment.NewLine}    ";
        private readonly static string cnlw8 = $",{Environment.NewLine}        ";
        private readonly static string cnlw12 = $",{Environment.NewLine}            ";

        protected RootCopyGenerator(Catalog catalog, Table rootTable, string primaryKeyParameterName,
            IReadOnlyDictionary<Column, Parameter> updateParameters = null)
        {
            ReferenceGraph = new ReferenceGraph(catalog, rootTable);
        }

        protected ReferenceGraph ReferenceGraph { get; }
        protected string PrimaryKeyParameterName { get; }
        protected StringBuilder ProcedureBody { get; } = new StringBuilder();

        public static string GenerateProcedure(IDbConnection connection, string rootTableName,
            IDbTransaction transaction = null, string procedureName = null, string primaryKeyParameterName = null, string schemaName = null)
        {
            var catalog = new Catalog(connection, transaction);

            return GenerateProcedure(catalog, catalog.FindTable(rootTableName, schemaName), procedureName, primaryKeyParameterName);
        }

        public static string GenerateProcedure(Catalog catalog, Table rootTable,
            string procedureName = null, string primaryKeyParameterName = null, IReadOnlyDictionary<Column, Parameter> updateParameters = null)
        {
            procedureName = procedureName ?? $"Copy{rootTable.SingularSpacelessName}";
            primaryKeyParameterName = primaryKeyParameterName ?? $"@{rootTable.PrimaryKey.Column.LowercaseSpacelessName}";
            string parameterDefinitions = !updateParameters?.Any() ?? true
? $@"
    {primaryKeyParameterName} INT"
: $@"
    {primaryKeyParameterName} INT,
    {string.Join(_cnlw4, updateParameters.Select(p => $"{p.Value.Name} {p.Value.DataTypeName}"))}";

            return
$@"CREATE PROCEDURE [{rootTable.Schema}].[{procedureName}]{parameterDefinitions}
AS
BEGIN
{GenerateProcedureBody(catalog, rootTable, primaryKeyParameterName, updateParameters)}
END;";
        }

        public static string GenerateProcedureBody(IDbConnection connection, string rootTableName,
            IDbTransaction transaction = null, string primaryKeyParameterName = null, string schemaName = null)
        {
            var catalog = new Catalog(connection, transaction);

            return GenerateProcedureBody(catalog, catalog.FindTable(rootTableName, schemaName), primaryKeyParameterName);
        }

        public static string GenerateProcedureBody(Catalog catalog, Table rootTable,
            string primaryKeyParameterName = null, IReadOnlyDictionary<Column, Parameter> updateParameters = null)
            => new RootCopyGenerator(catalog, rootTable, primaryKeyParameterName, updateParameters).ProcedureBody.ToString();
    }
}
