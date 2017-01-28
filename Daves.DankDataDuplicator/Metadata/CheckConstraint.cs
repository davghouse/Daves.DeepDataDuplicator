using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class CheckConstraint
    {
        protected CheckConstraint()
        { }

        public CheckConstraint(object name, object tableId, object isDisabled, object isTableLevel, object definition)
            : this((string)name, (int)tableId, (bool)isDisabled, (bool)isTableLevel, (string)definition)
        { }

        public CheckConstraint(string name, int tableId, bool isDisabled, bool isTableLevel, string definition)
        {
            Name = name;
            TableId = tableId;
            IsDisabled = isDisabled;
            IsTableLevel = IsTableLevel;
            Definition = definition;
        }

        public virtual string Name { get; }
        public virtual int TableId { get; }
        public virtual bool IsDisabled { get; }
        public virtual bool IsTableLevel { get; }
        public virtual string Definition { get; }
        public virtual Table Table { get; protected set; }

        public virtual void SetAssociations(IReadOnlyList<Table> tables)
            => Table = tables.Single(t => t.Id == TableId);

        public override string ToString()
            => $"{Table}: {Name}";
    }
}
