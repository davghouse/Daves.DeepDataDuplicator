using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class Column
    {
        protected Column()
        { }

        public Column(object tableId, object name, object columnId, object isNullable, object isIdentity, object isComputed)
            : this((int)tableId, (string)name, (int)columnId, (bool)isNullable, (bool)isIdentity, (bool)isComputed)
        { }

        public Column(int tableId, string name, int columnId, bool isNullable, bool isIdentity, bool isComputed)
        {
            TableId = tableId;
            Name = name;
            ColumnId = columnId;
            IsNullable = isNullable;
            IsIdentity = isIdentity;
            IsComputed = isComputed;
        }

        public virtual int TableId { get; }
        public virtual string Name { get; }
        public virtual int ColumnId { get; }
        public virtual bool IsNullable { get; }
        public virtual bool IsIdentity { get; }
        public virtual bool IsComputed { get; }
        public virtual Table Table { get; protected set; }

        public virtual void SetAssociations(IReadOnlyList<Table> tables)
            => Table = tables.Single(t => t.Id == TableId);

        public override string ToString()
            => $"{Table}: {Name}";
    }
}
