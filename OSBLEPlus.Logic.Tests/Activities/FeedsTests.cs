using System;
using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLEPlus.Logic.Tests.Activities
{
    [TestClass]
    public class FeedsTests
    {
        [TestMethod]
        public void TestGet()
        {
            var results = Feeds.Get(
                                    new DateTime(2014, 1, 1),
                                    new DateTime(2014, 2, 1),
                                    null,
                                    null,
                                    null,
                                    new List<int> {1, 2},
                                    null,
                                    null,
                                    null,
                                    null,
                                    20);

            Assert.IsNotNull(results);
            Assert.IsTrue(results.GetType() == typeof(List<FeedItem>));
        }
    }
}
