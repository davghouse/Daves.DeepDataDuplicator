using System;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class CheckConstraint
    {
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

        public string Name { get; }
        public int TableId { get; }
        public bool IsDisabled { get; }
        public bool IsTableLevel { get; }
        public string Definition { get; }
        public Table Table { get; protected set; }

        public virtual void Initialize(IReadOnlyList<Table> tables)
            => Table = tables.Single(t => t.Id == TableId);

        // Could make more accurate, but is there a solution that's not string-based?
        public virtual bool CoalescesOver(Column column)
            => Definition.IndexOf("COALESCE", StringComparison.OrdinalIgnoreCase) >= 0
            && Definition.IndexOf(column.Name, StringComparison.OrdinalIgnoreCase) >= 0
            && Definition.IndexOf("IS NOT NULL", StringComparison.OrdinalIgnoreCase) >= 0;

        public override string ToString()
            => $"{Table}: {Name}";
    }
}
