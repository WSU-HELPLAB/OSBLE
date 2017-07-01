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

        public static void SetUserProfileImage(int id, byte[] pic, SqlConnection connection = null)
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
            switch (discriminator)
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
                new { UserName = userName }).SingleOrDefault();

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
                new { abstractRoleId = (int)CourseRole.CourseRoles.Instructor, courseId = courseId }).ToList();

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

        public static List<int> GetCourseSectionUserProfileIds(int courseId, int section, SqlConnection connection = null)
        {
            List<int> courseSectionUserProfileIds = new List<int>();

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    courseSectionUserProfileIds = GetCourseSectionUserProfileIds(courseId, section, sqlc);
                }
                return courseSectionUserProfileIds;
            }

            string query = @"SELECT UserProfileID FROM CourseUsers WHERE AbstractCourseID = @courseId AND Section IN (@section, -2) 
                             SELECT UserProfileID, MultiSection FROM CourseUsers WHERE AbstractCourseID = @courseId AND Section = -1 ";
            List<string> multiSection = new List<string>();

            using (var multi = connection.QueryMultiple(query, new { courseId = courseId, section = section }))
            {
                courseSectionUserProfileIds = multi.Read<int>().ToList();
                var sectionList = multi.Read<dynamic>().ToList();

                foreach (dynamic result in sectionList)
                {
                    if (result.MultiSection.Contains(section.ToString()))
                    {
                        courseSectionUserProfileIds.Add(result.UserProfileID);
                    }
                }
            }
            return courseSectionUserProfileIds;
        }

        public static List<int> GetCourseSections(int courseId, SqlConnection connection = null)
        {
            List<int> courseSections = new List<int>();
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    courseSections = GetCourseSections(courseId, sqlc);
                }
                return courseSections;
            }

            var results = connection.Query<dynamic>("SELECT Section, MultiSection FROM CourseUsers WHERE AbstractCourseID = @courseId ",
                new { courseId = courseId });

            foreach (dynamic result in results)
            {
                if (result.Section == -1) //multi-section
                {
                    string multisection = result.MultiSection;
                    List<string> sectionIds = multisection.Split(',').ToList();
                    if (sectionIds.Count() > 1 && !sectionIds.Equals("all")) //just in case...
                    {
                        foreach (string id in sectionIds)
                        {
                            int parsedId = 0;
                            bool parseIdToInt = int.TryParse(id, out parsedId);
                            if (parseIdToInt) //only add ids that were successfully parsed            
                                courseSections.Add(parsedId);
                        }
                    }
                }
                else
                {
                    if (result.Section != -2) //ignore all section users
                    {
                        courseSections.Add(result.Section);
                    }
                }
            }
            return courseSections.Distinct().ToList(); //return distinct list of sections
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

        public static string GetEventLogVisibleToList(int eventLogId, SqlConnection connection = null)
        {
            string eventVisibleList = "";
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    eventVisibleList = GetEventLogVisibleToList(eventLogId, sqlc);
                }
                return eventVisibleList;
            }

            eventVisibleList = connection.Query<string>("SELECT ISNULL(EventVisibleTo, '') FROM EventLogs WHERE Id = @eventLogId ",
                new { eventLogId = eventLogId }).Single();

            return eventVisibleList;
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
            switch (discriminator)
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

        public static bool GetAbstractCourseHideMailValue(int courseID, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetAbstractCourseHideMailValue(courseID, sqlc); }
            }

            bool hideMail = connection.Query<bool>(@"SELECT ISNULL(HideMail, 0) FROM AbstractCourses WHERE ID =  @id", new { id = courseID }).SingleOrDefault();

            return hideMail;
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

        public static IEnumerable<dynamic> GetAllCourseUsersFromCourseId(int courseId, SqlConnection connection = null)
        {
            IEnumerable<dynamic> courseUsers = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { courseUsers = GetAllCourseUsersFromCourseId(courseId, sqlc); }
            }
            else
            {
                courseUsers = connection.Query<dynamic>("SELECT (up.FirstName + ' ' + up.LastName) as 'FullName', up.ID " +
                                                            "FROM CourseUsers cu " +
                                                            "INNER JOIN UserProfiles up " +
                                                            "ON cu.UserProfileID = up.ID " +
                                                            "WHERE AbstractCourseID = @courseId " +
                                                            "ORDER BY FullName",
                                                             new { courseId = courseId });
            }
            return courseUsers;
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
                IEnumerable<int> roleIDs = GetRoleIDsFromDiscriminator("CommunityRole", connection);
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

        public static string GetCourseFullNameFromCourseId(int courseId, SqlConnection connection = null)
        {
            string name = "";

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { name = GetCourseFullNameFromCourseId(courseId, sqlc); }
            }
            else
            {
                var result = connection.Query<dynamic>("SELECT Name, Prefix, Number, Semester, Year, Inactive, Nickname, Discriminator FROM AbstractCourses WHERE ID = @id",
                    new { id = courseId }).SingleOrDefault();

                if (result.Discriminator == "Course")
                    name = string.Format("{0} {1} - {2}, {3}, {4}", result.Prefix, result.Number, result.Name, result.Semester, result.Year);
                else
                    name = string.Format("{0} - {1}", result.Nickname, result.Name);

                // tack role on to the end
                //name += " (" + GetAbstractRoleNameFromID(cu.AbstractRoleID, connection) + ")";

                if (null != result.Inactive && result.Inactive)
                    name += " [INACTIVE]";
            }

            return name;
        }

        public static string GetUserFirstNameFromEventLogId(int eventLogId, SqlConnection connection = null)
        {
            string name = "";

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { name = GetUserFirstNameFromEventLogId(eventLogId, sqlc); }
            }
            else
            {
                string result = connection.Query<string>("SELECT ISNULL(FirstName, 'OSBLE USER') FROM UserProfiles WHERE ID = (SELECT SenderId FROM EventLogs WHERE Id = @eventLogId)",
                    new { eventLogId = eventLogId }).SingleOrDefault();
                return result;
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
                                                        "WHERE ID = @assignmentId", new { assignmentId }).FirstOrDefault();

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

        public static List<int> GetActiveCourseIds(int userProfileId)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "";

                    //we're getting all active course Ids because ask for help events are visible in all courses
                    query = "SELECT DISTINCT ISNULL(ac.ID, 0) " +
                                "FROM AbstractCourses ac " +
                                "INNER JOIN CourseUsers cu " +
                                "ON ac.ID = cu.AbstractCourseID " +
                                "WHERE GETDATE() < ac.EndDate " +
                                "AND ac.Inactive = 0 " +
                                "AND cu.UserProfileID = @userProfileId";

                    List<int> activeCourseIds = sqlConnection.Query<int>(query, new { userProfileId = userProfileId }).ToList();

                    sqlConnection.Close();

                    return activeCourseIds;
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                return new List<int>(); //failure, return empty list
            }
        }

        public static List<int> GetAllUserCourseIds(int userProfileId)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "";

                    //we're getting all active course Ids because ask for help events are visible in all courses
                    query = "SELECT DISTINCT ISNULL(ac.ID, 0) " +
                                "FROM AbstractCourses ac " +
                                "INNER JOIN CourseUsers cu " +
                                "ON ac.ID = cu.AbstractCourseID " +
                                "AND ac.Inactive = 0 " +
                                "AND cu.UserProfileID = @userProfileId";

                    List<int> activeCourseIds = sqlConnection.Query<int>(query, new { userProfileId = userProfileId }).ToList();

                    sqlConnection.Close();

                    return activeCourseIds;
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                return new List<int>(); //failure, return empty list
            }
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

            evt = connection.Query<ActivityEvent>("SELECT Id AS EventLogId, EventTypeId, EventDate, DateReceived, SenderId, CourseId, SolutionName, IsDeleted, IsAnonymous " +
                                                  "FROM EventLogs e " +
                                                  "WHERE e.Id = @EventId ",
                new { EventId = eventId }
                ).FirstOrDefault();

            return evt;
        }

        public static AskForHelpEvent GetAskForHelpEvent(int eventId, SqlConnection connection = null)
        {
            AskForHelpEvent evt = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    evt = GetAskForHelpEvent(eventId, sqlc);
                }

                return evt;
            }

            evt = connection.Query<AskForHelpEvent>("SELECT * " +
                                      "FROM AskForHelpEvents e " +
                                      "WHERE e.EventLogId = @EventId ",
                    new { EventId = eventId }
                    ).FirstOrDefault();

            return evt;
        }

        public static ExceptionEvent GetExceptionEvent(int eventId, SqlConnection connection = null)
        {
            ExceptionEvent evt = null;

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    evt = GetExceptionEvent(eventId, sqlc);
                }

                return evt;
            }

            evt = connection.Query<ExceptionEvent>("SELECT * " +
                                      "FROM ExceptionEvents e " +
                                      "WHERE e.EventLogId = @EventId ",
                    new { EventId = eventId }
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
                                                      "WHERE EventLogId = @id", new { id }).ToList());
            }

            List<int> helpfulMarkLogIds = new List<int>();

            // Get HelpfulMarks EventLogIds
            foreach (int id in logIds)
            {
                helpfulMarkLogIds.AddRange(connection.Query<int>("SELECT EventLogId " +
                                                                 "FROM HelpfulMarkGivenEvents " +
                                                                 "WHERE LogCommentEventId = @id", new { id }).ToList());
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
                                           "WHERE EventLogId = @eventId", new { eventId }).SingleOrDefault();

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
                                   "WHERE Id = @helpfulMarkLogIds", new { helpfulMarkLogIds });
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
                                   "WHERE EventLogId = @EventLogId ", new { Content = newText, EventLogId = eventId });
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
                                                    "WHERE Id = @logCommentId ", new { logCommentId = logId }).FirstOrDefault();

            // do not allow user to possibly mark their own comment as helpful
            if (logSenderId == markerId)
            {
                return helpfulMarkEventIds.Count;
            }

            // get the Source Event Log ID
            int sourceLogId = connection.Query<int>("SELECT SourceEventLogId " +
                                                    "From LogCommentEvents " +
                                                    "WHERE EventLogId = @eventId", new { eventId = eventLogId }).FirstOrDefault();
            // get the Source Event Log visibility groups
            string sourceLogEventVisibilityGroups = connection.Query<string>("SELECT ISNULL(EventVisibilityGroups, '') " +
                                                    "From EventLogs " +
                                                    "WHERE Id = @eventId", new { eventId = sourceLogId }).FirstOrDefault();
            // get the Source Event Log visible to list
            string sourceLogEventVisibleTo = connection.Query<string>("SELECT ISNULL(EventVisibleTo, '') " +
                                                    "From EventLogs " +
                                                    "WHERE Id = @eventId", new { eventId = sourceLogId }).FirstOrDefault();

            // get the Source Event Course ID
            int? sourceCourseId = null;

            try
            {
                sourceCourseId = connection.Query<int>("SELECT CourseId " +
                                                       "FROM EventLogs " +
                                                       "WHERE Id = @SourceEventId", new { SourceEventId = sourceLogId }).FirstOrDefault();
            }
            catch (NullReferenceException ex)
            {
                // do nothing
            }

            List<ActivityEvent> eventLogs =
                connection.Query<ActivityEvent>("SELECT Id AS EventLogId, EventTypeId AS EventId, EventDate, DateReceived, SenderId, CourseId, SolutionName, IsDeleted " +
                                                "FROM EventLogs " +
                                                "WHERE Id IN @logId", new { logId = helpfulMarkEventIds }).ToList();



            ActivityEvent senderEvent = eventLogs.Find(x => x.SenderId == markerId);


            // user clicked again, remove the MarkHelpfulCommentEvent
            if (senderEvent != null)
            {
                connection.Execute("DELETE FROM HelpfulMarkGivenEvents " +
                                   "WHERE EventLogId = @senderEventId", new { senderEventId = senderEvent.EventLogId });

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
                        SolutionName = "",
                        EventVisibilityGroups = sourceLogEventVisibilityGroups,
                        EventVisibleTo = sourceLogEventVisibleTo
                    };
                }
                else
                {
                    h = new HelpfulMarkGivenEvent()
                    {
                        LogCommentEventId = logId,
                        SenderId = markerId,
                        CourseId = sourceCourseId,
                        SolutionName = "",
                        EventVisibilityGroups = sourceLogEventVisibilityGroups,
                        EventVisibleTo = sourceLogEventVisibleTo
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
            catch (Exception ex)
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
                                         "WHERE EventLogId = @helpfulMarkId)", new { helpfulMarkId }).SingleOrDefault();
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
                                                     "WHERE EventLogId IN @logCommentIds", new { logCommentIds }).ToList();


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
            return GetActivityEventsFromId(userId, connection).ToList().Select(i => i.EventLogId).ToList();
        }

        public static IEnumerable<LogCommentEvent> GetCommentsForUserID(int userID, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetCommentsForUserID(userID, sqlc); }
            }

            IEnumerable<LogCommentEvent> comments = connection.Query<LogCommentEvent>("SELECT * FROM LogCommentEvents l INNER JOIN EventLogs e ON l.EventLogId = e.Id WHERE (IsDeleted IS NULL OR IsDeleted = 0) AND e.SenderId = @uid ORDER BY e.EventDate DESC",
                new { uid = userID });
            return comments;
        }

        public static bool LastSubmitGreaterThanMinutesInterval(int eventLogId, int minutes, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return LastSubmitGreaterThanMinutesInterval(eventLogId, minutes, sqlc); }
            }

            bool greaterThanInterval = false;

            try
            {
                DateTime newSubmissionTimestamp = connection.Query<DateTime>("SELECT EventDate FROM EventLogs WHERE Id = @eventLogId ",
                                                                    new { eventLogId = eventLogId }).Single();

                DateTime mostRecentSubmissionTimestamp = connection.Query<DateTime>("SELECT TOP 1 EventDate " +
                                                                                        "FROM EventLogs " +
                                                                                        "WHERE EventTypeId = 11 " + //11 is submit event
                                                                                        "AND SenderId = (SELECT SenderId " +
                                                                                                        "FROM EventLogs " +
                                                                                                        "WHERE Id = @eventLogId) " +
                                                                                        "AND CourseId = (SELECT CourseId " +
                                                                                                        "FROM EventLogs " +
                                                                                                        "WHERE Id = @eventLogId) " +
                                                                                        "AND Id != @eventLogId " +
                                                                                        "ORDER BY Id DESC ",
                                                                        new { eventLogId = eventLogId }).Single();

                TimeSpan difference = newSubmissionTimestamp - mostRecentSubmissionTimestamp;
                if (difference.Minutes > minutes)
                {
                    greaterThanInterval = true;
                }
                return greaterThanInterval;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion


        /*** Activity Feed *************************************************************************************************/
        #region ActivityFeed
        public static bool InsertActivityFeedComment(int logID, int senderID, string text, SqlConnection connection = null, bool isAnonymous = false)
        {
            if (connection == null)
            {
                bool result = false;
                using (SqlConnection sqlc = GetNewConnection()) { result = InsertActivityFeedComment(logID, senderID, text, sqlc, isAnonymous); }
                return result;
            }

            try
            {
                // Get the course id of the original post
                int? courseID;

                // nested try catch for when commenting on a post that has a NULL courseID
                try
                {
                    courseID = connection.Query<int>("SELECT ISNULL(CourseId, 0) FROM EventLogs WHERE Id = @id",
                        new { id = logID }).SingleOrDefault();
                    if (courseID == 0)
                    {
                        courseID = null;
                    }
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
                    SourceEvent = GetActivityEvent(logID, connection),
                    IsAnonymous = isAnonymous,
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

            if (emailToClass) //email to all course users
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

        public static UserProfile GetFeedItemSender(int postID, SqlConnection connection = null, bool isAnonymous = false)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { return GetFeedItemSender(postID, sqlc, isAnonymous); }
            }

            UserProfile sender = connection.Query<UserProfile>(
            "SELECT u.* " +
            "FROM UserProfiles u " +
            "JOIN EventLogs e ON u.ID = e.SenderId " +
            "WHERE e.Id = @id",
            new { id = postID }).SingleOrDefault();

            if (isAnonymous)
            {
                sender.FirstName = "Anonymous";
                sender.LastName = "User";
            }
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

        /*** Assignments *******************************************************************************************/
        #region Assignments
        public static string GetAssignmentName(int assignmentId = 0, SqlConnection connection = null)
        {
            if (assignmentId == 0)
            {
                return "";
            }

            string assignmentName = "";

            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection())
                {
                    assignmentName = GetAssignmentName(assignmentId, sqlc);
                }
                return assignmentName;
            }

            assignmentName = connection.Query<string>("SELECT ISNULL(AssignmentName, '') FROM Assignments WHERE ID = @assignmentId",
                new { assignmentId = assignmentId }).SingleOrDefault();

            return assignmentName;
        }

        #endregion



        internal static void UpdateEventVisibleToList(int eventLogId, string updatedVisibilityList, SqlConnection connection = null)
        {
            if (connection == null)
            {
                using (SqlConnection sqlc = GetNewConnection()) { UpdateEventVisibleToList(eventLogId, updatedVisibilityList, sqlc); }
            }
            else
            {
                connection.Query("UPDATE EventLogs " +
                                    "SET EventVisibleTo = @updatedVisibilityList, EventVisibilityGroups = 'Selected Users' " +
                                    "WHERE EventLogs.Id = @eventLogId",
                                    new { eventLogId = eventLogId, updatedVisibilityList = updatedVisibilityList });
            }

            return;
        }

        internal static bool InterventionEnabledForCourse(int courseId)
        {
            bool interventionsEnabled = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionsCourses WHERE CourseId = @CourseId ";

                    var result = sqlConnection.Query(query, new { CourseId = courseId }).SingleOrDefault();

                    if (result != null)
                    {
                        interventionsEnabled = result.InterventionsEnabled;
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return interventionsEnabled;
        }

        internal static string GetRoleNameFromCourseAndUserProfileId(int courseId, int userProfileId)
        {
            string roleName = "User";
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT Name FROM AbstractRoles ar INNER JOIN CourseUsers cu ON ar.ID = cu.AbstractRoleID " +
                                   "WHERE cu.AbstractCourseID = @CourseId AND cu.UserProfileID = @UserProfileId ";

                    roleName = sqlConnection.Query<string>(query, new { CourseId = courseId, UserProfileId = userProfileId }).SingleOrDefault();

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                return "User";
            }
            return roleName;
        }

        internal static bool GetGradebookSectionEditableSettings(int courseId)
        {
            bool sectionsEditable = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM GradebookSettings WHERE CourseId = @CourseId ";

                    var result = sqlConnection.Query(query, new { CourseId = courseId }).SingleOrDefault();

                    if (result != null)
                    {
                        sectionsEditable = result.SectionsEditable;
                    }   // else we'll just return false

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                return sectionsEditable;
            }
            return sectionsEditable;
        }

        internal static bool ToggleGradebookSectionsEditable(int courseId)
        {
            bool updateSuccess = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM GradebookSettings WHERE CourseId = @CourseId ";

                    var result = sqlConnection.Query(query, new { CourseId = courseId }).SingleOrDefault();

                    if (result != null) //we found a match, let's update it.
                    {
                        bool sectionsEditable = result.SectionsEditable;

                        string updateQuery = "UPDATE GradebookSettings SET SectionsEditable = @SectionsEditable WHERE CourseId = @CourseId ";
                        updateSuccess = sqlConnection.Execute(updateQuery, new { CourseId = courseId, SectionsEditable = !sectionsEditable }) != 0; //toggle the sectionsEditable value

                    }
                    else //no match, insert a row... set to true because the default will be false
                    {
                        string insertQuery = "INSERT INTO GradebookSettings (CourseId, SectionsEditable) VALUES (@CourseId, 1) ";
                        updateSuccess = sqlConnection.Execute(insertQuery, new { CourseId = courseId }) != 0;
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                return updateSuccess; //failure!
            }
            return updateSuccess;
        }

        internal static bool SetIsProgrammingCourse(int courseId, bool isProgrammingCourse)
        {
            bool updateSuccess = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionsCourses WHERE CourseId = @CourseId ";

                    var result = sqlConnection.Query(query, new { CourseId = courseId }).SingleOrDefault();

                    if (result != null) //we found a match, let's update it if it's different than the current value
                    {
                        string updateQuery = "UPDATE OSBLEInterventionsCourses SET IsProgrammingCourse = @IsProgrammingCourse WHERE CourseId = @CourseId ";
                        updateSuccess = sqlConnection.Execute(updateQuery, new { CourseId = courseId, IsProgrammingCourse = isProgrammingCourse }) != 0; //toggle the sectionsEditable value

                    }
                    else //no match, insert a row, default to interventions disabled
                    {
                        string insertQuery = "INSERT INTO OSBLEInterventionsCourses (CourseId, InterventionsEnabled, IsProgrammingCourse) VALUES (@CourseId, 0, @IsProgrammingCourse) ";
                        updateSuccess = sqlConnection.Execute(insertQuery, new { CourseId = courseId, IsProgrammingCourse = isProgrammingCourse }) != 0;
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                return updateSuccess; //failure!
            }
            return updateSuccess;
        }

        internal static bool GetIsProgrammingCourseSetting(int courseId)
        {
            bool isProgrammingCourse = false;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionsCourses WHERE CourseId = @CourseId ";

                    var result = sqlConnection.Query(query, new { CourseId = courseId }).SingleOrDefault();

                    if (result != null) //we found a match
                    {
                        isProgrammingCourse = result.IsProgrammingCourse;
                    }

                    //else no match, leave it false

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                return isProgrammingCourse; //failure!
            }
            return isProgrammingCourse;
        }

        internal static DateTime GetInterventionLastRefreshTime(int userProfileId)
        {
            DateTime lastRefresh = DateTime.UtcNow;
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();

                    string query = "SELECT * FROM OSBLEInterventionsStatus WHERE UserProfileId = @UserProfileId ";

                    var result = sqlConnection.Query(query, new { UserProfileId = userProfileId }).SingleOrDefault();

                    if (result != null) //we found a match
                    {
                        lastRefresh = result.LastRefresh;
                    }

                    //else no match, leave it time now

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                return lastRefresh; //failure!
            }
            return lastRefresh;
        }

        internal static List<Event> RemoveDuplicateEvents(List<Event> events)
        {
            if (events.Count() == 0)
            {
                return events;
            }

            int posterId = events.First().Poster.ID;
            List<Event> newEvents = new List<Event>(events);

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();
                    string query = "SELECT * FROM Events WHERE PosterID = @PosterId ";
                    var results = sqlConnection.Query(query, new { PosterId = posterId });
                    sqlConnection.Close();

                    if (results == null || results.Count() == 0)
                    {
                        return events;
                    }

                    foreach (var item in results)
                    {
                        foreach (Event newEvent in events)
                        {
                            if (item.StartDate == newEvent.StartDate &&
                                item.EndDate == newEvent.EndDate &&
                                item.Title == newEvent.Title &&
                                item.Description == newEvent.Description)
                            {
                                newEvents.Remove(newEvent);
                                break;   
                            }                            
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to remove duplicate events: ", e);
                return new List<Event>();
            }

            return newEvents;
        }

        internal static bool DeleteCurrentUserEvents(int posterId)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();
                    string query = "DELETE FROM Events WHERE PosterID = @PosterId AND (Description NOT LIKE '\\[url:Assignment Page%' ESCAPE '\\' OR Description IS NULL) ";
                    sqlConnection.Execute(query, new { PosterId = posterId });
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error in DeleteCurrentUserEvents(): ", e);
            }
            return true;
        }



        internal static List<Tuple<string, int, int>> GetPostsAndRepliesCount(int courseId, DateTime startDate, DateTime endDate)
        {
            List<Tuple<string, int, int>> postReplyList = new List<Tuple<string, int, int>>();

            var userProfiles = GetUserProfilesForCourse(courseId);

            startDate = startDate.CourseToUTC(courseId);
            endDate = endDate.CourseToUTC(courseId);

            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();
                    string postsQuery = "SELECT COUNT(*) FROM (SELECT up.FirstName, up.LastName, fpe.EventDate FROM FeedPostEvents fpe " + 
	                                    "LEFT OUTER JOIN EventLogs e ON e.Id = fpe.EventLogId " +
                                        "LEFT OUTER JOIN UserProfiles up ON e.SenderId = up.ID " + 
	                                    "WHERE CourseId = @CourseId AND (IsDeleted IS NULL OR IsDeleted = 0) AND EventVisibilityGroups = 'class' " + 
	                                    "AND SenderId = @UserProfileId AND fpe.EventDate BETWEEN @StartDate AND @EndDate	 " + 
	                                    "GROUP BY up.FirstName, up.LastName, fpe.EventDate, fpe.EventDate) posts ";

                    string askForHelpQuery = "SELECT COUNT(*) FROM (SELECT up.FirstName, up.LastName, afhe.EventDate FROM AskForHelpEvents afhe " +
	                                         "LEFT OUTER JOIN EventLogs e ON e.Id = afhe.EventLogId " +
	                                         "LEFT OUTER JOIN UserProfiles up ON e.SenderId = up.ID " +
                                             "WHERE (IsDeleted IS NULL OR IsDeleted = 0) " +
                                             "AND SenderId IN (SELECT UserProfileID FROM CourseUsers WHERE AbstractRoleID = 3) " +
	                                         "AND afhe.EventDate BETWEEN @StartDate AND @EndDate " +
	                                         "AND up.ID = @UserProfileId " +
	                                         "GROUP BY up.FirstName, up.LastName, afhe.EventDate, afhe.EventDate) askforhelp";

                    string repliesQuery = "SELECT COUNT(*) FROM (SELECT up.FirstName, up.LastName, lce.EventDate FROM LogCommentEvents lce " +
	                                      "LEFT OUTER JOIN EventLogs e ON e.Id = lce.EventLogId " +
	                                      "LEFT OUTER JOIN UserProfiles up ON e.SenderId = up.ID " +
	                                      "WHERE CourseId = @CourseId AND (IsDeleted IS NULL OR IsDeleted = 0) " +
                                          "AND SenderId IN (SELECT UserProfileID FROM CourseUsers WHERE AbstractCourseID = @CourseId AND AbstractRoleID = 3) " +
	                                      "AND lce.EventDate BETWEEN @StartDate AND @EndDate " +
                                          "AND up.ID = @UserProfileId " +
	                                      "GROUP BY up.FirstName, up.LastName, lce.EventDate, lce.EventDate) replies";

                    foreach (var user in userProfiles)
                    {
                        int postResult = sqlConnection.Query<int>(postsQuery, new { CourseId = courseId, UserProfileId = user.ID, StartDate = startDate, EndDate = endDate }).SingleOrDefault();
                        int askForHelptResult = sqlConnection.Query<int>(askForHelpQuery, new { CourseId = courseId, UserProfileId = user.ID, StartDate = startDate, EndDate = endDate }).SingleOrDefault();
                        int repliesResult = sqlConnection.Query<int>(repliesQuery, new { CourseId = courseId, UserProfileId = user.ID, StartDate = startDate, EndDate = endDate }).SingleOrDefault();    
                        postReplyList.Add(new Tuple<string, int, int>(user.FullName, postResult + askForHelptResult, repliesResult));
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error in GetPostsAndRepliesCount(): ", e);
            }
            return postReplyList;
        }

        internal static int GetAssignmentPointTotal(int assignmentID)
        {
            int pointTotal = 0;
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();
                    string query = "SELECT PointSpread FROM Levels WHERE RubricID = (SELECT Top 1 a.RubricID FROM Assignments a WHERE a.ID = @AssignmentId) ";

                    var results = sqlConnection.Query(query, new { AssignmentId = assignmentID}).ToList();
                    foreach (var level in results)
                    {
                        pointTotal += level.PointSpread;
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error in GetPostsAndRepliesCount(): ", e);
            }
            return pointTotal;
        }
    }
}