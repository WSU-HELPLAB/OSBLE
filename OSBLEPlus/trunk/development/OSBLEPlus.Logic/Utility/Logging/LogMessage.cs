namespace OSBLEPlus.Logic.Utility.Logging
{
    public enum LogPriority : byte
    {
        LowPriority = 0,
        MediumPriority = 1,
        HighPriority = 2
    };

    public class LogMessage
    {
        public LogPriority Priority { get; set; }
        public string Message { get; set; }
    }
}
