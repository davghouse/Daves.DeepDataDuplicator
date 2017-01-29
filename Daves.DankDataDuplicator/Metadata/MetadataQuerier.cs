using Daves.DankDataDuplicator.Helpers;
using System;
using System.Collections.Generic;
using System.Data;

namespace Daves.DankDataDuplicator.Metadata
{
    public class MetadataQuerier
    {
        protected readonly IDbConnection _connection;
        protected readonly IDbTransaction _transaction;

        public MetadataQuerier(IDbConnection connection, IDbTransaction transaction = null)
        {
            _connection = connection;
            _transaction = transaction;
        }

        protected virtual string SchemaQuery =>
@"SELECT
    name name,
    schema_id id
FROM sys.schemas";

        protected virtual string TableQuery =>
@"SELECT
    name name,
    object_id id,
    schema_id schemaId
FROM sys.tables";

        protected virtual string ColumnQuery =>
@"SELECT
    object_id tableId,
    name name,
    column_id columnId,
    is_nullable isNullable,
    is_identity isIdentity,
    is_computed isComputed
FROM sys.columns
WHERE object_id IN (SELECT object_id FROM sys.tables)";

        protected virtual string PrimaryKeyQuery =>
@"SELECT
    object_id tableId,
    name name
FROM sys.indexes
WHERE is_primary_key = 1
    AND object_id IN (SELECT object_id FROM sys.tables)";

        protected virtual string PrimaryKeyColumnQuery =>
@"SELECT
    i.object_id tableId,
    ic.column_id columnId
FROM sys.indexes i
JOIN sys.index_columns ic
    ON i.object_id = ic.object_id
    AND i.index_id = ic.index_id
WHERE i.is_primary_key = 1
    AND i.object_id IN (SELECT object_id FROM sys.tables)";

        protected virtual string ForeignKeyQuery =>
@"SELECT
    name name,
    object_id id,
    parent_object_id parentTableId,
    referenced_object_id referencedTableId,
    is_disabled isDisabled
FROM sys.foreign_keys
WHERE parent_object_id IN (SELECT object_id FROM sys.tables)
    AND referenced_object_id IN (SELECT object_id FROM sys.tables)";

        protected virtual string ForeignKeyColumnQuery =>
@"SELECT
    constraint_object_id foreignKeyId,
    parent_object_id parentTableId,
    parent_column_id parentColumnId,
    referenced_object_id referencedTableId,
    referenced_column_id referencedColumnId
FROM sys.foreign_key_columns
WHERE parent_object_id IN (SELECT object_id FROM sys.tables)
    AND referenced_object_id IN (SELECT object_id FROM sys.tables)";

        protected virtual string CheckConstraintQuery =>
@"SELECT
    name name,
    parent_object_id tableId,
    is_disabled isDisabled,
    CAST(CASE WHEN parent_column_id = 0 THEN 1 ELSE 0 END AS BIT) AS isTableLevel,
    definition definition
FROM sys.check_constraints
WHERE parent_object_id IN (SELECT object_id FROM sys.tables)";

        public virtual IReadOnlyList<Schema> QuerySchemas()
            => Query(SchemaQuery,
                r => new Schema(r["name"], r["id"]))
            .ToReadOnlyList();

        public virtual IReadOnlyList<Table> QueryTables()
            => Query(TableQuery,
                r => new Table(r["name"], r["id"], r["schemaId"]))
            .ToReadOnlyList();

        public virtual IReadOnlyList<Column> QueryColumns()
            => Query(ColumnQuery,
                r => new Column(r["tableId"], r["name"], r["columnId"], r["isNullable"], r["isIdentity"], r["isComputed"]))
            .ToReadOnlyList();

        public virtual IReadOnlyList<PrimaryKey> QueryPrimaryKeys()
            => Query(PrimaryKeyQuery,
                r => new PrimaryKey(r["tableId"], r["name"]))
            .ToReadOnlyList();

        public virtual IReadOnlyList<PrimaryKeyColumn> QueryPrimaryKeyColumns()
            => Query(PrimaryKeyColumnQuery,
                r => new PrimaryKeyColumn(r["tableId"], r["columnId"]))
            .ToReadOnlyList();

        public virtual IReadOnlyList<ForeignKey> QueryForeignKeys()
            => Query(ForeignKeyQuery,
                r => new ForeignKey(r["name"], r["id"], r["parentTableId"], r["referencedTableId"], r["isDisabled"]))
            .ToReadOnlyList();

        public virtual IReadOnlyList<ForeignKeyColumn> QueryForeignKeyColumns()
            => Query(ForeignKeyColumnQuery,
                r => new ForeignKeyColumn(r["foreignKeyId"], r["parentTableId"], r["parentColumnId"], r["referencedTableId"], r["referencedColumnId"]))
            .ToReadOnlyList();

        public virtual IReadOnlyList<CheckConstraint> QueryCheckConstraints()
            => Query(CheckConstraintQuery,
                r => new CheckConstraint(r["name"], r["tableId"], r["isDisabled"], r["isTableLevel"], r["definition"]))
            .ToReadOnlyList();

        protected virtual IEnumerable<T> Query<T>(string query, Func<IDataRecord, T> parse)
        {
            using (IDbCommand command = _connection.CreateCommand())
            {
                command.CommandText = query;
                command.Transaction = _transaction;
                using (IDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return parse((IDataRecord)reader);
                    }
                }
            }
        }
    }
}
