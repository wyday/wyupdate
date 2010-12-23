using wyUpdate.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace wyUpdateTests
{
    /// <summary>
    ///This is a test class for VersionToolsTest and is intended
    ///to contain all VersionToolsTest Unit Tests
    ///</summary>
    [TestClass]
    public class VersionToolsTest
    {
        TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Compare
        ///</summary>
        [TestMethod]
        public void CompareTest()
        {
            string[] versionA = { "1.0",     "1.0 beta 1",     "1 a",      "1.0 Beta .5", ".9",  "1.1.0", "1.0rc2", "1.0beta1", "1.2.03", "1.2rc" };
            string[] versionB = { "1.0.0.0", "1.0.0.0 beta 1", "1.0 beta", "1.0 Beta 4",  "1.0", "2.1.0", "1.0rc3", "1.0rc2",   "1 2 3",  "1.2 release candidate" };
            int[] expected = { 0, 0, 1, -1, -1, -1, -1, -1, 0, 0 };

            for (int i=0; i<versionA.Length; i++)
            {
                int actual = VersionTools.Compare(versionA[i], versionB[i]);

                Assert.AreEqual(expected[i], actual);
            }
        }
    }
}
