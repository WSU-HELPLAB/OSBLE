using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLE.Models.Queries;
using OSBLE.Utility;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLEPlus.Logic.Tests.Activities
{
    [TestClass]
    public class GetUserFeedFromId
    {
        [TestMethod]
        public void GetUserFeed()
        {
            // returns aggregate feed items from Bob Smith
            // need to have data in your DB post from bob smith for this to pass
            List<FeedItem> a = ActivityFeedQuery.ProfileQuery(1).ToList();

            Assert.IsNotNull(a);
        }
    }
}
