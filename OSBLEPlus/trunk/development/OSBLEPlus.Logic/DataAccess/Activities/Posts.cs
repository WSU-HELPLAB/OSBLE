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

                    try
                    {
                        OSBLE.Models.FileSystem.Directories.GetAssignmentWithId(submit.CourseId ?? 1
                            , submit.AssignmentId, teamId).AddFile(string.Format("{0}.zip", submit.Sender.FullName), zipStream);
                    }
                    catch (ZipException)
                    {
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
    }
}
