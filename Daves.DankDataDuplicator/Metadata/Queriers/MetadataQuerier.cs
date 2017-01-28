using Daves.DankDataDuplicator.Helpers;
using System;
using System.Collections.Generic;
using System.Data;

namespace Daves.DankDataDuplicator.Metadata.Queriers
{
    public abstract class MetadataQuerier
    {
        protected readonly IDbConnection _connection;
        protected readonly IDbTransaction _transaction;

        protected MetadataQuerier()
        { }

        protected MetadataQuerier(IDbConnection connection, IDbTransaction transaction = null)
        {
            _connection = connection;
            _transaction = transaction;
        }

        protected abstract string SchemaQuery { get; }
        protected abstract string TableQuery { get; }
        protected abstract string ColumnQuery { get; }
        protected abstract string PrimaryKeyQuery { get; }
        protected abstract string PrimaryKeyColumnQuery { get; }
        protected abstract string ForeignKeyQuery { get; }
        protected abstract string ForeignKeyColumnQuery { get; }
        protected abstract string CheckConstraintQuery { get; }

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
