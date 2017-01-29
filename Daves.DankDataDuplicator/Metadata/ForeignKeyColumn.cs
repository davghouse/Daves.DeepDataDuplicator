using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class ForeignKeyColumn
    {
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

        public int ForeignKeyId { get; }
        public int ParentTableId { get; }
        public int ParentColumnId { get; }
        public int ReferencedTableId { get; }
        public int ReferencedColumnId { get; }
        public ForeignKey ForeignKey { get; protected set; }
        public Table ParentTable { get; protected set; }
        public Column ParentColumn { get; protected set; }
        public Table ReferencedTable { get; protected set; }
        public Column ReferencedColumn { get; protected set; }

        public virtual void Initialize(IReadOnlyList<ForeignKey> foreignKeys, IReadOnlyList<Table> tables, IReadOnlyList<Column> columns)
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
