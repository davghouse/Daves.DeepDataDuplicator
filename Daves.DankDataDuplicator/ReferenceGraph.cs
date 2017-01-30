using Daves.DankDataDuplicator.Helpers;
using Daves.DankDataDuplicator.Metadata;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Daves.DankDataDuplicator
{
    public partial class ReferenceGraph : IReadOnlyList<ReferenceGraph.Vertex>
    {
        public ReferenceGraph(Catalog catalog, Table rootTable)
        {
            if (!rootTable.HasIdentityColumnAsPrimaryKey)
                throw new ArgumentException($"As the root table, {rootTable} needs an identity column as its primary key.");

            var tables = new Stack<Table>();
            ComputeOrdering(tables, new HashSet<Table>(), new HashSet<Table>(), rootTable);

            Tables = tables.ToReadOnlyList();
            Vertices = Tables
                .Select(t => new Vertex(this, t))
                .ToReadOnlyList();
            Vertices.ForEach(v => v.Initialize());
        }

        public IReadOnlyList<Table> Tables { get; }
        public IReadOnlyList<Vertex> Vertices { get; }

        // DFS-based topological sort similar to the listing in: https://en.wikipedia.org/w/index.php?title=Topological_sorting&oldid=753542990.
        // Restructured to avoid recursive calls in the two special scenarios, for readability and a better error message. Table's dependent
        // upon table (table's which reference it through a required foreign key) are added to the stack, then table is.
        protected virtual void ComputeOrdering(Stack<Table> tables, HashSet<Table> beingVisited, HashSet<Table> beenVisited, Table table)
        {
            beingVisited.Add(table);

            foreach (var dependentTable in table.ReferencingForeignKeys
                .Where(k => k.IsEffectivelyRequired)
                .Select(k => k.ParentTable)
                .Distinct())
            {
                // If a table is being visited, we're in the process of visiting all tables dependent upon it. Therefore, if dependentTable
                // is already being visited, table must depend upon it... but it evidently also depends upon table, so there's a cycle.
                if (beingVisited.Contains(dependentTable))
                    throw new ArgumentException($"A dependency cycle exists through {dependentTable} and {table}.");

                if (!beenVisited.Contains(dependentTable))
                {
                    ComputeOrdering(tables, beingVisited, beenVisited, dependentTable);
                }
            }

            beenVisited.Add(table);
            beingVisited.Remove(table);
            tables.Push(table);
        }

        public Vertex this[int index] => Vertices[index];
        public int Count => Vertices.Count;
        public IEnumerator<Vertex> GetEnumerator() => Vertices.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Vertices).GetEnumerator();
    }
}
