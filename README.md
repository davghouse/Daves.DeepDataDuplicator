Dave's Deep Data Duplicator
================

Uses database metadata to generate deep copy procedures for SQL Server.

Introduction
------------

Take the following simple schema:

![Rooted World](/Daves.DeepDataDuplicator.UnitTests/SampleCatalogs/Diagrams/RootedWorld.PNG)

Say we want to copy a nation and all the data recursively dependent upon it. We'd copy the nation row, all the provinces dependent upon that nation, and all the residents dependent upon those provinces. The procedure to do that looks like [this](https://gist.github.com/davghouse/37d07ac6ac6fb21ddd1b1de8a5b94702), and gets generated like this, where 'connection' is an IDbConnection to the database for retrieving its metadata:

```cs
string procedure = RootCopyGenerator.GenerateProcedure(connection, rootTableName: "Nations");
```

We might only want to copy a province. In that case, we'd copy the province row, and all the residents dependent upon that province. The procedure to do that looks like [this](https://gist.github.com/davghouse/c0e80cb43c43940cbfc8a153f96ccd3d), and gets generated like this:

```cs
string procedure = RootCopyGenerator.GenerateProcedure(connection, rootTableName: "Provinces");
```

Take the more complex schema, based upon the one above:

![Unrooted World](/Daves.DeepDataDuplicator.UnitTests/SampleCatalogs/Diagrams/UnrootedWorld.PNG)

Say we want to copy a nation and all the data recursively dependent upon it. We'd copy the nation row, all the provinces dependent upon that nation, and... here's a little complication. Residents are dependent upon both nations and provinces now. In copying a nation, we need to copy all the residents with that nation as their nationality. In copying a province, we need to copy all the residents with that province as their province. Hence, we need to copy all the residents with at least some dependency on what's been copied, be it through the nation dependency, the province dependency, or both. The procedure to do that looks like [this](https://gist.github.com/davghouse/13e57054334052b54b07578ae4f342e2), and gets generated like this:

```cs
string procedure = DeepCopyGenerator.GenerateProcedure(connection, rootTableName: "Nations");
```

Notice we're now using DeepCopyGenerator instead of RootCopyGenerator. DeepCopyGenerator *is* a RootCopyGenerator, but it makes fewer assumptions about the data. The DeepCopyGenerator left joins when there are multiple dependent references on a table, making sure that at least one dependency goes back to a copied row. The RootCopyGenerator inner joins, so all dependencies necessarily go back to a copied row. The RootCopyGenerator is appropriate when the row we're copying from truly acts as a logical root for the data beneath it. In the second schema, this isn't the case. A resident can be dependent upon two nations, one through its nationality, and one through its province. However, imagine instead of NationalityNationID it was just providing some denormalization as NationID. In that case, RootCopyGenerator would again be appropriate. Note that RootCopyGenerator and DeepCopyGenerator produce exactly the same procedure for the first schema, because when there's only one dependency between tables we might as well inner join. It's best to use RootCopyGenerator when possible, because inner joins provide better performance than left joins.

Example
-------
I need a deep copy procedure for a web application backed by a multi-tenant database. In this database, an 'organization' row acts as a root for all of the data beneath it. Non-developer team members configure template organizations through normal use of the application. When potential customers create free trials, or when new customers are being configured, an appropriate template organization is chosen and copied from to seed their organization. Here's the FluentMigrator migration which updates the procedure in response to any schema changes (the procedure is dropped prior to this point):

```cs
[Maintenance(MigrationStage.AfterAll), Tags("PrePublish"), Tags("MigrateUp")]
public class AA04_CreateCopyOrganizationProcedure : ForwardOnlyMigration
{
    public override void Up()
    {
        Execute.WithConnection((connection, transaction) =>
        {
            var catalog = new Catalog(connection, transaction);
            var hostnameTable = catalog.FindTable("OrganizationHostnames");
            var organizationTable = catalog.FindTable("Organizations");
            var userTable = catalog.FindTable("Users");
            string copyOrganizationProcedure = RootCopyGenerator.GenerateProcedure(
                catalog: catalog,
                rootTable: organizationTable,
                primaryKeyParameterName: "@fromOrganizationID",
                updateParameters: new Dictionary<Column, Parameter>
                {
                    { hostnameTable.FindColumn("Hostname"), new Parameter("@toHostname", "VARCHAR (50)") },
                    { organizationTable.FindColumn("Name"), new Parameter("@toOrganizationName", "NVARCHAR (50)") }
                },
                primaryKeyOutputParameterName: "@insertedOrganizationID",
                excludedColumns: new[]
                {
                    organizationTable.FindColumn("SisenseGroupID"),
                    organizationTable.FindColumn("SisenseDataSecurityRuleID"),
                    userTable.FindColumn("SisenseUserID"),
                    userTable.FindColumn("SisenseApiSecret")
                },
                excludedTables: new[]
                {
                    catalog.FindTable("FreeTrialSettings")
                });

            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = copyOrganizationProcedure;
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
        });
    }
}
```

Algorithm
---------
Starting from the root table, recursively discover tables with dependent references to the discovered tables. Dependent references are non-nullable foreign keys, or nullable foreign keys coalesced over by a check constraint. Perform a [topological sort](https://en.wikipedia.org/w/index.php?title=Topological_sorting&oldid=753542990) on those tables. Copy the tables in that order, using the dependent references to discover what rows need to be copied. Rows are inserted with their dependent references already updated, but their non-dependent references (nullable foreign keys) are left alone. Once all the dependent references are taken care of (i.e., all the rows have been inserted), go through and update the non-dependent references.

Notes
-----
As seen above, the root table can have a dependent reference off to some other table, with the recursion never discovering that table. In that case, the dependent reference is copied without being updated just like any other non-reference column.

The copying relies on tables having an identity column as a primary key, except for tables without incoming dependent references **and** without outgoing non-dependent references (e.g., cross-reference tables are fine). To understand the limitations better, search the code for 'ArgumentException'.

Triggers should be reviewed to make sure they won't cause problems, or they should be disabled before running the procedure. The main concern is that non-dependent references of inserted rows temporarily reference non-inserted rows. In my case triggers were being used to update some aggregate on a table being dependently referenced, so there was no problem.
