﻿using Daves.DankDataDuplicator.Metadata;

namespace Daves.DankDataDuplicator
{
    public partial class ReferenceGraph
    {
        public class Reference
        {
            protected readonly Vertex _vertex;

            public Reference(Vertex vertex, Column parentColumn, Table referencedTable)
            {
                _vertex = vertex;
                ParentColumn = parentColumn;
                ReferencedTable = referencedTable;
            }

            public Column ParentColumn { get; }
            public Table ReferencedTable { get; }
        }
    }
}