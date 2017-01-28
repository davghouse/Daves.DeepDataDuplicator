using Daves.DankDataDuplicator.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class PrimaryKey
    {
        protected PrimaryKey()
        { }

        public PrimaryKey(object tableId, object name)
            : this((int)tableId, (string)name)
        { }

        public PrimaryKey(int tableId, string name)
        {
            TableId = tableId;
            Name = name;
        }

        public virtual int TableId { get; }
        public virtual string Name { get; }
        public virtual Table Table { get; protected set; }
        public virtual IReadOnlyList<PrimaryKeyColumn> PrimaryKeyColumns { get; protected set; }

        public virtual void SetAssociations(IReadOnlyList<Table> tables, IReadOnlyList<PrimaryKeyColumn> primaryKeyColumns)
        {
            Table = tables.Single(t => t.Id == TableId);
            PrimaryKeyColumns = primaryKeyColumns
                .Where(c => c.TableId == TableId)
                .ToReadOnlyList();
        }

        public override string ToString()
            => $"{Table}: {Name}";
    }
}
