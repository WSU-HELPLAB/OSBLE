using System;
using System.IO;

namespace OSBLEPlus.Logic.Utility.Logging
{
    public class LocalErrorLogger : ILogger
    {
        private readonly string _filePath;
        public LogPriority MinimumPriority { get; set; }
        public string FilePath
        {
            get
            {
                return _filePath;
            }
        }

        public LocalErrorLogger()
        {
            _filePath = StringConstants.LocalErrorLogPath;
            MinimumPriority = LogPriority.MediumPriority;
        }

        public LocalErrorLogger(string filePath)
            : this()
        {
            _filePath = filePath;
        }

        public void WriteToLog(LogMessage message)
        {
            //ignore events below our threshold.
            if (message.Priority < MinimumPriority)
            {
                return;
            }

            lock (this)
            {
                try
                {
                    using (var writer = File.AppendText(_filePath))
                    {
                        var text = string.Format("{0},{1},{2}",
                            message.Priority,
                            DateTime.UtcNow.ToString("HH:mm:ss"),
                            message.Message
                            );
                        writer.WriteLine(text);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        public void WriteToLog(string content)
        {
            var message = new LogMessage()
            {
                Message = content,
                Priority = LogPriority.LowPriority
            };
            WriteToLog(message);
        }

        public void WriteToLog(string content, LogPriority priority)
        {
            var message = new LogMessage()
            {
                Message = content,
                Priority = priority
            };
            WriteToLog(message);
        }
    }
}
