using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class ForeignKeyColumn
    {
        protected ForeignKeyColumn()
        { }

        public ForeignKeyColumn(object foreignKeyId, object parentTableId, object parentColumnId, object referencedTableId, object referencedColumnId)
            : this ((int)foreignKeyId, (int)parentTableId, (int)parentColumnId, (int)referencedTableId, (int)referencedColumnId)
        { } 

        public ForeignKeyColumn(int foreignKeyId, int parentTableId, int parentColumnId, int referencedTableId, int referencedColumnId)
        {
            ForeignKeyId = foreignKeyId;
            ParentTableId = parentTableId;
            ParentColumnId = parentColumnId;
            ReferencedTableId = referencedTableId;
            ReferencedColumnId = referencedColumnId;
        }

        public virtual int ForeignKeyId { get; }
        public virtual int ParentTableId { get; }
        public virtual int ParentColumnId { get; }
        public virtual int ReferencedTableId { get; }
        public virtual int ReferencedColumnId { get; }
        public virtual ForeignKey ForeignKey { get; protected set; }
        public virtual Table ParentTable { get; protected set; }
        public virtual Column ParentColumn { get; protected set; }
        public virtual Table ReferencedTable { get; protected set; }
        public virtual Column ReferencedColumn { get; protected set; }

        public virtual void SetAssociations(IReadOnlyList<ForeignKey> foreignKeys, IReadOnlyList<Table> tables, IReadOnlyList<Column> columns)
        {
            ForeignKey = foreignKeys.Single(k => k.Id == ForeignKeyId);
            ParentTable = tables.Single(t => t.Id == ParentTableId);
            ParentColumn = columns.Single(c => c.TableId == ParentTableId && c.ColumnId == ParentColumnId);
            ReferencedTable = tables.Single(t => t.Id == ReferencedTableId);
            ReferencedColumn = columns.Single(c => c.TableId == ReferencedTableId && c.ColumnId == ReferencedColumnId);
        }

        public override string ToString()
            => $"{ParentColumn} to {ReferencedColumn} ({ForeignKey?.Name})";
    }
}
