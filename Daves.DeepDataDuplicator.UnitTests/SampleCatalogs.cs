using Daves.DeepDataDuplicator.Metadata;

namespace Daves.DeepDataDuplicator.UnitTests
{
    public static class SampleCatalogs
    {
        // A nation has provinces, and provinces have residents. A Nation row acts as a top-level root, because
        // when a nation row is copied, all rows that get copied must depend only upon the copied nation row.
        public static readonly Catalog RootedWorld;

        // A nation has provinces, and provinces have residents. Residents also have a nationality, which is a
        // reference to their birth nation. A Nation row doesn't act as a top-level root, because when a nation
        // row is copied, residents dependent upon that nation's provinces get copied, but those same residents
        // may be dependent upon an entirely different nation through their nationality. All rows that get copied
        // do depend upon the copied nation row, but can also depend upon non-copied nation rows.
        public static readonly Catalog UnrootedWorld;

        static SampleCatalogs()
        {
            RootedWorld = BuildRootedWorldCatalog();
            UnrootedWorld = BuildUnrootedWorldCatalog();
        }

        private static Catalog BuildRootedWorldCatalog()
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
                new Column(tableId: 6, name: "ID", columnId: 1, isNullable: false, isIdentity: true, isComputed: false),
                new Column(tableId: 6, name: "ProvinceID", columnId: 2, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 3, name: "ID", columnId: 1, isNullable: false, isIdentity: true, isComputed: false),
                new Column(tableId: 3, name: "Name", columnId: 2, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 3, name: "DateFounded", columnId: 3, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 4, name: "ID", columnId: 1, isNullable: false, isIdentity: true, isComputed: false),
                new Column(tableId: 4, name: "NationID", columnId: 2, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 4, name: "Name", columnId: 3, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 4, name: "TimeZone", columnId: 4, isNullable: false, isIdentity: false, isComputed: false)
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
                new ForeignKey(name: "FK_Provinces_NationID_Nations_ID", id: 5, parentTableId: 4, referencedTableId: 3, isDisabled: false),
                new ForeignKey(name: "FK_Residents_ProvinceID_Provinces_ID", id: 7, parentTableId: 6, referencedTableId: 4, isDisabled: false)
            };
            var foreignKeyColumns = new[]
            {
                new ForeignKeyColumn(foreignKeyId: 7, parentTableId: 6, parentColumnId: 2, referencedTableId: 4, referencedColumnId: 1),
                new ForeignKeyColumn(foreignKeyId: 5, parentTableId: 4, parentColumnId: 2, referencedTableId: 3, referencedColumnId: 1)
            };
            var checkConstraints = new CheckConstraint[0];

            return new Catalog(schemas, tables, columns, primaryKeys, primaryKeyColumns, foreignKeys, foreignKeyColumns, checkConstraints);
        }

        private static Catalog BuildUnrootedWorldCatalog()
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
                new Column(tableId: 6, name: "ID", columnId: 1, isNullable: false, isIdentity: true, isComputed: false),
                new Column(tableId: 6, name: "ProvinceID", columnId: 2, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 6, name: "NationalityNationID", columnId: 3, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 6, name: "SpouseResidentID", columnId: 4, isNullable: true, isIdentity: false, isComputed: false),
                new Column(tableId: 6, name: "FavoriteProvinceID", columnId: 5, isNullable: true, isIdentity: false, isComputed: false),
                new Column(tableId: 3, name: "ID", columnId: 1, isNullable: false, isIdentity: true, isComputed: false),
                new Column(tableId: 3, name: "Name", columnId: 2, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 3, name: "DateFounded", columnId: 3, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 4, name: "ID", columnId: 1, isNullable: false, isIdentity: true, isComputed: false),
                new Column(tableId: 4, name: "NationID", columnId: 2, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 4, name: "Name", columnId: 3, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 4, name: "TimeZone", columnId: 4, isNullable: false, isIdentity: false, isComputed: false),
                new Column(tableId: 4, name: "LeaderResidentID", columnId: 5, isNullable: true, isIdentity: false, isComputed: false)
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
                new ForeignKey(name: "FK_Provinces_NationID_Nations_ID", id: 5, parentTableId: 4, referencedTableId: 3, isDisabled: false),
                new ForeignKey(name: "FK_Residents_ProvinceID_Provinces_ID", id: 7, parentTableId: 6, referencedTableId: 4, isDisabled: false),
                new ForeignKey(name: "FK_Residents_NationalityNationID_Nations_ID", id: 8, parentTableId: 6, referencedTableId: 3, isDisabled: false),
                new ForeignKey(name: "FK_Residents_SpouseResidentID_Residents_ID", id: 9, parentTableId: 6, referencedTableId: 6, isDisabled: false),
                new ForeignKey(name: "FK_Provinces_LeaderResidentID_Residents_ID", id: 10, parentTableId: 4, referencedTableId: 6, isDisabled: false),
                new ForeignKey(name: "FK_Residents_FavoriteProvinceID_Provinces_ID", id: 11, parentTableId: 6, referencedTableId: 4, isDisabled: false)
            };
            var foreignKeyColumns = new[]
            {
                new ForeignKeyColumn(foreignKeyId: 7, parentTableId: 6, parentColumnId: 2, referencedTableId: 4, referencedColumnId: 1),
                new ForeignKeyColumn(foreignKeyId: 5, parentTableId: 4, parentColumnId: 2, referencedTableId: 3, referencedColumnId: 1),
                new ForeignKeyColumn(foreignKeyId: 8, parentTableId: 6, parentColumnId: 3, referencedTableId: 3, referencedColumnId: 1),
                new ForeignKeyColumn(foreignKeyId: 9, parentTableId: 6, parentColumnId: 4, referencedTableId: 6, referencedColumnId: 1),
                new ForeignKeyColumn(foreignKeyId: 10, parentTableId: 4, parentColumnId: 5, referencedTableId: 6, referencedColumnId: 1),
                new ForeignKeyColumn(foreignKeyId: 11, parentTableId: 6, parentColumnId: 5, referencedTableId: 4, referencedColumnId: 1)
            };
            var checkConstraints = new CheckConstraint[0];

            return new Catalog(schemas, tables, columns, primaryKeys, primaryKeyColumns, foreignKeys, foreignKeyColumns, checkConstraints);
        }
    }
}
