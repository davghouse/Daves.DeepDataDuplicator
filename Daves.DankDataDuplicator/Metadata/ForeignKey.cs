using Daves.DankDataDuplicator.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class ForeignKey
    {
        protected ForeignKey()
        { }

        public ForeignKey(object name, object id, object parentTableId, object referencedTableId, object isDisabled)
            : this((string)name, (int)id, (int)parentTableId, (int)referencedTableId, (bool)isDisabled)
        { }

        public ForeignKey(string name, int id, int parentTableId, int referencedTableId, bool isDisabled)
        {
            Name = name;
            Id = id;
            ParentTableId = parentTableId;
            ReferencedTableId = referencedTableId;
            IsDisabled = isDisabled;
        }

        public virtual string Name { get; }
        public virtual int Id { get; }
        public virtual int ParentTableId { get; }
        public virtual int ReferencedTableId { get; }
        public virtual bool IsDisabled { get; }
        public virtual Table ParentTable { get; protected set; }
        public virtual Table ReferencedTable { get; protected set; }
        public virtual IReadOnlyList<ForeignKeyColumn> ForeignKeyColumns { get; protected set; }

        public virtual void SetAssociations(IReadOnlyList<Table> tables, IReadOnlyList<ForeignKeyColumn> foreignKeyColumns)
        {
            ParentTable = tables.Single(t => t.Id == ParentTableId);
            ReferencedTable = tables.Single(t => t.Id == ReferencedTableId);
            ForeignKeyColumns = foreignKeyColumns
                .Where(fkc => fkc.ForeignKeyId == Id)
                .ToReadOnlyList();
        }

        public override string ToString()
            => $"{ParentTable} to {ReferencedTable}: {Name}";
    }
}
