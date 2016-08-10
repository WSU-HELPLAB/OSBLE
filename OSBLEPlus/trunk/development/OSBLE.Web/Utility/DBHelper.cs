using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Dapper;
using OSBLEPlus.Logic.Utility;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Web.UI.WebControls;
using System.Net.Mail;
using OSBLE.Models.Courses;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Models.HomePage;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.Queries;
using OSBLEPlus.Logic.Utility.Lookups;

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
        public static UserProfile GetUserProfile(int id, SqlConnection connection = null, bool includeProfilePic = false)
        {
            UserProfile profile = null;
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { profile = GetUserProfile(id, sqlc, includeProfilePic); }
                return profile;
            }

            profile = connection.Query<UserProfile>("SELECT * FROM UserProfiles WHERE ID = @uid",
                new { uid = id }).SingleOrDefault();

            return profile;
        }

        public static ProfileImage GetUserProfileImage(int id, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetUserProfileImage(id, sqlc); }
            }

            try
            {
                return connection.Query<ProfileImage>("SELECT * FROM ProfileImages WHERE UserID = @uid",
                    new { uid = id }).Single();
            }
            catch
            {
                return null;
            }
        }

        public static void SetUserProfileImage(int id, byte[] pic,  SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { SetUserProfileImage(id, pic, sqlc); }
                return;
            }

            // check if we need to insert or update
            if (GetUserProfileImage(id, connection) == null) // insert
            {
                connection.Execute(@"INSERT ProfileImages(UserID, Picture) VALUES (@uid, @picture)",
                    new { uid = id, picture = pic });
            }
            else // update
            {
                connection.Execute(@"UPDATE ProfileImages SET Picture = @picture WHERE UserID =  @uid",
                    new { uid = id, picture = pic });
            }
        }

        public static CourseUser GetCourseUserFromProfileAndCourse(int userProfileID, int courseID, SqlConnection connection = null)
        {
            CourseUser cu = null;
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { cu = GetCourseUserFromProfileAndCourse(userProfileID, courseID, sqlc); }
                return cu;
            }

            cu = connection.Query<CourseUser>("SELECT * FROM CourseUsers WHERE UserProfileID = @uid AND AbstractCourseID = @cid",
                new { uid = userProfileID, cid = courseID }).FirstOrDefault();

            if (cu != null)
            {
                // Get non-basic data
                AbstractRole role = GetAbstractRole(cu.AbstractRoleID, connection);
                AbstractCourse course = GetAbstractCourse(courseID, connection);
                cu.AbstractRole = role;
                cu.AbstractCourse = course;
            }

            return cu;
        }

        public static List<UserProfile> GetUserProfilesForCourse(int courseId)
        {
            var currentUsers = new List<UserProfile>();
            using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {
                sqlConnection.Open();
                
                string query = "SELECT UserProfiles.ID, UserProfiles.FirstName, UserProfiles.LastName " +
                               "FROM UserProfiles " +
                               "INNER JOIN CourseUsers " +
                               "ON UserProfiles.ID = CourseUsers.UserProfileID " +
                               "WHERE CourseUsers.AbstractCourseID = @courseId ";
                
                currentUsers = sqlConnection.Query<UserProfile>(query, new { courseId = courseId }).ToList();

                sqlConnection.Close();
            }
            return currentUsers;
        }

        public static AbstractRole GetAbstractRole(int roleID, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetAbstractRole(roleID, sqlc); }
            }

            string discriminator = connection.Query<string>("SELECT Discriminator FROM AbstractRoles WHERE ID = @id", new { id = roleID }).Single();
            AbstractRole role = null;
            switch(discriminator)
            {
                case "CourseRole":
                    role = connection.Query<CourseRole>("SELECT * FROM AbstractRoles WHERE ID = @id", new { id = roleID }).Single();
                    break;
                case "CommunityRole":
                    role = connection.Query<CommunityRole>("SELECT * FROM AbstractRoles WHERE ID = @id", new { id = roleID }).Single();
                    break;
                case "AssessmentCommitteeChairRole":
                    role = connection.Query<AssessmentCommitteeChairRole>("SELECT * FROM AbstractRoles WHERE ID = @id", new { id = roleID }).Single();
                    break;
                case "AssessmentCommitteeMemberRole":
                    role = connection.Query<AssessmentCommitteeMemberRole>("SELECT * FROM AbstractRoles WHERE ID = @id", new { id = roleID }).Single();
                    break;
                case "ABETEvaluatorRole":
                    role = connection.Query<ABETEvaluatorRole>("SELECT * FROM AbstractRoles WHERE ID = @id", new { id = roleID }).Single();
                    break;
            }

            return role;
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

        public static int GetUserProfileIndexForName(string firstName, string lastName, SqlConnection connection = null)
        {
            int index = -1;
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    index = GetUserProfileIndexForName(firstName, lastName, sqlc);
                }
                return index;
            }

            index = connection.Query<int>("SELECT Id FROM UserProfiles WHERE FirstName = @FirstName AND LastName = @LastName",
                new { FirstName = firstName, LastName = lastName }).FirstOrDefault();

            return index;
        }

        public static List<int> GetCourseInstructorIds(int courseId, SqlConnection connection = null)
        {            
            List<int> courseInstructorIds = new List<int>();
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    courseInstructorIds = GetCourseInstructorIds(courseId, sqlc);
                }
                return courseInstructorIds;
            }

            courseInstructorIds = connection.Query<int>("SELECT UserProfileID FROM CourseUsers WHERE AbstractRoleID = @abstractRoleId AND AbstractCourseID = @courseId ",
                new { abstractRoleId = (int) CourseRole.CourseRoles.Instructor , courseId = courseId }).ToList();

            return courseInstructorIds;
        }

        public static List<int> GetCourseTAIds(int courseId, SqlConnection connection = null)
        {            
            List<int> courseTAIds = new List<int>();
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    courseTAIds = GetCourseTAIds(courseId, sqlc);
                }
                return courseTAIds;
            }

            courseTAIds = connection.Query<int>("SELECT UserProfileID FROM CourseUsers WHERE AbstractRoleID = @abstractRoleId AND AbstractCourseID = @courseId ",
                new { abstractRoleId = (int)CourseRole.CourseRoles.TA, courseId = courseId }).ToList();

            return courseTAIds;
        }

        public static string GetEventLogVisibilityGroups(int eventLogId, SqlConnection connection = null)
        {
            string eventVisibilityGroups = "";
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    eventVisibilityGroups = GetEventLogVisibilityGroups(eventLogId, sqlc);
                }
                return eventVisibilityGroups;
            }

            eventVisibilityGroups = connection.Query<string>("SELECT ISNULL(EventVisibilityGroups, '') FROM EventLogs WHERE Id = @eventLogId ",
                new { eventLogId = eventLogId }).Single();

            return eventVisibilityGroups;
        }

        public static Dictionary<int, string> GetMailAddressUserId(List<string> emailAddresses, SqlConnection connection = null)
        {
            Dictionary<int, string> UserIdEmailPair = new Dictionary<int, string>();
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    UserIdEmailPair = GetMailAddressUserId(emailAddresses, sqlc);
                }
                return UserIdEmailPair;
            }
            
            var result = connection.Query("SELECT ID, UserName FROM UserProfiles WHERE UserName IN @emailAddresses ",
                new { emailAddresses = emailAddresses }).ToList();

            foreach (var item in result)
            {
                UserIdEmailPair.Add(item.ID, item.UserName);
            }

            return UserIdEmailPair;
        }

        #endregion


        /*** Courses & Communities *****************************************************************************************/
        #region Courses
        public static AbstractCourse GetAbstractCourse(int courseID, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetAbstractCourse(courseID, sqlc); }
            }

            string discriminator = connection.Query<string>(@"SELECT Discriminator FROM AbstractCourses WHERE ID = @id", new { id = courseID }).SingleOrDefault();
            AbstractCourse course = null;
            switch(discriminator)
            {
                case "Course":
                    course = connection.Query<Course>(@"SELECT * FROM AbstractCourses WHERE ID = @id", new { id = courseID }).SingleOrDefault();
                    break;
                case "Community":
                    course = connection.Query<Community>(@"SELECT * FROM AbstractCourses WHERE ID = @id", new { id = courseID }).SingleOrDefault();
                    break;
            }

            return course;
        }

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
                var result = connection.Query<dynamic>("SELECT Prefix, Nickname, Number FROM AbstractCourses WHERE ID = @id", new { id = courseID }).SingleOrDefault();
                if (!String.IsNullOrEmpty(result.Prefix) && !String.IsNullOrEmpty(result.Number))
                    name = result.Prefix + " " + result.Number;
                else
                    name = result.Nickname;
            }

            return name;
        }

        public static DateTime GetCourseStart(int courseID, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetCourseStart(courseID, sqlc); }
            }

            return connection.Query<DateTime>("SELECT StartDate FROM AbstractCourses WHERE ID = @id", new { id = courseID }).SingleOrDefault();
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
                courses = connection.Query<CourseUser>(
                     "SELECT * " +
                    "FROM CourseUsers cusers " +
                    "INNER JOIN AbstractCourses acourses " +
                    "ON cusers.AbstractCourseID = acourses.ID " +
                    "WHERE UserProfileID = @uid " +
                    "AND AbstractRoleID IN @rids " +
                    "AND Hidden = 0 " +
                    "AND IsDeleted = 0 ",
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
                communities = connection.Query<CourseUser>(
                    "SELECT * " +
                    "FROM CourseUsers cusers " +
                    "INNER JOIN AbstractCourses acourses " +
                    "ON cusers.AbstractCourseID = acourses.ID " +
                    "WHERE UserProfileID = @uid " +
                    "AND AbstractRoleID IN @rids " +
                    "AND Hidden = 0 " +
                    "AND IsDeleted = 0 ",
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
                var result = connection.Query<dynamic>("SELECT Name, Prefix, Number, Semester, Year, Inactive, Nickname, Discriminator FROM AbstractCourses WHERE ID = @id",
                    new { id = cu.AbstractCourseID }).SingleOrDefault();

                if (result.Discriminator == "Course")
                    name = string.Format("{0} {1} - {2}, {3}, {4}", result.Prefix, result.Number, result.Name, result.Semester, result.Year);
                else
                    name = string.Format("{0} - {1}", result.Nickname, result.Name);

                // tack role on to the end
                name += " (" + GetAbstractRoleNameFromID(cu.AbstractRoleID, connection) + ")";
                
                if (null != result.Inactive && result.Inactive)
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

        public static bool AssignmentDueDatePast(int assignmentId, int abstractCourseId, SqlConnection connection = null)
        {
            if (assignmentId < 1) return false;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    return AssignmentDueDatePast(assignmentId, abstractCourseId, sqlc);
                }
            }

            Assignment a = connection.Query<Assignment>("SELECT * " +
                                                        "FROM Assignments " +
                                                        "WHERE ID = @assignmentId", new {assignmentId}).FirstOrDefault();
            
            // if assignment is not found, or the current course user is not in the class, return false
            if (a == null || a.CourseID != abstractCourseId) return false;

            DateTime checkDateWithLateHours = a.DueTime.AddHours(a.HoursLateWindow);

            return (DateTime.UtcNow >= checkDateWithLateHours);
        }

        public static DateTime? AssignmentDueDateWithLateHoursInCourseTime(int assignmentId, int abstractCourseId,
            SqlConnection connection = null)
        {
            if (assignmentId < 1) return null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    return AssignmentDueDateWithLateHoursInCourseTime(assignmentId, abstractCourseId, sqlc);
                }
            }

            Assignment a = connection.Query<Assignment>("SELECT * " +
                                                        "FROM Assignments " +
                                                        "WHERE ID = @assignmentId", new { assignmentId }).FirstOrDefault();

            // if assignment is not found, or the current course user is not in the class, return false
            if (a == null || a.CourseID != abstractCourseId) return null;

            DateTime utcTime = a.DueTime.AddHours(a.HoursLateWindow);

            return utcTime.UTCToCourse(abstractCourseId);
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
                
        public static bool IsCourse(int courseId)
        {
            AbstractCourse course = GetAbstractCourse(courseId);

            if (course is Course)
            {
                return true;
            }
            return false;
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

            events = connection.Query<Event>("SELECT e.* FROM Events e INNER JOIN CourseUsers cu ON e.PosterID = cu.ID WHERE cu.AbstractCourseID = @cid AND e.StartDate >= @sd AND ( e.EndDate <= @ed OR e.EndDate is Null )AND e.Approved = '1'",
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

            evt = connection.Query<ActivityEvent>("SELECT Id AS EventLogId, EventTypeId, EventDate, DateReceived, SenderId, CourseId, SolutionName, IsDeleted " +
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

            List<int> eventLogIds = comments.Select(i => i.EventLogId).ToList();

            // get logCommentEventIds for HelpfulMarks
            List<int> logIds = new List<int>();

            foreach (int id in eventLogIds)
            {
                logIds.AddRange(connection.Query<int>("SELECT Id " +
                                                      "FROM LogCommentEvents " +
                                                      "WHERE EventLogId = @id", new{id}).ToList());
            }

            List<int> helpfulMarkLogIds = new List<int>();

            // Get HelpfulMarks EventLogIds
            foreach (int id in logIds)
            {
                helpfulMarkLogIds.AddRange(connection.Query<int>("SELECT EventLogId " +
                                                                 "FROM HelpfulMarkGivenEvents " +
                                                                 "WHERE LogCommentEventId = @id", new{id}).ToList());
            }

            // need to get all logIds from comments then markhelpful delete as well
            
                          //connection.Query<int>("SELECT EventLogId " +
                          //"FROM HelpfulMarkGivenEvents " +
                          //"WHERE LogCommentEventId = @logIds", logIds).ToList();

            
            if (comments.Count > 0)
            {
                // soft delete eventlogs associated with LogComments
                connection.Execute("UPDATE EventLogs " +
                                   "SET IsDeleted = 1" +
                                   "WHERE Id = @EventLogId", comments);
            }
            
            // soft delete eventlog for feedpost
            connection.Execute("UPDATE EventLogs " +
                   "SET IsDeleted = 1 " +
                   "WHERE Id = @EventLogId", new { EventLogId = eventId });

            // soft delete MarkHelpfulComment EventLog
            foreach (int id in helpfulMarkLogIds)
            {
                connection.Execute("UPDATE EventLogs " +
                                  "SET IsDeleted = 1 " +
                                  "WHERE Id = @EventLogId", new { EventLogId = id });               
            }

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
            int logId = connection.Query<int>("Select Id " +
                                           "FROM LogCommentEvents " +
                                           "WHERE EventLogId = @eventId", new {eventId}).SingleOrDefault();

            List<int> helpfulMarkLogIds = 
                connection.Query<int>("SELECT EventLogId " +
                                      "FROM HelpfulMarkGivenEvents " +
                                      "WHERE LogCommentEventId = @logId", new { logId }).ToList();
                

            // soft delete eventlog
            if (l != null)
            {
                connection.Execute("UPDATE EventLogs " +
                                   "SET IsDeleted = 1 " +
                                   "WHERE Id = @EventLogId", l);

                connection.Execute("UPDATE EventLogs " +
                                   "SET IsDeleted = 1 " +
                                   "WHERE Id = @helpfulMarkLogIds", new {helpfulMarkLogIds});
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

            // get the Log Comment ID
            int logId = connection.Query<int>("SELECT Id " +
                                              "FROM LogCommentEvents " +
                                              "WHERE EventLogId = @eventId", new { eventId = eventLogId }).FirstOrDefault();

            int logSenderId = connection.Query<int>("SELECT SenderId " +
                                                    "FROM EventLogs " +
                                                    "WHERE Id = @logCommentId ", new {logCommentId = logId}).FirstOrDefault();

            // do not allow user to possibly mark their own comment as helpful
            if (logSenderId == markerId)
            {
                return helpfulMarkEventIds.Count;
            }

            // get the Source Event Log ID
            int sourceLogId = connection.Query<int>("SELECT SourceEventLogId " +
                                                    "From LogCommentEvents " +
                                                    "WHERE EventLogId = @eventId", new { eventId = eventLogId }).FirstOrDefault();

            // get the Source Event Course ID
            int? sourceCourseId = null;

            try
            {
                sourceCourseId = connection.Query<int>("SELECT CourseId " +
                                                       "FROM EventLogs " +
                                                       "WHERE Id = @SourceEventId", new {SourceEventId = sourceLogId}).FirstOrDefault();
            }
            catch (NullReferenceException ex)
            {
                // do nothing
            }

            List<ActivityEvent> eventLogs =
                connection.Query<ActivityEvent>("SELECT Id AS EventLogId, EventTypeId AS EventId, EventDate, DateReceived, SenderId, CourseId, SolutionName, IsDeleted " + 
                                                "FROM EventLogs " +
                                                "WHERE Id IN @logId", new {logId = helpfulMarkEventIds}).ToList();



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
                HelpfulMarkGivenEvent h;
                if (sourceCourseId == null)
                {
                    h = new HelpfulMarkGivenEvent()
                    {
                        LogCommentEventId = logId,
                        SenderId = markerId,
                        SolutionName = ""
                    };
                }
                else
                {
                    h = new HelpfulMarkGivenEvent()
                    {
                        LogCommentEventId = logId,
                        SenderId = markerId,
                        CourseId = sourceCourseId,
                        SolutionName = ""
                    };
                }


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
                                              "WHERE EventLogId = @logCommentId", new { logCommentId }).SingleOrDefault();

            // find eventlogids
            List<int> helpEvents = connection.Query<int>("SELECT EventLogId " +
                                                         "FROM HelpfulMarkGivenEvents " +
                                                         "WHERE LogCommentEventId = @logId", new { logId }).ToList();

            // find the senders
            List<int> senders = connection.Query<int>("SELECT SenderId " +
                                                   "FROM EventLogs " +
                                                   "WHERE Id IN @helpEvents", new { helpEvents }).ToList();

            return senders.Contains(userId);

        }

        public static Dictionary<int, bool> DictionaryOfMarkedLogs(int userId, List<int> logCommentIds, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    return DictionaryOfMarkedLogs(userId, logCommentIds, sqlc);
                }
            }

            Dictionary<int, bool> senderMarkedComment = new Dictionary<int, bool>();
            // find the log id
            List<int> logIds = connection.Query<int>("SELECT Id " +
                                                     "FROM LogCommentEvents " +
                                                     "WHERE EventLogId IN @logCommentIds", new {logCommentIds}).ToList();

            
            foreach (int logId in logIds)
            {
                // find eventlogids
                List<int> helpEvents = connection.Query<int>("SELECT EventLogId " +
                                             "FROM HelpfulMarkGivenEvents " +
                                             "WHERE LogCommentEventId = @logId", new { logId }).ToList();

                // find the senders
                List<int> senders = connection.Query<int>("SELECT SenderId " +
                                                       "FROM EventLogs " +
                                                       "WHERE Id IN @helpEvents", new { helpEvents }).ToList();

                if (senders.Contains(userId))
                {
                    senderMarkedComment[logId] = true;
                }
                else
                {
                    senderMarkedComment[logId] = false;
                }


            }

            return senderMarkedComment;
        }

        public static IEnumerable<ActivityEvent> GetActivityEventsFromId(int userId, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    return GetActivityEventsFromId(userId, sqlc);
                }
            }

            return connection.Query<ActivityEvent>("SELECT Id AS EventLogId, EventTypeId AS EventId, EventDate, DateReceived, SenderId, CourseId, SolutionName, IsDeleted " +
                                                "FROM EventLogs " +
                                                "WHERE SenderId = @id " +
                                                "AND (EventTypeId = 1 OR " +    //AskForHelp
                                                "EventTypeId = 7 OR " +          //FeedPost
                                                "EventTypeId = 8 OR " +         //MarkHelpful
                                                "EventTypeId = 9)" +            //LogComment
                                                "AND (IsDeleted = 0 OR IsDeleted IS NULL) ",
                                                new { id = userId }).ToList();
        }

        public static IEnumerable<LogCommentEvent> GetLogCommentEventsFromEventLogIds(IEnumerable<int> ids,
            IEnumerable<ActivityEvent> activityEvents, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    return GetLogCommentEventsFromEventLogIds(ids, activityEvents, sqlc);
                }
            }
            
            LogCommentEvent l = new LogCommentEvent();

            
            return connection.Query<LogCommentEvent>("SELECT * " +
                                                  "FROM LogCommentEvents " +
                                                  "WHERE EventLogId IN @ids", new { ids }).ToList();
        }

        public static IEnumerable<int> GetUserFeedFromId(int userId, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    return GetUserFeedFromId(userId, sqlc);
                }
            }
            
            // AddEventTypeIdes here as necessary using the syntax below if you want addition events
            return GetActivityEventsFromId(userId, connection).ToList().Select(i=>i.EventLogId).ToList();
        }

        public static IEnumerable<LogCommentEvent> GetCommentsForUserID(int userID, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetCommentsForUserID(userID, sqlc); }
            }

            IEnumerable<LogCommentEvent> comments = connection.Query<LogCommentEvent>("SELECT l.* FROM LogCommentEvents l INNER JOIN EventLogs e ON l.EventLogId = e.Id WHERE e.SenderId = @uid ORDER BY e.EventDate DESC",
                new { uid = userID });
            return comments;
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

        public static List<MailAddress> GetActivityFeedForwardedEmails(int courseID, SqlConnection connection = null, bool emailToClass = false)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetActivityFeedForwardedEmails(courseID, sqlc, emailToClass); }
            }

            string query = "";

            if(emailToClass) //email to all course users
            {
                query = "SELECT DISTINCT u.* FROM UserProfiles u INNER JOIN CourseUsers c ON u.ID = c.UserProfileID WHERE c.AbstractCourseID = @id ";
            }
            else //email to just those that have email forwarding enabled
            {
                query = "SELECT DISTINCT u.* FROM UserProfiles u INNER JOIN CourseUsers c ON u.ID = c.UserProfileID WHERE c.AbstractCourseID = @id AND u.EmailAllActivityPosts = 1";
            }

            IEnumerable<UserProfile> users = connection.Query<UserProfile>(query,
                new { id = courseID });

            List<MailAddress> addresses = new List<MailAddress>(users.Select(u => new MailAddress(u.UserName, u.FullName)));
            return addresses;
        }

        public static List<MailAddress> GetReplyForwardedEmails(int originalPostID, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetReplyForwardedEmails(originalPostID, sqlc); }
            }

            IEnumerable<UserProfile> users = connection.Query<UserProfile>(
                "SELECT DISTINCT u.* " +
                "FROM UserProfiles u " +
                "JOIN EventLogs e ON u.ID = e.SenderId " +
                "JOIN LogCommentEvents l ON e.Id = l.EventLogId " +
                "WHERE l.SourceEventLogId = @opID AND u.EmailAllActivityPosts = 1",
                new { opID = originalPostID });

            UserProfile originalPoster = GetFeedItemSender(originalPostID, connection);

            List<MailAddress> addresses = new List<MailAddress>(users.Select(u => new MailAddress(u.UserName, u.FullName)));
            if (originalPoster.EmailAllActivityPosts && !users.Any(u => u.ID == originalPoster.ID))
                addresses.Add(new MailAddress(originalPoster.UserName, originalPoster.FullName));

            return addresses;
        }

        public static UserProfile GetFeedItemSender(int postID, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetFeedItemSender(postID, sqlc); }
            }

            UserProfile sender = connection.Query<UserProfile>(
                "SELECT u.* " +
                "FROM UserProfiles u " +
                "JOIN EventLogs e ON u.ID = e.SenderId " +
                "WHERE e.Id = @id",
                new { id = postID }).SingleOrDefault();

            return sender;
        }

        public static bool IsEventDeleted(int postID, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return IsEventDeleted(postID, sqlc); }
            }

            bool? isDeleted = connection.Query<bool?>("SELECT IsDeleted FROM EventLogs WHERE Id = @id", 
                new { id = postID }).SingleOrDefault();
            return isDeleted.HasValue && isDeleted.Value;
        }

        public static bool AddHashTags(List<string> hashTags)
        {
            // Add each tag in the list to the database (if it doesn't already exist in the database)
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "BEGIN IF NOT EXISTS (SELECT Content FROM HashTags WHERE Content = @Content) BEGIN INSERT INTO HashTags values (@Content) END END";

                    // TODO: optimize to run one query inserting all hashtags
                    foreach (string hashTag in hashTags)
                    {
                        sqlConnection.Query<int>(query, new { Content = hashTag });
                    }
                    
                    sqlConnection.Close();

                    return true;
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                return false; //failure
            }
        }

        public static List<string> GetHashTags()
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "";

                    query = "SELECT DISTINCT Content FROM HashTags";

                    List<string> hashTags = sqlConnection.Query<string>(query).ToList();

                    sqlConnection.Close();

                    return hashTags;
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                return new List<string>(); //failure, return empty list
            }
        }

        #endregion        
    }
}