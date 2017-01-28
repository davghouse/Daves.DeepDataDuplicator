using Daves.DankDataDuplicator.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DankDataDuplicator.Metadata
{
    public class Schema
    {
        protected Schema()
        { }

        public Schema(object name, object id)
            : this((string)name, (int)id)
        { }

        public Schema(string name, int id)
        {
            Name = name;
            Id = id;
        }

        public virtual string Name { get; }
        public virtual int Id { get; }
        public virtual IReadOnlyList<Table> Tables { get; protected set; }

        public virtual void SetAssociations(IReadOnlyList<Table> tables)
            => Tables = tables
            .Where(t => t.SchemaId == Id)
            .ToReadOnlyList();

        public override string ToString()
            => Name;
    }
}
