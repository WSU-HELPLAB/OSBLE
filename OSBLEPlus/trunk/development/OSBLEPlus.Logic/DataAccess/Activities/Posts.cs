using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
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
        private const int BatchSize = 100;

        public static long Post(IEnumerable<IActivityEvent> events)
        {
            try
            {
                var sql = new StringBuilder();
                var activityEvents = events as IActivityEvent[] ?? events.ToArray();
                var batches = activityEvents.Length / BatchSize;
                var batchId = DateTime.Now.Ticks;

                for (var b = 0; b < batches + 1; b++)
                {
                    sql.Clear();
                    sql.AppendFormat("DECLARE {0} INT{1}", StringConstants.SqlHelperLogIdVar, Environment.NewLine);
                    sql.AppendFormat("DECLARE {0} INT{1}", StringConstants.SqlHelperEventIdVar, Environment.NewLine);
                    sql.AppendFormat("DECLARE {0} INT{1}", StringConstants.SqlHelperDocIdVar, Environment.NewLine);
                    sql.AppendFormat("DECLARE {0} INT{1}", StringConstants.SqlHelperIdVar, Environment.NewLine);

                    var from = b * BatchSize;
                    var to = (b + 1) * BatchSize > activityEvents.Length ? activityEvents.Length : (b + 1) * BatchSize;

                    for (var idx = from; idx < to; idx++)
                    {
                        var eventLog = activityEvents[idx];
                        eventLog.BatchId = batchId;
                        sql.AppendFormat("{0}{1}", eventLog.GetInsertScripts(), Environment.NewLine);
                    }

                    //execute sql batch insert statements
                    using (var connection = new SqlConnection(StringConstants.ConnectionString))
                    {
                        connection.Execute(sql.ToString());
                    }
                }

                return batchId;
            }
            catch (Exception)
            {
                //TODO: inject Log4Net to log error details into files
                return -1;
            }
        }

        public static int SubmitAssignment(SubmitEvent submit)
        {
            try
            {
                var sql = new StringBuilder();
                sql.AppendFormat("DECLARE {0} INT{1}", StringConstants.SqlHelperLogIdVar, Environment.NewLine);
                sql.AppendFormat("{0}{1}", submit.GetInsertScripts(), Environment.NewLine);

                //execute sql batch insert statements
                using (var connection = new SqlConnection(StringConstants.ConnectionString))
                {
                    return connection.Query<int>(sql.ToString()).Single();
                }
            }
            catch (Exception)
            {
                //TODO: inject Log4Net to log error details into files
                return -1;
            }
        }

        public static void SaveToFileSystem(SubmitEvent submit, int teamid, string path=null)
        {
            using (var zipStream = new MemoryStream())
            {
                zipStream.Write(submit.SolutionData, 0, submit.SolutionData.Length);
                zipStream.Position = 0;
                try
                {
                    if (path == null)
                    {
                        var a = HttpContext.Current.Server.MapPath("~").TrimEnd('\\');
                        path = string.Format("{0}\\OSBLE.Web\\App_Data\\FileSystem\\", Directory.GetParent(a).FullName);
                    }
                    OSBLE.Models.FileSystem.Directories.GetAssignmentWithId(submit.CourseId ?? 1
                        , submit.AssignmentId, submit.SenderId, path).AddFile(string.Format("{0}.zip", submit.Sender.FullName), zipStream);
                }
                catch (ZipException)
                {
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
                    return connection.Execute(errorLog.GetInsertScripts());
                }
            }
            catch (Exception)
            {
                //TODO: inject Log4Net to log error details into files
                return -1;
            }
        }
    }
}
