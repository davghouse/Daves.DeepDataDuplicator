using Daves.DankDataDuplicator.Helpers;
using Daves.DankDataDuplicator.Metadata.Queriers;
using System.Collections.Generic;
using System.Data;

namespace Daves.DankDataDuplicator.Metadata
{
    public class Catalog
    {
        protected Catalog()
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

            SetAssociations();
        }

        public static Catalog CreateForSqlServer(IDbConnection connection, IDbTransaction transaction = null)
            => new Catalog(new SqlServerMetadataQuerier(connection, transaction));

        public virtual IReadOnlyList<Schema> Schemas { get; }
        public virtual IReadOnlyList<Table> Tables { get; }
        public virtual IReadOnlyList<Column> Columns { get; }
        public virtual IReadOnlyList<PrimaryKey> PrimaryKeys { get; }
        public virtual IReadOnlyList<PrimaryKeyColumn> PrimaryKeyColumns { get; }
        public virtual IReadOnlyList<ForeignKey> ForeignKeys { get; }
        public virtual IReadOnlyList<ForeignKeyColumn> ForeignKeyColumns { get; }
        public virtual IReadOnlyList<CheckConstraint> CheckConstraints { get; }

        public virtual void SetAssociations()
        {
            Schemas.ForEach(s => s.SetAssociations(Tables));
            Tables.ForEach(t => t.SetAssociations(Schemas, Columns, PrimaryKeys, ForeignKeys, CheckConstraints));
            Columns.ForEach(c => c.SetAssociations(Tables));
            PrimaryKeys.ForEach(k => k.SetAssociations(Tables, PrimaryKeyColumns));
            PrimaryKeyColumns.ForEach(c => c.SetAssociations(PrimaryKeys, Tables, Columns));
            ForeignKeys.ForEach(k => k.SetAssociations(Tables, ForeignKeyColumns));
            ForeignKeyColumns.ForEach(c => c.SetAssociations(ForeignKeys, Tables, Columns));
            CheckConstraints.ForEach(c => c.SetAssociations(Tables));
        }

        public override string ToString()
            => $"{Tables.Count} tables with {Columns.Count} columns";
    }
}
