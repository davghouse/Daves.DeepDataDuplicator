using System.Data;

namespace Daves.DankDataDuplicator.Metadata.Queriers
{
    public class SqlServerMetadataQuerier : MetadataQuerier
    {
        public SqlServerMetadataQuerier(IDbConnection connection, IDbTransaction transaction = null)
            : base(connection, transaction)
        { }

        protected override string SchemaQuery =>
@"SELECT
    name name,
    schema_id id
FROM sys.schemas";

        protected override string TableQuery =>
@"SELECT
    name name,
    object_id id,
    schema_id schemaId
FROM sys.tables";

        protected override string ColumnQuery =>
@"SELECT
    object_id tableId,
    name name,
    column_id columnId,
    is_nullable isNullable,
    is_identity isIdentity,
    is_computed isComputed
FROM sys.columns
WHERE object_id IN (SELECT object_id FROM sys.tables)";

        protected override string PrimaryKeyQuery =>
@"SELECT
    object_id tableId,
    name name
FROM sys.indexes
WHERE is_primary_key = 1
    AND object_id IN (SELECT object_id FROM sys.tables)";

        protected override string PrimaryKeyColumnQuery =>
@"SELECT
    i.object_id tableId,
    ic.column_id columnId
FROM sys.indexes i
JOIN sys.index_columns ic
    ON i.object_id = ic.object_id
    AND i.index_id = ic.index_id
WHERE i.is_primary_key = 1
    AND i.object_id IN (SELECT object_id FROM sys.tables)";

        protected override string ForeignKeyQuery =>
@"SELECT
    name name,
    object_id id,
    parent_object_id parentTableId,
    referenced_object_id referencedTableId,
    is_disabled isDisabled
FROM sys.foreign_keys
WHERE parent_object_id IN (SELECT object_id FROM sys.tables)
    AND referenced_object_id IN (SELECT object_id FROM sys.tables)";

        protected override string ForeignKeyColumnQuery =>
@"SELECT
    constraint_object_id foreignKeyId,
    parent_object_id parentTableId,
    parent_column_id parentColumnId,
    referenced_object_id referencedTableId,
    referenced_column_id referencedColumnId
FROM sys.foreign_key_columns
WHERE parent_object_id IN (SELECT object_id FROM sys.tables)
    AND referenced_object_id IN (SELECT object_id FROM sys.tables)";

        protected override string CheckConstraintQuery =>
@"SELECT
    name name,
    parent_object_id tableId,
    is_disabled isDisabled,
    CAST(CASE WHEN parent_column_id = 0 THEN 1 ELSE 0 END AS BIT) AS isTableLevel,
    definition definition
FROM sys.check_constraints
WHERE parent_object_id IN (SELECT object_id FROM sys.tables)";
    }
}
