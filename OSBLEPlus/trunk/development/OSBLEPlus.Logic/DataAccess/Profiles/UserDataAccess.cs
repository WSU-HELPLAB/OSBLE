using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Dapper;

using OSBLE.Models.Users;
using OSBLEPlus.Logic.DomainObjects.Interfaces;
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
                return connection.Query<User>(UserQuery.SelectByUserId,
                                                            new { Id = id }).FirstOrDefault();
            }
        }
        public static IUser GetByName(string userName)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                return connection.Query<User>(UserQuery.SelectByUserName,
                                                            new { UserName = userName }).FirstOrDefault();
            }
        }

        public static bool ValidateUser(string userName, string password)
        {
            return UserProfile.ValidateUser(userName, password);
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
    }
}
