﻿using Daves.DeepDataDuplicator.Metadata;

namespace Daves.DeepDataDuplicator
{
    public partial class ReferenceGraph
    {
        public class Reference
        {
            public Reference(Vertex vertex, Column parentColumn, Table referencedTable)
            {
                Vertex = vertex;
                ParentColumn = parentColumn;
                ReferencedTable = referencedTable;
            }

            protected Vertex Vertex { get; }
            public Column ParentColumn { get; }
            public Table ReferencedTable { get; }
        }
    }
}