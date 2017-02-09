using Daves.DeepDataDuplicator.UnitTests.SampleCatalogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Daves.DeepDataDuplicator.UnitTests
{
    [TestClass]
    public class ReferenceGraphTests
    {
        [TestMethod]
        public void TableOrder_ForRootedWorld()
        {
            var referenceGraph = new ReferenceGraph(
                catalog: RootedWorld.Catalog,
                rootTable: RootedWorld.NationsTable);

            CollectionAssert.AreEqual(new[]
                {
                    RootedWorld.NationsTable,
                    RootedWorld.ProvincesTable,
                    RootedWorld.ResidentsTable
                }, referenceGraph.Tables.ToArray());
        }

        [TestMethod]
        public void TableOrder_ForUnrootedWorld()
        {
            var referenceGraph = new ReferenceGraph(
                catalog: UnrootedWorld.Catalog,
                rootTable: UnrootedWorld.NationsTable);

            CollectionAssert.AreEqual(new[]
                {
                    UnrootedWorld.NationsTable,
                    UnrootedWorld.ProvincesTable,
                    UnrootedWorld.ResidentsTable
                }, referenceGraph.Tables.ToArray());
        }

        [TestMethod]
        public void DependentReferences_ForRootedWorld()
        {
            var referenceGraph = new ReferenceGraph(
                catalog: RootedWorld.Catalog,
                rootTable: RootedWorld.NationsTable);

            ReferenceGraph.Reference reference;

            Assert.AreEqual(0, referenceGraph[0].DependentReferences.Count);

            reference = referenceGraph[1].DependentReferences.Single();
            Assert.AreEqual(RootedWorld.ProvincesTable.FindColumn("NationID"), reference.ParentColumn);
            Assert.AreEqual(RootedWorld.NationsTable, reference.ReferencedTable);

            reference = referenceGraph[2].DependentReferences.Single();
            Assert.AreEqual(RootedWorld.ResidentsTable.FindColumn("ProvinceID"), reference.ParentColumn);
            Assert.AreEqual(RootedWorld.ProvincesTable, reference.ReferencedTable);
        }

        [TestMethod]
        public void DependentReferences_ForUnrootedWorld()
        {
            var referenceGraph = new ReferenceGraph(
                catalog: UnrootedWorld.Catalog,
                rootTable: UnrootedWorld.NationsTable);

            ReferenceGraph.Reference reference;

            Assert.AreEqual(0, referenceGraph[0].DependentReferences.Count);

            reference = referenceGraph[1].DependentReferences.Single();
            Assert.AreEqual(UnrootedWorld.ProvincesTable.FindColumn("NationID"), reference.ParentColumn);
            Assert.AreEqual(UnrootedWorld.NationsTable, reference.ReferencedTable);

            reference = referenceGraph[2].DependentReferences.Single(r => r.ReferencedTable == UnrootedWorld.ProvincesTable);
            Assert.AreEqual(UnrootedWorld.ResidentsTable.FindColumn("ProvinceID"), reference.ParentColumn);
            reference = referenceGraph[2].DependentReferences.Single(r => r.ReferencedTable == UnrootedWorld.NationsTable);
            Assert.AreEqual(UnrootedWorld.ResidentsTable.FindColumn("NationalityNationID"), reference.ParentColumn);
            Assert.AreEqual(2, referenceGraph[2].DependentReferences.Count);
        }

        [TestMethod]
        public void NonDependentReferences_ForRootedWorld()
        {
            var referenceGraph = new ReferenceGraph(
                catalog: RootedWorld.Catalog,
                rootTable: RootedWorld.NationsTable);

            Assert.AreEqual(0, referenceGraph[0].NonDependentReferences.Count);
            Assert.AreEqual(0, referenceGraph[1].NonDependentReferences.Count);
            Assert.AreEqual(0, referenceGraph[2].NonDependentReferences.Count);
        }

        [TestMethod]
        public void NonDependentReferences_ForUnrootedWorld()
        {
            var referenceGraph = new ReferenceGraph(
                catalog: UnrootedWorld.Catalog,
                rootTable: UnrootedWorld.NationsTable);

            ReferenceGraph.Reference reference;

            Assert.AreEqual(0, referenceGraph[0].NonDependentReferences.Count);

            reference = referenceGraph[1].NonDependentReferences.Single();
            Assert.AreEqual(UnrootedWorld.ProvincesTable.FindColumn("LeaderResidentID"), reference.ParentColumn);
            Assert.AreEqual(UnrootedWorld.ResidentsTable, reference.ReferencedTable);

            reference = referenceGraph[2].NonDependentReferences.Single(r => r.ReferencedTable == UnrootedWorld.ResidentsTable);
            Assert.AreEqual(UnrootedWorld.ResidentsTable.FindColumn("SpouseResidentID"), reference.ParentColumn);
            reference = referenceGraph[2].NonDependentReferences.Single(r => r.ReferencedTable == UnrootedWorld.ProvincesTable);
            Assert.AreEqual(UnrootedWorld.ResidentsTable.FindColumn("FavoriteProvinceID"), reference.ParentColumn);
            Assert.AreEqual(2, referenceGraph[2].NonDependentReferences.Count);
        }
    }
}
