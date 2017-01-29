using Daves.DankDataDuplicator.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class PrimaryKey
    {
        public PrimaryKey(object tableId, object name)
            : this((int)tableId, (string)name)
        { }

        public PrimaryKey(int tableId, string name)
        {
            TableId = tableId;
            Name = name;
        }

        public int TableId { get; }
        public string Name { get; }
        public Table Table { get; protected set; }
        public IReadOnlyList<PrimaryKeyColumn> PrimaryKeyColumns { get; protected set; }

        public virtual void Initialize(IReadOnlyList<Table> tables, IReadOnlyList<PrimaryKeyColumn> primaryKeyColumns)
        {
            Table = tables.Single(t => t.Id == TableId);
            PrimaryKeyColumns = primaryKeyColumns
                .Where(c => c.TableId == TableId)
                .ToReadOnlyList();
        }

        public virtual IEnumerable<Column> Columns
            => PrimaryKeyColumns.Select(c => c.Column);

        public virtual Column Column
            => PrimaryKeyColumns.Count == 1 ? PrimaryKeyColumns[0].Column : null;

        public override string ToString()
            => $"{Table}: {Name}";
    }
}
