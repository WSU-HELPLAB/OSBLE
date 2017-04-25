using System;
using System.Data.SqlClient;
using System.IO;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DomainObjects.Helpers
{
    public class LocalErrorLog
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public DateTime LogDate { get; set; }
        public string Content { get; set; }

        public LocalErrorLog()
        {
            Content = string.Empty;
            LogDate = DateTime.MinValue;
        }

        public static LocalErrorLog FromFile(string filePath)
        {
            var log = new LocalErrorLog();
            DateTime localDateTime;
            DateTime.TryParse(Path.GetFileNameWithoutExtension(filePath), out localDateTime);
            log.LogDate = localDateTime;
            try
            {
                log.Content = File.ReadAllText(filePath);
            }
            catch (Exception)
            {
                log.Content = string.Empty;
            }
            return log;
        }

        public SqlCommand GetInsertCommand()
        {
            var cmd = new SqlCommand
            {
                CommandText = string.Format(@"
DECLARE {0} INT
INSERT INTO dbo.LocalErrorLogs ([SenderId],[LogDate],[Content]) VALUES (@SenderId, @LogDate, @Content)
SELECT {0}=SCOPE_IDENTITY()

SELECT {0}", StringConstants.SqlHelperLogIdVar)

                /*
                 INSERT INTO dbo.SubmitEvents (EventLogId, EventDate, SolutionName, AssignmentId)
     VALUES ({0}, @EventDate, @SolutionName, @AssignmentId)
                 */
            };
            cmd.Parameters.AddWithValue("@SenderId", SenderId);
            cmd.Parameters.AddWithValue("@LogDate", LogDate);
            cmd.Parameters.AddWithValue("@Content", Content);
            //cmd.Parameters.AddWithValue("@EventDate", LogDate);
            //cmd.Parameters.AddWithValue("@SolutionName", "error.txt");
            //cmd.Parameters.AddWithValue("@AssignmentId", 0);            
            return cmd;
        }
    }

    public class LocalErrorLogRequest
    {
        public string AuthToken { get; set; }
        public LocalErrorLog Log { get; set; }
    }

}
