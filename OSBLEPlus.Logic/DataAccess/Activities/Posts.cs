using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using Dapper;
using Ionic.Zip;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Helpers;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DataAccess.Activities
{
    public class Posts
    {
        public static int SaveEvent(IActivityEvent activityEvent)
        {
            try
            {
                //execute sql batch insert statements
                using (var connection = new SqlConnection(StringConstants.ConnectionString))
                {
                    using (var cmd = activityEvent.GetInsertCommand())
                    {
                        cmd.Connection = connection;
                        connection.Open();
                        var logId = Convert.ToInt32(cmd.ExecuteScalar());
                        connection.Close();
                        return logId;
                    }
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static void SaveToFileSystem(SubmitEvent submit)
        {
            using (var zipStream = new MemoryStream())
            {
                zipStream.Write(submit.SolutionData, 0, submit.SolutionData.Length);
                zipStream.Position = 0;

                using (
                    var connection = new SqlConnection(StringConstants.ConnectionString))
                {
                    var teamId = connection.Query<int>("dbo.GetAssignmentTeam",
                        new { assignmentId = submit.AssignmentId, userId = submit.SenderId },
                        commandType: CommandType.StoredProcedure).SingleOrDefault();

                    int timezoneOffset = connection.Query<int>("SELECT ISNULL(TimeZoneOffset, -8) FROM AbstractCourses WHERE ID = @courseId ",
                        new { courseId = submit.CourseId }).SingleOrDefault();
                    
                    TimeZoneInfo tzInfo = GetTimeZone(timezoneOffset);
                    DateTime utcKind = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
                    DateTime submitTimeStamp = TimeZoneInfo.ConvertTimeFromUtc(utcKind, tzInfo);

                    try
                    {
                        string submitTimestampInCourseTime = submitTimeStamp.ToString("MM-dd-yyyy-HH-mm-ss");
                        
                        OSBLE.Models.FileSystem.Directories.GetAssignmentWithId(submit.CourseId ?? 1
                            , submit.AssignmentId, teamId).AddFile(string.Format("{0}-{1}.zip", submit.Sender.FullName, submitTimestampInCourseTime), zipStream);                        
                    }
                    catch (ZipException ze)
                    {
                        throw new Exception("SaveToFileSystem() failure...", ze);
                    }
                }
            }
        }

        public static int SubmitLocalErrorLog(LocalErrorLog errorLog)
        {
            try
            {
                //execute sql batch insert statements
                using (var connection = new SqlConnection(StringConstants.ConnectionString))
                {
                    using (var cmd = errorLog.GetInsertCommand())
                    {
                        cmd.Connection = connection;
                        connection.Open();
                        var errLogId = Convert.ToInt32(cmd.ExecuteScalar());
                        connection.Close();
                        return errLogId;
                    }
                }
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static TimeZoneInfo GetTimeZone(int tzoffset)
        {
            string zone = "";
            switch (tzoffset)
            {
                case 0:
                    zone = "Greenwich Standard Time";
                    break;
                case 1:
                    zone = "W. Europe Standard Time";
                    break;
                case 2:
                    zone = "E. Europe Standard Time";
                    break;
                case 3:
                    zone = "Russian Standard Time";
                    break;
                case 4:
                    zone = "Arabian Standard Time";
                    break;
                case 5:
                    zone = "West Asia Standard Time";
                    break;
                case 6:
                    zone = "Central Asia Standard Time";
                    break;
                case 7:
                    zone = "North Asia Standard Time";
                    break;
                case 8:
                    zone = "Taipei Standard Time";
                    break;
                case 9:
                    zone = "Tokyo Standard Time";
                    break;
                case 10:
                    zone = "AUS Eastern Standard Time";
                    break;
                case 11:
                    zone = "Central Pacific Standard Time";
                    break;
                case 12:
                    zone = "New Zealand Standard Time";
                    break;
                case 13:
                    zone = "Tonga Standard Time";
                    break;
                case -1:
                    zone = "Cape Verde Standard Time";
                    break;
                case -2:
                    zone = "Mid-Atlantic Standard Time";
                    break;
                case -3:
                    zone = "E. South America Standard Time";
                    break;
                case -4:
                    zone = "Atlantic Standard Time";
                    break;
                case -5:
                    zone = "Eastern Standard Time";
                    break;
                case -6:
                    zone = "Central Standard Time";
                    break;
                case -7:
                    zone = "Mountain Standard Time";
                    break;
                case -8:
                    zone = "Pacific Standard Time";
                    break;
                case -9:
                    zone = "Alaskan Standard Time";
                    break;
                case -10:
                    zone = "Hawaiian Standard Time";
                    break;
                case -11:
                    zone = "Samoa Standard Time";
                    break;
                case -12:
                    zone = "Dateline Standard Time";
                    break;
                default:
                    zone = "";
                    break;
            }
            TimeZoneInfo tz;
            if (zone != "")
                tz = TimeZoneInfo.FindSystemTimeZoneById(zone);
            else
            {
                //going to assume utc
                tz = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
            }
            return tz;
        }
    }
}
