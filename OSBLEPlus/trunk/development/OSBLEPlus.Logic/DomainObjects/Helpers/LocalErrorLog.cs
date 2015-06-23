using System;
using System.IO;

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

        public string GetInsertScripts()
        {
            return string.Format(@"
INSERT INTO [dbo].[LocalErrorLogs] ([SenderId],[LogDate],[Content])
VALUES ({0},'{1}','{2}',){3}", SenderId, LogDate, Content, Environment.NewLine);
        }
    }

    public class LocalErrorLogRequest
    {
        public string AuthToken { get; set; }
        public LocalErrorLog Log { get; set; }
    }

}
