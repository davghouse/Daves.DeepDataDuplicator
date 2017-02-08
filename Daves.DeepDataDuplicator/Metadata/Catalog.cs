using Daves.DeepDataDuplicator.Helpers;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Daves.DeepDataDuplicator.Metadata
{
    public class Catalog
    {
        public Catalog(IDbConnection connection, IDbTransaction transaction = null)
            : this(new MetadataQuerier(connection, transaction))
        { }

        public Catalog(MetadataQuerier metadataQuerier)
            : this(metadataQuerier.QuerySchemas(),
                metadataQuerier.QueryTables(),
                metadataQuerier.QueryColumns(),
                metadataQuerier.QueryPrimaryKeys(),
                metadataQuerier.QueryPrimaryKeyColumns(),
                metadataQuerier.QueryForeignKeys(),
                metadataQuerier.QueryForeignKeyColumns(),
                metadataQuerier.QueryCheckConstraints())
        { }

        public Catalog(
            IReadOnlyList<Schema> schemas,
            IReadOnlyList<Table> tables,
            IReadOnlyList<Column> columns,
            IReadOnlyList<PrimaryKey> primaryKeys,
            IReadOnlyList<PrimaryKeyColumn> primaryKeyColumns,
            IReadOnlyList<ForeignKey> foreignKeys,
            IReadOnlyList<ForeignKeyColumn> foreignKeyColumns,
            IReadOnlyList<CheckConstraint> checkConstraints)
        {
            Schemas = schemas;
            Tables = tables;
            Columns = columns;
            PrimaryKeys = primaryKeys;
            PrimaryKeyColumns = primaryKeyColumns;
            ForeignKeys = foreignKeys;
            ForeignKeyColumns = foreignKeyColumns;
            CheckConstraints = checkConstraints;

            Initialize();
        }

        public IReadOnlyList<Schema> Schemas { get; }
        public IReadOnlyList<Table> Tables { get; }
        public IReadOnlyList<Column> Columns { get; }
        public IReadOnlyList<PrimaryKey> PrimaryKeys { get; }
        public IReadOnlyList<PrimaryKeyColumn> PrimaryKeyColumns { get; }
        public IReadOnlyList<ForeignKey> ForeignKeys { get; }
        public IReadOnlyList<ForeignKeyColumn> ForeignKeyColumns { get; }
        public IReadOnlyList<CheckConstraint> CheckConstraints { get; }

        public virtual void Initialize()
        {
            Schemas.ForEach(s => s.Initialize(Tables));
            Tables.ForEach(t => t.Initialize(Schemas, Columns, PrimaryKeys, ForeignKeys, CheckConstraints));
            Columns.ForEach(c => c.Initialize(Tables));
            PrimaryKeys.ForEach(k => k.Initialize(Tables, PrimaryKeyColumns));
            PrimaryKeyColumns.ForEach(c => c.Initialize(PrimaryKeys, Tables, Columns));
            ForeignKeys.ForEach(k => k.Initialize(Tables, ForeignKeyColumns));
            ForeignKeyColumns.ForEach(c => c.Initialize(ForeignKeys, Tables, Columns));
            CheckConstraints.ForEach(c => c.Initialize(Tables));
        }

        public Table FindTable(string tableName, string tableSchemaName = null)
            => Tables
            .Where(t => tableSchemaName == null || t.Schema.Name == tableSchemaName)
            .Single(t => t.Name == tableName);

        public Column FindColumn(string tableName, string columnName, string tableSchemaName = null)
            => FindTable(tableName, tableSchemaName)
            .FindColumn(columnName);

        public override string ToString()
            => $"{Tables.Count} tables with {Columns.Count} columns";
    }
}
