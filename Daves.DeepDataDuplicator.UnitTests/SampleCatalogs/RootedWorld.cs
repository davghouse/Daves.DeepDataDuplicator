using Daves.DeepDataDuplicator.Metadata;

namespace Daves.DeepDataDuplicator.UnitTests.SampleCatalogs
{
    public static class RootedWorld
    {
        // A nation has provinces, and provinces have residents. A Nation row acts as the top-level root, because
        // when a nation row is copied, all rows that get copied depend only upon the copied nation row, not a different one.
        public static readonly Catalog Catalog;
        public static Table NationsTable => Catalog.FindTable("Nations");
        public static Table ProvincesTable => Catalog.FindTable("Provinces");
        public static Table ResidentsTable => Catalog.FindTable("Residents");

        static RootedWorld()
        {
            var schemas = new[]
            {
                new Schema(name: "dbo", id: 1),
                new Schema(name: "sys", id: 2)
            };
            var tables = new[]
            {
                // Create tables out of order to make sure proper ordering is happening.
                new Table(name: "Residents", id: 6, schemaId: 1),
                new Table(name: "Nations", id: 3, schemaId: 1),
                new Table(name: "Provinces", id: 4, schemaId: 1)
            };
            var columns = new[]
            {
                new Column(tableId: 6, name: "ID", columnId: 1, isNullable: false, isIdentity: true),
                new Column(tableId: 6, name: "Name", columnId: 2, isNullable: false),
                new Column(tableId: 6, name: "ProvinceID", columnId: 3, isNullable: false),
                new Column(tableId: 3, name: "ID", columnId: 1, isNullable: false, isIdentity: true),
                new Column(tableId: 3, name: "Name", columnId: 2, isNullable: false),
                new Column(tableId: 3, name: "FoundedDate", columnId: 3, isNullable: false),
                new Column(tableId: 4, name: "ID", columnId: 1, isNullable: false, isIdentity: true),
                new Column(tableId: 4, name: "NationID", columnId: 2, isNullable: false),
                new Column(tableId: 4, name: "Name", columnId: 3, isNullable: false),
                new Column(tableId: 4, name: "Motto", columnId: 4, isNullable: false)
            };
            var primaryKeys = new[]
            {
                new PrimaryKey(tableId: 6, name: "PK_Residents"),
                new PrimaryKey(tableId: 3, name: "PK_Nations"),
                new PrimaryKey(tableId: 4, name: "PK_Provinces"),
            };
            var primaryKeyColumns = new[]
            {
                new PrimaryKeyColumn(tableId: 3, columnId: 1),
                new PrimaryKeyColumn(tableId: 4, columnId: 1),
                new PrimaryKeyColumn(tableId: 6, columnId: 1)
            };
            var foreignKeys = new[]
            {
                new ForeignKey(name: "FK_Provinces_NationID_Nations_ID", id: 5, parentTableId: 4, referencedTableId: 3),
                new ForeignKey(name: "FK_Residents_ProvinceID_Provinces_ID", id: 7, parentTableId: 6, referencedTableId: 4)
            };
            var foreignKeyColumns = new[]
            {
                new ForeignKeyColumn(foreignKeyId: 7, parentTableId: 6, parentColumnId: 3, referencedTableId: 4, referencedColumnId: 1),
                new ForeignKeyColumn(foreignKeyId: 5, parentTableId: 4, parentColumnId: 2, referencedTableId: 3, referencedColumnId: 1)
            };
            var checkConstraints = new CheckConstraint[0];

            Catalog = new Catalog(schemas, tables, columns, primaryKeys, primaryKeyColumns, foreignKeys, foreignKeyColumns, checkConstraints);
        }
    }
}
