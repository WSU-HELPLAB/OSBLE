using OSBLE.Areas.AssignmentWizard.Controllers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using System.Collections.Generic;
using OSBLE.Models;
using System.Web;
using System.Data.Entity;
using OSBLE.Controllers;

namespace OSBLE.UnitTests.Areas.AssignmentWizard.Controllers
{
    
    
    /// <summary>
    ///This is a test class for BasicsControllerTest and is intended
    ///to contain all BasicsControllerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class BasicsControllerTest
    {


        private TestContext testContextInstance;

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

        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
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

        [TestMethod()]
        public void InsertTest()
        {
            BasicsController controller = new BasicsController();
            Assignment someAssignment = new Assignment()
            {
                AssignmentDescription = "asdf",
                Type = AssignmentTypes.Basic,
                AssignmentName = "foo",
                CategoryID = 8
            };
            controller.Index(someAssignment);

            //if it worked, then the ID should be non-zero
            Assert.AreNotEqual(0, someAssignment.ID);
        }
    }
}
