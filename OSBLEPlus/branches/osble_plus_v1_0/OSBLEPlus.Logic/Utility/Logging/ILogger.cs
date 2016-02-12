namespace OSBLEPlus.Logic.Utility.Logging
{
    public interface ILogger
    {
        /// <summary>
        /// Sets the minimum priority that the logger will listen to
        /// </summary>
        LogPriority MinimumPriority { get; set; }

        /// <summary>
        /// Writes the supplied text to OSBIDE's log file.
        /// </summary>
        /// <param name="content"></param>
        void WriteToLog(string content);

        /// <summary>
        /// Writes the supplied text to OSBIDE's log file.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="priority"></param>
        void WriteToLog(string content, LogPriority priority);

        /// <summary>
        /// Writes the supplied message to OSBIDE's log file
        /// </summary>
        /// <param name="message"></param>
        void WriteToLog(LogMessage message);

    }
}
