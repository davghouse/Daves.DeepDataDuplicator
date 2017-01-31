using Daves.DeepDataDuplicator.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DeepDataDuplicator.Metadata
{
    public class Table
    {
        public Table(object name, object id, object schemaId)
            : this((string)name, (int)id, (int)schemaId)
        { }

        public Table(string name, int id, int schemaId)
        {
            Name = name;
            Id = id;
            SchemaId = schemaId;
        }

        public string Name { get; }
        public int Id { get; }
        public int SchemaId { get; }
        public Schema Schema { get; protected set; }
        public IReadOnlyList<Column> Columns { get; protected set; }
        public PrimaryKey PrimaryKey { get; protected set; }
        public IReadOnlyList<ForeignKey> ChildForeignKeys { get; protected set; }
        public IReadOnlyList<ForeignKey> ReferencingForeignKeys { get; protected set; }
        public IReadOnlyList<CheckConstraint> CheckConstraints { get; protected set; }

        public virtual void Initialize(
            IReadOnlyList<Schema> schemas,
            IReadOnlyList<Column> columns,
            IReadOnlyList<PrimaryKey> primaryKeys,
            IReadOnlyList<ForeignKey> foreignKeys,
            IReadOnlyList<CheckConstraint> checkConstraints)
        {
            Schema = schemas.Single(s => s.Id == SchemaId);
            Columns = columns
                .Where(c => c.TableId == Id)
                .ToReadOnlyList();
            PrimaryKey = primaryKeys.SingleOrDefault(k => k.TableId == Id);
            ChildForeignKeys = foreignKeys
                .Where(k => k.ParentTableId == Id)
                .ToReadOnlyList();
            ReferencingForeignKeys = foreignKeys
                .Where(k => k.ReferencedTableId == Id)
                .ToReadOnlyList();
            CheckConstraints = checkConstraints
                .Where(c => c.TableId == Id)
                .ToReadOnlyList();
        }

        public virtual string SingularSpacelessName
            => Name.ToSingularSpacelessName();

        public virtual bool HasIdentityColumnAsPrimaryKey
            => PrimaryKey?.Column?.IsIdentity ?? false;

        public virtual string DefaultPrimaryKeyParameterName
            => $"@{PrimaryKey?.Column?.LowercaseSpacelessName}";

        public virtual Column FindColumn(string columnName)
            => Columns.Single(c => c.Name == columnName);

        public override string ToString()
            => $"{Schema}.{Name}";
    }
}
