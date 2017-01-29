﻿using Daves.DankDataDuplicator.Metadata;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using Daves.DankDataDuplicator.Helpers;

namespace Daves.DankDataDuplicator
{
    public partial class ReferenceGraph : IReadOnlyList<ReferenceGraph.Vertex>
    {
        protected readonly Catalog _catalog;
        protected readonly Table _rootTable;
        protected readonly /*IReadOnly*/HashSet<Table> _tables;
        protected readonly IReadOnlyList<Vertex> _orderedVertices;

        public ReferenceGraph(Catalog catalog, Table rootTable)
        {
            _catalog = catalog;
            _rootTable = rootTable;

            if (!_rootTable.HasIdentityColumnAsPrimaryKey)
                throw new ArgumentException($"As the root table, {_rootTable} needs an identity column as its primary key.");

            var orderedTables = new Stack<Table>();
            ComputeOrdering(orderedTables, new HashSet<Table>(), new HashSet<Table>(), _rootTable);

            _orderedVertices = orderedTables
                .Select(t => new Vertex(this, t))
                .ToReadOnlyList();
            _orderedVertices.ForEach(v => v.Initialize());
        }

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

        public Vertex this[int index] => _orderedVertices[index];
        public int Count => _orderedVertices.Count;
        public IEnumerator<Vertex> GetEnumerator() => _orderedVertices.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_orderedVertices).GetEnumerator();
    }
}