using System;
using System.Data.SqlClient;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Web.Mvc;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLE;
using OSBLE.Utility;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLE.Web.Tests.ActivityFeed
{
    [TestClass]
    public class CRUDTests
    {
        [TestMethod]
        public void InsertUpdateDelete()
        {
            FeedPostEvent log = new FeedPostEvent()
            {
                SenderId = 1,
                Comment = "This is a test FeedPostEvent.",
                CourseId = 3,
                SolutionName = "TestPost"
            };

            
            using (SqlConnection conn = DBHelper.GetNewConnection())
            {
                //////////////////////////////////////////////////
                // necessary code to insert feed post into the DB
                // this comes from PostFeedItem in the OSBLE.Web
                // FeedController
                //////////////////////////////////////////////////
                string sql = log.GetInsertScripts();
                conn.Execute(sql);

                string feedPostText = "This is a test FeedPostEvent.";
                string findPost = "SELECT * " +
                                  "FROM FeedPostEvents " +
                                  "WHERE Comment = @Text";

                var checkPost = conn.Query<FeedPostEvent>(findPost, new {Text=feedPostText}).FirstOrDefault();

                int eventLogId = checkPost.EventLogId;

                // these two Items should be non-null
                Assert.AreEqual(feedPostText, checkPost.Comment);

                //////////////////////////////////////////////////
                // necessary code to update feed post into the DB
                // this comes from EditFeedItem in the OSBLE.Web
                // FeedController
                //////////////////////////////////////////////////

                string updatePostText = "This is a test update to a FeedPostEvent.";
                DBHelper.EditFeedPost(eventLogId, updatePostText, conn);

                string findUpdate = "SELECT * " +
                                    "FROM FeedPostEvents " +
                                    "WHERE Comment = @Text ";


                checkPost = conn.Query<FeedPostEvent>(findUpdate, new{Text=updatePostText}).FirstOrDefault();

                Assert.AreEqual(checkPost.Comment, updatePostText);

                //////////////////////////////////////////////////
                // necessary code to delete feed post into the DB
                // this comes from DeleteFeedPost in the OSBLE.Web
                // FeedController
                //////////////////////////////////////////////////
                 
                // this is a soft delete, and marks the table entry as isdeleted
                // the sproc will not pull these from the DB, but leave them intact
                // for analytics purposes
                DBHelper.DeleteFeedPostEvent(checkPost.EventLogId, conn);

                string findDeletedItem = "SELECT * " +
                                         "FROM EventLogs " +
                                         "WHERE Id = @EventLogId " +
                                         "AND IsDeleted = 1 ";

                // assuming this item is pulled from the DB, then it is "Deleted"

                ActivityEvent checkEventLog = conn.Query<ActivityEvent>(findDeletedItem, checkPost).FirstOrDefault();

                Assert.AreNotEqual(null, checkEventLog);
            }
        }
    }
}