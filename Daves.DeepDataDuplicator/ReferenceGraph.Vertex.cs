﻿using Daves.DeepDataDuplicator.Helpers;
using Daves.DeepDataDuplicator.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Daves.DeepDataDuplicator
{
    public partial class ReferenceGraph
    {
        public class Vertex
        {
            protected internal Vertex(ReferenceGraph referenceGraph, Table table)
            {
                ReferenceGraph = referenceGraph;
                Table = table;
            }

            protected ReferenceGraph ReferenceGraph { get; }
            public Table Table { get; }
            public IReadOnlyList<Reference> DependentReferences { get; protected set; }
            public IReadOnlyList<Reference> NonDependentReferences { get; protected set; }

            protected internal virtual void Initialize()
            {
                InitializeDependentReferences();
                InitializeNonDependentReferences();
            }

            protected virtual void InitializeDependentReferences()
            {
                var requiredForeignKeys = Table.ChildForeignKeys
                    .Where(k => k.IsEffectivelyRequired)
                    .Where(k => ReferenceGraph.Tables.Contains(k.ReferencedTable));

                foreach (var foreignKey in requiredForeignKeys)
                {
                    if (!foreignKey.ReferencedTable.HasIdentityColumnAsPrimaryKey)
                        throw new ArgumentException($"As a table with dependent tables, {foreignKey.ReferencedTable} needs an identity column as its primary key.");

                    if (!foreignKey.IsReferencingPrimaryKey)
                        throw new ArgumentException($"As a dependent of {foreignKey.ReferencedTable}, {Table} can have required foreign keys only to that table's primary key.");
                }

                DependentReferences = BuildReferences(requiredForeignKeys);
            }

            protected virtual void InitializeNonDependentReferences()
            {
                var optionalForeignKeys = Table.ChildForeignKeys
                    .Where(k => k.IsReferencingPrimaryKey)
                    .Where(k => ReferenceGraph.Tables.Contains(k.ReferencedTable))
                    .Where(k => !k.IsEffectivelyRequired);

                if (optionalForeignKeys.Any() && !Table.HasIdentityColumnAsPrimaryKey)
                    throw new ArgumentException($"In order to update its optional foreign keys, {Table} needs an identity column as its primary key.");

                foreach (var foreignKey in optionalForeignKeys)
                {
                    if (!foreignKey.ReferencedTable.HasIdentityColumnAsPrimaryKey)
                        throw new ArgumentException($"As a table with referencing tables, {foreignKey.ReferencedTable} needs an identity column as its primary key.");
                }

                NonDependentReferences = BuildReferences(optionalForeignKeys);
            }

            protected virtual IReadOnlyList<Reference> BuildReferences(IEnumerable<ForeignKey> foreignKeysReferencingIdentityPrimaryKeys)
            {
                // The foreign keys are likely already equivalent to distinct (fromColumn, toTable) pairs, but need to be sure in case of weird or misconfigured databases.
                var references = foreignKeysReferencingIdentityPrimaryKeys
                    .SelectMany(k => k.ForeignKeyColumns)
                    .Where(fkc => fkc.ReferencedColumn == fkc.ReferencedTable.PrimaryKey.Column)
                    .Select(fkc => new { fkc.ParentColumn, fkc.ReferencedTable })
                    .Distinct()
                    .Select(a => new Reference(this, a.ParentColumn, a.ReferencedTable))
                    .ToReadOnlyList();

                // And some last edge case handling, again probably just for misconfigured databases.
                var columnsDependentOnMultiplePrimaryKeys = references
                    .GroupBy(d => d.ParentColumn)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);
                if (columnsDependentOnMultiplePrimaryKeys.Any())
                    throw new ArgumentException($"{columnsDependentOnMultiplePrimaryKeys.First()} is dependent on multiple primary keys.");

                return references;
            }

            public virtual bool IsReferenced()
                => ReferenceGraph.Any(v =>
                    v.DependentReferences.Any(r => r.ReferencedTable == Table)
                    || v.NonDependentReferences.Any(r => r.ReferencedTable == Table));
        }
    }
}
