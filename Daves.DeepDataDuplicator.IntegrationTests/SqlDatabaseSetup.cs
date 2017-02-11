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
        public static readonly string ConnectionString;

        static SqlDatabaseSetup()
        {
            using (var executionContext = SqlDatabaseTestClass.TestService.OpenExecutionContext())
            {
                ConnectionString = executionContext.Connection.ConnectionString;
            }
        }

        [AssemblyInitialize]
        public static void InitializeAssembly(TestContext testContext)
        {
            SqlDatabaseTestClass.TestService.DeployDatabaseProject();

            using (var transaction = new TransactionScope())
            {
                using (var connection = new SqlConnection(ConnectionString))
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
