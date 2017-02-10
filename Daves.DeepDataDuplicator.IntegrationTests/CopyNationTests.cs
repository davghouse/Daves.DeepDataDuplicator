using Microsoft.Data.Tools.Schema.Sql.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace Daves.DeepDataDuplicator.IntegrationTests
{
    [TestClass]
    public class CopyNationTests : SqlDatabaseTestClass
    {
        private SqlDatabaseTestActions CopyNation_UsingDefaultProcedureData;

        public CopyNationTests()
            => InitializeComponent();

        [TestInitialize]
        public void TestInitialize()
            => base.InitializeTest();

        [TestCleanup]
        public void TestCleanup()
            => base.CleanupTest();

        [TestMethod]
        public void CopyNation_UsingDefaultProcedure()
        {
            Trace.WriteLineIf((CopyNation_UsingDefaultProcedureData.PretestAction != null), "Executing pre-test script...");
            SqlExecutionResult[] pretestResults = TestService.Execute(this.PrivilegedContext, this.PrivilegedContext, CopyNation_UsingDefaultProcedureData.PretestAction);
            try
            {
                Trace.WriteLineIf((CopyNation_UsingDefaultProcedureData.TestAction != null), "Executing test script...");
                SqlExecutionResult[] testResults = TestService.Execute(this.ExecutionContext, this.PrivilegedContext, CopyNation_UsingDefaultProcedureData.TestAction);
            }
            finally
            {
                Trace.WriteLineIf((CopyNation_UsingDefaultProcedureData.PosttestAction != null), "Executing post-test script...");
                SqlExecutionResult[] posttestResults = TestService.Execute(this.PrivilegedContext, this.PrivilegedContext, CopyNation_UsingDefaultProcedureData.PosttestAction);
            }
        }

        #region Designer support code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Microsoft.Data.Tools.Schema.Sql.UnitTesting.SqlDatabaseTestAction CopyNation_UsingDefaultProcedure_TestAction;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CopyNationTests));
            Microsoft.Data.Tools.Schema.Sql.UnitTesting.Conditions.ChecksumCondition checksum;
            this.CopyNation_UsingDefaultProcedureData = new Microsoft.Data.Tools.Schema.Sql.UnitTesting.SqlDatabaseTestActions();
            CopyNation_UsingDefaultProcedure_TestAction = new Microsoft.Data.Tools.Schema.Sql.UnitTesting.SqlDatabaseTestAction();
            checksum = new Microsoft.Data.Tools.Schema.Sql.UnitTesting.Conditions.ChecksumCondition();
            // 
            // CopyNation_UsingDefaultProcedure_TestAction
            // 
            CopyNation_UsingDefaultProcedure_TestAction.Conditions.Add(checksum);
            resources.ApplyResources(CopyNation_UsingDefaultProcedure_TestAction, "CopyNation_UsingDefaultProcedure_TestAction");
            // 
            // checksum
            // 
            checksum.Checksum = "1403054032";
            checksum.Enabled = true;
            checksum.Name = "checksum";
            // 
            // CopyNation_UsingDefaultProcedureData
            // 
            this.CopyNation_UsingDefaultProcedureData.PosttestAction = null;
            this.CopyNation_UsingDefaultProcedureData.PretestAction = null;
            this.CopyNation_UsingDefaultProcedureData.TestAction = CopyNation_UsingDefaultProcedure_TestAction;
        }

        #endregion
    }
}
