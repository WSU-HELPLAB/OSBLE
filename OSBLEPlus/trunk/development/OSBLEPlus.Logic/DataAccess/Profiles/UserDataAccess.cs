using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Dapper;
using OSBLE.Interfaces;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DataAccess.Profiles
{
    public static class UserDataAccess
    {
        public static IUser GetById(int id)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                return connection.Query<UserProfile>(UserQuery.SelectByUserId,
                                                            new { Id = id }).FirstOrDefault();
            }
        }
        public static IUser GetByName(string userName)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                return connection.Query<UserProfile>(UserQuery.SelectByUserName,
                                                            new { UserName = userName }).FirstOrDefault();
            }
        }

        public static bool ValidateUser(string userName, string password)
        {
            return UserProfile.ValidateUser(userName, password, true);
        }

        public static int LogUserTransaction(int userId, DateTime activityTime)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                return connection.Execute("dbo.LogUserTransaction", new {UserId = userId, ActivityTime = activityTime},
                    commandType: CommandType.StoredProcedure);
            }
        }

        public static List<ProfileCourse> GetProfileCoursesForUser(int userId, DateTime currentTime)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                return
                    connection.Query<ProfileCourse>("dbo.GetCoursesForUser",
                        new {UserId = userId, CurrentDate = currentTime.Date.AddDays(1).AddSeconds(-1)},
                        commandType: CommandType.StoredProcedure).ToList();
            }
        }

        public static DateTime GetMostRecentSocialActivityForUser(int userId)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var activityTime =
                    connection.Query<DateTime?>("dbo.GetMostRecentSocialActivityForUser",
                        new { UserId = userId },
                        commandType: CommandType.StoredProcedure).SingleOrDefault();

                return activityTime ?? DateTime.MinValue;
            }
        }
    }
}
