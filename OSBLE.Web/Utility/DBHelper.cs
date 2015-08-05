using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dapper;
using OSBLEPlus.Logic.Utility;
using System.Data.SqlClient;
using OSBLE.Models.Courses;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Models.HomePage;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLE.Attributes;

namespace OSBLE.Utility
{
    /// <summary>
    /// This is a class designed to make dapper database calls unified from a central location.
    /// Add any static getter and/or setter for any chunk of data needed from the db here.
    /// Try to follow the standard naming convention used here, as it makes it easier to find
    /// these methods in the future, and prevents duplicate methods.
    /// </summary>
    public static class DBHelper
    {
        public static SqlConnection GetNewConnection()
        {
            return new SqlConnection(StringConstants.ConnectionString);
        }


        /*** Users *********************************************************************************************************/
        #region Users
        public static UserProfile GetUserProfile(int id, SqlConnection connection = null)
        {
            UserProfile profile = null;
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { profile = GetUserProfile(id, sqlc); }
                return profile;
            }

            profile = connection.Query<UserProfile>("SELECT * FROM UserProfiles WHERE ID = @uid",
                new { uid = id }).SingleOrDefault();

            return profile;
        }

        public static CourseUser GetCourseUserFromProfileAndCourse(int userProfileID, int courseID, SqlConnection connection = null)
        {
            CourseUser cu = null;
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { cu = GetCourseUserFromProfileAndCourse(userProfileID, courseID, sqlc); }
                return cu;
            }

            cu = connection.Query<CourseUser>("SELECT * FROM CourseUsers WHERE UserProfileID = @uid AND AbstractCourseID = @courseID",
                new { uid = userProfileID, cid = courseID }).FirstOrDefault();

            return cu;
        }

        public static UserProfile GetUserProfile(string userName, SqlConnection connection = null)
        {
            UserProfile profile = null;
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    profile = GetUserProfile(userName, sqlc);
                }
                return profile;
            }

            profile = connection.Query<UserProfile>("SELECT * FROM UserProfiles WHERE UserName = @UserName",
                new {UserName = userName}).SingleOrDefault();

            return profile;
        }

        #endregion


        /*** Courses & Communities *****************************************************************************************/
        #region Courses
        public static string GetCourseShortNameFromID(int courseID, SqlConnection connection = null)
        {
            string name = "";

            // Set up our connection:
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { name = GetCourseShortNameFromID(courseID, sqlc); }
            }
            else
            {
                var result = connection.Query<dynamic>("SELECT Prefix, Number FROM AbstractCourses WHERE ID = @id", new { id = courseID }).SingleOrDefault();
                if (result != null)
                    name = result.Prefix + " " + result.Number;
            }

            return name;
        }

        public static IEnumerable<CourseUser> GetAllCurrentCourses(int userProfileID, SqlConnection connection = null)
        {
            IEnumerable<CourseUser> currentCourses = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { currentCourses = GetAllCurrentCourses(userProfileID, sqlc); }
            }
            else
            {
                currentCourses = connection.Query<CourseUser>("SELECT * FROM CourseUsers WHERE UserProfileID = @uid",
                    new { uid = userProfileID });
            }

            return currentCourses;
        }

        public static IEnumerable<CourseUser> GetCoursesFromUserProfileID(int userProfileID, SqlConnection connection = null)
        {
            IEnumerable<CourseUser> courses = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { courses = GetCoursesFromUserProfileID(userProfileID, sqlc); }
            }
            else
            {
                IEnumerable<int> roleIDs = GetRoleIDsFromDiscriminator("CourseRole", connection);
                courses = connection.Query<CourseUser>("SELECT * FROM CourseUsers WHERE UserProfileID = @uid AND AbstractRoleID IN @rids",
                    new { uid = userProfileID, rids = roleIDs });
            }

            return courses;
        }

        public static IEnumerable<CourseUser> GetCommunitiesFromUserProfileID(int userProfileID, SqlConnection connection = null)
        {
            IEnumerable<CourseUser> communities = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { communities = GetCommunitiesFromUserProfileID(userProfileID, sqlc); }
            }
            else
            {
                IEnumerable<int> roleIDs = GetRoleIDsFromDiscriminator("CommunityRole",connection);
                communities = connection.Query<CourseUser>("SELECT * FROM CourseUsers WHERE UserProfileID = @uid AND AbstractRoleID IN @rids",
                    new { uid = userProfileID, rids = roleIDs });
            }

            return communities;
        }


        public static string GetCourseFullNameFromCourseUser(CourseUser cu, SqlConnection connection = null)
        {
            string name = "";

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { name = GetCourseFullNameFromCourseUser(cu, sqlc); }
            }
            else
            {
                var result = connection.Query<dynamic>("SELECT Name, Prefix, Number, Semester, Year, Inactive, Discriminator FROM AbstractCourses WHERE ID = @id",
                    new { id = cu.AbstractCourseID }).SingleOrDefault();

                if (result.Discriminator == "Course")
                    name = string.Format("{0} {1} - {2}, {3}, {4}", result.Prefix, result.Number, result.Name, result.Semester, result.Year);
                else
                    name = result.Name; // communities are much simpler

                // tack role on to the end
                name += " (" + GetAbstractRoleNameFromID(cu.AbstractRoleID, connection) + ")";

                if (result.Inactive)
                    name += " [INACTIVE]";
            }
            
            return name;
        }

        public static string GetAbstractRoleNameFromID(int ID, SqlConnection connection = null)
        {
            string name = "";

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { name = GetAbstractRoleNameFromID(ID, sqlc); }
            }
            else
            {
                name = connection.Query<string>("SELECT [Name] FROM AbstractRoles WHERE ID = @id", new { id = ID }).SingleOrDefault();
            }

            return name;
        }

        public static IEnumerable<int> GetRoleIDsFromDiscriminator(string discriminator, SqlConnection connection = null)
        {
            IEnumerable<int> ids = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { ids = GetRoleIDsFromDiscriminator(discriminator, sqlc); }
            }
            else
            {
                ids = connection.Query<int>("SELECT [ID] FROM AbstractRoles WHERE Discriminator = @disc", new { disc = discriminator });
            }

            return ids;
        }
        #endregion


        /*** Discussion Teams **********************************************************************************************/
        #region DiscussionTeams
        public static int GetDiscussionTeamIDFromTeamID(int teamID, SqlConnection connection = null)
        {
            int dtID;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { dtID = GetDiscussionTeamIDFromTeamID(teamID, sqlc); }
            }
            else
            {
                dtID = connection.Query<int>(
                        "SELECT Top 1 [ID] FROM DiscussionTeams WHERE TeamID = @id",
                        new { id = teamID }).SingleOrDefault();
            }

            return dtID;
        }

        public static IEnumerable<DiscussionPost> GetDiscussionPosts(int courseUserID, int discussionTeamID, SqlConnection connection = null)
        {
            IEnumerable<DiscussionPost> dps = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { dps = GetDiscussionPosts(courseUserID, discussionTeamID, sqlc); }
            }
            else
            {
                dps = connection.Query<DiscussionPost>(
                        "SELECT * FROM DiscussionPosts WHERE DiscussionTeamID = @did AND CourseUserID = @cid",
                        new { did = discussionTeamID, cid = courseUserID });
            }

            return dps;
        }

        public static IEnumerable<DiscussionPost> GetInitialDiscussionPosts(int courseUserID, int assignmentID, SqlConnection connection = null)
        {
            IEnumerable<DiscussionPost> dps = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { dps = GetInitialDiscussionPosts(courseUserID, assignmentID, sqlc); }
            }
            else
            {
                dps = connection.Query<DiscussionPost>(
                    "SELECT * FROM DiscussionPosts WHERE CourseUserID = @cid AND AssignmentID = @aid AND ParentPostID IS NULL",
                    new { cid = courseUserID, aid = assignmentID });
            }

            return dps;
        }

        public static void InsertDiscussionPosts(IEnumerable<DiscussionPost> posts, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { InsertDiscussionPosts(posts, sqlc); }
            }
            else
            {
                connection.Execute(
                        "INSERT DiscussionPosts (Posted, CourseUserID, Content, AssignmentID, DiscussionTeamID) VALUES (@Posted, @CourseUserID, @Content, @AssignmentID, @DiscussionTeamID)",
                        posts);
            }
        }
        #endregion


        /*** Events ********************************************************************************************************/
        #region Events
        public static IEnumerable<Event> GetApprovedCourseEvents(int courseID, DateTime start, DateTime end, SqlConnection connection = null)
        {
            IEnumerable<Event> events = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { events = GetApprovedCourseEvents(courseID, start, end, sqlc); }
                return events;
            }

            events = connection.Query<Event>("SELECT e.* FROM Events e INNER JOIN CourseUsers cu ON e.PosterID = cu.ID WHERE cu.AbstractCourseID = @cid AND e.StartDate >= @sd AND e.EndDate <= @ed AND e.Approved = '1'",
                new { cid = courseID, sd = start, ed = end });

            return events;
        }

        /// <summary>
        /// Returns event log from DB
        /// </summary>
        /// <param name="userProfileId"></param>
        /// <param name="courseId"></param>
        /// <param name="eventId"></param>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static ActivityEvent GetActivityEvent(int eventId, SqlConnection connection = null)
        {
            ActivityEvent evt = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    evt = GetActivityEvent(eventId, sqlc);
                }
 
                return evt;
            }

            evt = connection.Query<ActivityEvent>("SELECT * " +
                                                  "FROM EventLogs e " +
                                                  "WHERE e.Id = @EventId ",
                new {EventId = eventId}
                ).FirstOrDefault();

            return evt;
        }

        public static FeedPostEvent GetFeedPostEvent(int eventId, SqlConnection connection = null)
        {
            FeedPostEvent evt = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    evt = GetFeedPostEvent(eventId, sqlc);
                }

                return evt;
            }

            evt = connection.Query<FeedPostEvent>("SELECT * " +
                                      "FROM FeedPostEvents e " +
                                      "WHERE e.EventLogId = @EventId ",
                    new { EventId = eventId }
                    ).FirstOrDefault();

            return evt;
        }

        public static List<LogCommentEvent> GetLogCommentEvents(int eventId, SqlConnection connection = null)
        {
            List<LogCommentEvent> comments = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    comments = GetLogCommentEvents(eventId, sqlc);
                }

                return comments;
            }

            comments = connection.Query<LogCommentEvent>("SELECT * " +
                                "FROM LogCommentEvents l " +
                                "WHERE l.SourceEventLogId = @EventId",
                                new { EventId = eventId }).ToList();

            return comments;
        }

        public static LogCommentEvent GetSingularLogComment(int eventId, SqlConnection connection = null)
        {
            LogCommentEvent comment = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    comment = GetSingularLogComment(eventId, sqlc);
                }

                return comment;
            }

            comment = connection.Query<LogCommentEvent>("SELECT * " +
                                "FROM LogCommentEvents l " +
                                "WHERE l.EventLogId = @EventId",
                                new { EventId = eventId }).FirstOrDefault();

            return comment;
        }

        /// <summary>
        /// Deletes the feed post event, associated eventlog, and asociated LogComments/EventLogs for LogComments
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="connection"></param>
        public static void DeleteFeedPostEvent(int eventId, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    DeleteFeedPostEvent(eventId, sqlc);
                }

                return;
            }

            List<LogCommentEvent> comments = GetLogCommentEvents(eventId, connection);

            //FeedPostEvent fpe = GetFeedPostEvent(eventId, connection);

            
            if (comments.Count > 0)
            {
                // soft delete eventlogs associated with LogComments
                connection.Execute("UPDATE EventLogs " +
                                   "SET IsDeleted = 1" +
                                   "WHERE Id = @EventLogId", comments);
            }
            
            // soft delete eventlog for feedpost
            connection.Execute("UPDATE EventLogs " +
                   "SET IsDeleted = 1" +
                   "WHERE Id = @EventLogId", new { EventLogId = eventId });
        }

        public static void DeleteLogComment(int eventId, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    DeleteLogComment(eventId, sqlc);
                }

                return;
            }

            LogCommentEvent l = GetSingularLogComment(eventId, connection);

            // soft delete eventlog
            if (l != null)
            {
                connection.Execute("UPDATE EventLogs " +
                                   "SET IsDeleted = 1" +
                                   "WHERE Id = @EventLogId", l);
            }

        }

        public static void EditFeedPost(int eventId, string newText, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    EditFeedPost(eventId, newText, sqlc);
                }

                return;
            }

            FeedPostEvent fpe = GetFeedPostEvent(eventId);

            // make sure the event exists and the text was changed
            if (fpe != null && fpe.Comment != newText) 
            {
                connection.Execute("UPDATE FeedPostEvents " +
                                   "SET Comment = @Comment " +
                                   "WHERE EventLogId = @EventLogId ", new { Comment = newText, EventLogId = eventId });
            }


        }

        public static void EditLogComment(int eventId, string newText, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    EditLogComment(eventId, newText, sqlc);
                }

                return;
            }

            LogCommentEvent lce = GetSingularLogComment(eventId);

            // make sure the event exists and the text was changed
            if (lce != null && lce.Content != newText)
            {
                connection.Execute("UPDATE LogCommentEvents " +
                                   "SET Content = @Content " +
                                   "WHERE EventLogId = @EventLogId ", new {Content = newText, EventLogId = eventId});
            }
        }

        public static List<int> GetHelpfulMarksLogIds(int eventLogId, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    return GetHelpfulMarksLogIds(eventLogId, sqlc);
                }
            }

            // get the logCommentId
            int logId = connection.Query<int>("SELECT Id " +
                                              "FROM LogCommentEvents " +
                                              "WHERE EventLogId = @eventId", new { eventId = eventLogId }).FirstOrDefault();

            return connection.Query<int>("SELECT EventLogId " +
                                      "FROM HelpfulMarkGivenEvents " +
                                      "WHERE LogCommentEventId = @logId",
                                      new { logId }).ToList();

        }

        /// <summary>
        /// Inserts HelpfulMarkGivenEvent
        /// </summary>
        /// <returns>Number of helpfulMarks associated with the logComment being marked</returns>
        public static int MarkLogCommentHelpful(int eventLogId, int markerId, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    return MarkLogCommentHelpful(eventLogId, markerId, sqlc);
                }
            }

            List<int> helpfulMarkEventIds = GetHelpfulMarksLogIds(eventLogId, connection);

            // get the logCommentId
            int logId = connection.Query<int>("SELECT Id " +
                                              "FROM LogCommentEvents " +
                                              "WHERE EventLogId = @eventId", new { eventId = eventLogId }).FirstOrDefault();

            List<ActivityEvent> eventLogs =
                connection.Query<ActivityEvent>("SELECT Id AS EventLogId, EventTypeId AS EventId, EventDate, DateReceived, SenderId, CourseId, BatchId, SolutionName, IsDeleted " + 
                                                "FROM EventLogs " +
                                                "WHERE Id = @logId", new {logId = helpfulMarkEventIds}).ToList();

            ActivityEvent senderEvent = eventLogs.Find(x => x.SenderId == markerId);
            
            
            // user clicked again, remove the MarkHelpfulCommentEvent
            if (senderEvent != null)
            {
                connection.Execute("DELETE FROM HelpfulMarkGivenEvents " +
                                   "WHERE EventLogId = @senderEventId", new {senderEventId = senderEvent.EventLogId});

                connection.Execute("DELETE FROM EventLogs " +
                   "WHERE Id = @senderEventId", new { senderEventId = senderEvent.EventLogId });
                // do not execute insert string, just return number of marks
                return helpfulMarkEventIds.Count - 1;
            }

            try
            {
                var h = new HelpfulMarkGivenEvent()
                {
                    LogCommentEventId = logId,
                    SenderId = markerId,
                    SolutionName = ""
                };
                using (var cmd = h.GetInsertCommand())
                {
                    cmd.Connection = connection;
                    connection.Open();
                    cmd.ExecuteScalar();
                    connection.Close();
                    return helpfulMarkEventIds.Count + 1;
                }
            }
            catch(Exception ex)
            {
                return helpfulMarkEventIds.Count;
            }
        }

        public static int GetHelpfulMarkFeedSourceId(int helpfulMarkId, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    return GetHelpfulMarkFeedSourceId(helpfulMarkId, sqlc);
                }
            }
            
            return connection.Query<int>("SELECT SourceEventLogId " +
                                         "FROM LogCommentEvents " +
                                         "WHERE Id = (SELECT LogCommentEventId " +
                                         "FROM HelpfulMarkGivenEvents " +
                                         "WHERE EventLogId = @helpfulMarkId)", new {helpfulMarkId}).SingleOrDefault();
        }

        public static bool UserMarkedLog(int userId, int logCommentId, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    return UserMarkedLog(userId, logCommentId, sqlc);
                }
            }

            // find the log id
            int logId = connection.Query<int>("SELECT Id " +
                                              "FROM LogCommentEvents " +
                                              "WHERE EventLogId = @logCommentId", new {logCommentId}).SingleOrDefault();

            // find eventlogids
            List<int> helpEvents = connection.Query<int>("SELECT EventLogId " +
                                                         "FROM HelpfulMarkGivenEvents " +
                                                         "WHERE LogCommentEventId = @logId", new{logId}).ToList();

            // find the senders
            List<int> senders = connection.Query<int>("SELECT SenderId " +
                                                   "FROM EventLogs " +
                                                   "WHERE Id = @helpEvents", new {helpEvents}).ToList();

            return senders.Contains(userId);

        }

        #endregion


        /*** Activity Feed *************************************************************************************************/
        #region ActivityFeed
        public static bool InsertActivityFeedComment(int logID, int senderID, string text, SqlConnection connection = null)
        {
            if (connection == null)
            {
                bool result = false;
                using (SqlConnection sqlc = GetNewConnection()) { result = InsertActivityFeedComment(logID, senderID, text, sqlc); }
                return result;
            }

            try
            {
                // Get the course id of the original post
                int? courseID;

                // nested try catch for when commenting on a post that has a NULL courseID
                try
                {
                    courseID = connection.Query<int>("SELECT CourseId FROM EventLogs WHERE Id = @id",
                        new {id = logID}).SingleOrDefault();
                }
                catch
                {
                    courseID = null;
                }
                    

                LogCommentEvent e = new LogCommentEvent(DateTime.UtcNow)
                {
                    Content = text,
                    SourceEventLogId = logID,
                    SolutionName = "",
                    SenderId = senderID,
                    CourseId = courseID,
                    SourceEvent = GetActivityEvent(logID, connection)
                };
                using (var cmd = e.GetInsertCommand())
                {
                    // this no longer works, need to do cmd.ExecuteScalar() to get the query to run correctly.
                    //connection.Execute(cmd.CommandText, cmd.Parameters);
                    cmd.Connection = connection;

                    connection.Open();
                    cmd.ExecuteScalar();
                    connection.Close();
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        #endregion
    }
}