using Daves.DankDataDuplicator.Helpers;
using Daves.DankDataDuplicator.Metadata;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

namespace Daves.DankDataDuplicator
{
    public partial class ReferenceGraph : IReadOnlyList<ReferenceGraph.Vertex>
    {
        public ReferenceGraph(Catalog catalog, Table rootTable)
        {
            Catalog = catalog;
            RootTable = rootTable;

            if (!RootTable.HasIdentityColumnAsPrimaryKey)
                throw new ArgumentException($"As the root table, {RootTable} needs an identity column as its primary key.");

            var orderedTables = new Stack<Table>();
            ComputeOrdering(orderedTables, new HashSet<Table>(), new HashSet<Table>(), RootTable);

            OrderedVertices = orderedTables
                .Select(t => new Vertex(this, t))
                .ToReadOnlyList();
            OrderedVertices.ForEach(v => v.Initialize());
        }

        protected Catalog Catalog { get; }
        protected Table RootTable { get; }
        protected /*IReadOnly*/HashSet<Table> Tables { get; }
        protected IReadOnlyList<Vertex> OrderedVertices { get; }

        // DFS-based topological sort similar to the listing in: https://en.wikipedia.org/w/index.php?title=Topological_sorting&oldid=753542990.
        // Restructured to avoid recursive calls in the two special scenarios, for readability and a better error message. Table's dependent
        // upon table (table's which reference it through a required foreign key) are added to the stack, then table is.
        protected virtual void ComputeOrdering(Stack<Table> orderedTables, HashSet<Table> beingVisited, HashSet<Table> beenVisited, Table table)
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
                    ComputeOrdering(orderedTables, beingVisited, beenVisited, dependentTable);
                }
            }

            beenVisited.Add(table);
            beingVisited.Remove(table);
            orderedTables.Push(table);
        }

        public Vertex this[int index] => OrderedVertices[index];
        public int Count => OrderedVertices.Count;
        public IEnumerator<Vertex> GetEnumerator() => OrderedVertices.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)OrderedVertices).GetEnumerator();
    }
}
