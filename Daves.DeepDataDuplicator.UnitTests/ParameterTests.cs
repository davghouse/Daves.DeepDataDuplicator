using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Daves.DeepDataDuplicator.UnitTests
{
    [TestClass]
    public class ParameterTests
    {
        [TestMethod]
        public void Construction_WithInvalidName()
        {
            var parameter = new Parameter(name: "count", dataTypeDescription: "INT = NULL");
            Assert.AreEqual("@count", parameter.Name);
        }

        [TestMethod]
        public void Construction_WithValidName()
        {
            var parameter = new Parameter(name: "@count", dataTypeDescription: "INT = NULL");
            Assert.AreEqual("@count", parameter.Name);
        }
    }
}
