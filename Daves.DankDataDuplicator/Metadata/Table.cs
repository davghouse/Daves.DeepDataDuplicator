using Daves.DankDataDuplicator.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class Table
    {
        protected Table()
        { }

        public Table(object name, object id, object schemaId)
            : this((string)name, (int)id, (int)schemaId)
        { }

        public Table(string name, int id, int schemaId)
        {
            Name = name;
            Id = id;
            SchemaId = schemaId;
        }

        public virtual string Name { get; }
        public virtual int Id { get; }
        public virtual int SchemaId { get; }
        public virtual Schema Schema { get; protected set; }
        public virtual IReadOnlyList<Column> Columns { get; protected set; }
        public virtual PrimaryKey PrimaryKey { get; protected set; }
        public virtual IReadOnlyList<ForeignKey> ChildForeignKeys { get; protected set; }
        public virtual IReadOnlyList<ForeignKey> ReferencingForeignKeys { get; protected set; }
        public virtual IReadOnlyList<CheckConstraint> CheckConstraints { get; protected set; }

        public virtual void SetAssociations(
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

        public override string ToString()
            => $"{Schema}.{Name}";
    }
}
