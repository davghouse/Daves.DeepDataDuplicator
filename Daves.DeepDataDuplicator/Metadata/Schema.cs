using Daves.DeepDataDuplicator.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DeepDataDuplicator.Metadata
{
    public class Schema
    {
        public Schema(object name, object id)
            : this((string)name, (int)id)
        { }

        public Schema(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public string Name { get; }
        public int Id { get; }
        public IReadOnlyList<Table> Tables { get; protected set; }

        public virtual void Initialize(IReadOnlyList<Table> tables)
            => Tables = tables
            .Where(t => t.SchemaId == Id)
            .ToReadOnlyList();

        public string SpacelessName
            => Name.ToSpacelessName();

        public override string ToString()
            => Name;
    }
}
