﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Dapper;

using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interfaces;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DataAccess.Profiles
{
    public class CourseDataAccess
    {
        public static List<SubmisionAssignment> GetAssignmentsForCourse(int courseId, DateTime currenTime)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                using (var multi = connection.QueryMultiple("dbo.GetAssignmentsForCourse",
                        new { CourseId = courseId, CurrentDate = currenTime.Date.AddDays(1).AddSeconds(-1) },
                        commandType: CommandType.StoredProcedure))
                {
                    var assignments = multi.Read<SubmisionAssignment>().ToList();
                    var course = (ICourse) multi.Read<ProfileCourse>().Single();
                    assignments.ForEach(x=> { x.Course = course; });

                    return assignments;
                } 
            }
        }
        public static DateTime? GetLastSubmitDateForAssignment(int assignmentId)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                return
                    connection.Query<DateTime?>("dbo.GetLastSubmitDateForAssignment",
                        new { AssignmentId = assignmentId },
                        commandType: CommandType.StoredProcedure).FirstOrDefault();
            }
        }
    }
}
