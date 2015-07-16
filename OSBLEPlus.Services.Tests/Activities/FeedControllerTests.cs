using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Services.Controllers;

namespace OSBLEPlus.Services.Tests.Activities
{
    [TestClass]
    public class FeedControllerTests
    {
        [TestMethod]
        public async Task GetContacts_Should_Return_List_Of_Contacts()
        {
            var results = await new FeedController().Get(
                                    new DateTime(2014, 1, 1),
                                    new DateTime(2014, 2, 1),
                                    null,
                                    new List<int> { 1, 2 },
                                    null,
                                    null,
                                    null,
                                    null,
                                    20);

            Assert.IsTrue(results != null);
        }
    }
}
