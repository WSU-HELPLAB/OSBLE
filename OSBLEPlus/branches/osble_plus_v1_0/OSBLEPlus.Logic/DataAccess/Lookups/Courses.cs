using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Dapper;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DataAccess.Lookups
{
    public class Courses
    {
        public static List<NameValuePair> GetCourses()
        {            using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {
                sqlConnection.Open();

                var courses = sqlConnection.Query<NameValuePair>(@"GetCourses", commandType: CommandType.StoredProcedure).ToList();

                sqlConnection.Close();

                courses.Insert(0, new NameValuePair{Value = -1, Name = "Any" });

                return courses;
            }
        }
    }
}
