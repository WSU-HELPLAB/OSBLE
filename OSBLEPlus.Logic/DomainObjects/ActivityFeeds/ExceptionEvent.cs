namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class ExceptionEvent: ActivityEvent
    {
        public string DocumentName { get; set; }
        public int ExceptionAction { get; set; }
        public string ExceptionCode { get; set; }
        public string ExceptionDescription { get; set; }
        public string ExceptionType { get; set; }
        public string LineContent { get; set; }
        public int LineNumber { get; set; }
        public ExceptionEvent() { } // NOTE!! This is required by Dapper ORM
    }
}
