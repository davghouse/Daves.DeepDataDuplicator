using Daves.DeepDataDuplicator.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DeepDataDuplicator.Metadata
{
    public class ForeignKey
    {
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

        public string Name { get; }
        public int Id { get; }
        public int ParentTableId { get; }
        public int ReferencedTableId { get; }
        public bool IsDisabled { get; }
        public Table ParentTable { get; protected set; }
        public Table ReferencedTable { get; protected set; }
        public IReadOnlyList<ForeignKeyColumn> ForeignKeyColumns { get; protected set; }

        public virtual void Initialize(IReadOnlyList<Table> tables, IReadOnlyList<ForeignKeyColumn> foreignKeyColumns)
        {
            ParentTable = tables.Single(t => t.Id == ParentTableId);
            ReferencedTable = tables.Single(t => t.Id == ReferencedTableId);
            ForeignKeyColumns = foreignKeyColumns
                .Where(fkc => fkc.ForeignKeyId == Id)
                .ToReadOnlyList();
        }

        public virtual IEnumerable<Column> ParentColumns
            => ForeignKeyColumns
            .Select(fkc => fkc.ParentColumn);

        public virtual IEnumerable<Column> ReferencedColumns
            => ForeignKeyColumns
            .Select(fkc => fkc.ReferencedColumn);

        public virtual bool IsEffectivelyRequired
            => ParentColumns.All(c => !c.IsNullable
                || c.Table.CheckConstraints.Any(cc => cc.CoalescesOver(c)));

        public virtual bool IsReferencingPrimaryKey
            => ReferencedTable.PrimaryKey?.Columns
            .All(c => ReferencedColumns.Contains(c)) ?? false;

        public override string ToString()
            => $"{ParentTable} to {ReferencedTable}: {Name}";
    }
}
