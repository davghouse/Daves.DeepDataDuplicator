using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class PrimaryKeyColumn
    {
        public PrimaryKeyColumn(object tableId, object columnId)
            : this((int)tableId, (int)columnId)
        { }

        public PrimaryKeyColumn(int tableId, int columnId)
        {
            TableId = tableId;
            ColumnId = columnId;
        }

        public int TableId { get; }
        public int ColumnId { get; }
        public PrimaryKey PrimaryKey { get; protected set; }
        public Table Table { get; protected set; }
        public Column Column { get; protected set; }

        public virtual void Initialize(IReadOnlyList<PrimaryKey> primaryKeys, IReadOnlyList<Table> tables, IReadOnlyList<Column> columns)
        {
            PrimaryKey = primaryKeys.Single(k => k.TableId == TableId);
            Table = tables.Single(t => t.Id == TableId);
            Column = columns.Single(c => c.TableId == TableId && c.ColumnId == ColumnId);
        }

        public override string ToString()
            => $"{Column} ({PrimaryKey?.Name})";
    }
}
