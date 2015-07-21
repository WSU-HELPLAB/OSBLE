using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLE.Utility;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using System.Data.SqlClient;

namespace OSBLE.Web.Tests
{
    [TestClass]
    public class DBHelperTests
    {
        SqlConnection con = new SqlConnection(@"data source=localhost\SQLEXPRESS;Database=OSBLEPlus;Trusted_Connection=yes;Persist Security Info=True;");

        [TestMethod]
        public void GetActivityFeedItemsTest()
        {
            int[] ids = {1,2};
            List<FeedItem> items = DBHelper.GetActivityFeedItems(ids, con);
            Assert.AreEqual(ids.Length, items.Count);
        }

    }
}
