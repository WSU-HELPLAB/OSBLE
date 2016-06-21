using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Dapper;
using OSBLE.Interfaces;
using OSBLE.Models.Assignments;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DataAccess.Profiles
{
    public class CourseDataAccess
    {
        public static WhatsNewItem GetMostRecentWhatsNewItem(int courseId=0)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                return connection.Query<WhatsNewItem>("dbo.GetMostRecentWhatsNewItem",
                    new {CourseId = courseId},
                    commandType: CommandType.StoredProcedure).SingleOrDefault();
            }
        }

        public static List<SubmisionAssignment> GetAssignmentsForCourse(int courseId, DateTime currentTime)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                using (var multi = connection.QueryMultiple("dbo.GetAssignmentsForCourse",
                        new { CourseId = courseId, CurrentDate = currentTime,
                            AssignType = AssignmentTypes.Basic,
                            FileType=DeliverableType.PluginSubmission },
                        commandType: CommandType.StoredProcedure))
                {
                    var assignments = multi.Read<SubmisionAssignment>().ToList();
                    var course = (ICourse)multi.Read<ProfileCourse>().Single();
                    assignments.ForEach(x => { x.Course = course; });

                    return assignments;
                }
            }
        }

        public static DateTime? GetLastSubmitDateForAssignment(int assignmentId, int userId)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                return
                    connection.Query<DateTime?>("dbo.GetLastSubmitDateForAssignment",
                        new { AssignmentId = assignmentId, UserId = userId},
                        commandType: CommandType.StoredProcedure).FirstOrDefault();
            }
        }

        public static List<StudentData> GetStudentList(int courseId)
        {
            using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {

                return sqlConnection.Query<StudentData>(@"GetStudentList",
                    new
                    {
                        courseId
                    }, commandType: CommandType.StoredProcedure)
                    .ToList();
            }
        } 
    }
}
