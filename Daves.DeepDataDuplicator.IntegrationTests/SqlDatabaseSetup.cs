using Microsoft.Data.Tools.Schema.Sql.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data;
using System.Data.SqlClient;
using System.Transactions;

namespace Daves.DeepDataDuplicator.IntegrationTests
{
    [TestClass]
    public class SqlDatabaseSetup
    {
        [AssemblyInitialize]
        public static void InitializeAssembly(TestContext testContext)
        {
            SqlDatabaseTestClass.TestService.DeployDatabaseProject();

            string connectionString;
            using (var executionContext = SqlDatabaseTestClass.TestService.OpenExecutionContext())
            {
                connectionString = executionContext.Connection.ConnectionString;
            }

            using (var transaction = new TransactionScope())
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    using (IDbCommand command = connection.CreateCommand())
                    {
                        command.CommandText = DeepCopyGenerator.GenerateProcedure(
                            connection: connection,
                            rootTableName: "Nations");
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Complete();
            }
        }
    }
}
