using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class PrimaryKeyColumn
    {
        protected PrimaryKeyColumn()
        { }

        public PrimaryKeyColumn(object tableId, object columnId)
            : this((int)tableId, (int)columnId)
        { }

        public PrimaryKeyColumn(int tableId, int columnId)
        {
            TableId = tableId;
            ColumnId = columnId;
        }

        public virtual int TableId { get; }
        public virtual int ColumnId { get; }
        public virtual PrimaryKey PrimaryKey { get; protected set; }
        public virtual Table Table { get; protected set; }
        public virtual Column Column { get; protected set; }

        public virtual void SetAssociations(IReadOnlyList<PrimaryKey> primaryKeys, IReadOnlyList<Table> tables, IReadOnlyList<Column> columns)
        {
            PrimaryKey = primaryKeys.Single(k => k.TableId == TableId);
            Table = tables.Single(t => t.Id == TableId);
            Column = columns.Single(c => c.TableId == TableId && c.ColumnId == ColumnId);
        }

        public override string ToString()
            => $"{Column} ({PrimaryKey?.Name})";
    }
}
