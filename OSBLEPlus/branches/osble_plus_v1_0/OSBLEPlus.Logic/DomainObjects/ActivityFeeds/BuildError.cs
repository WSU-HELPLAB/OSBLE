namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class BuildError
    {
        public int LogId { get; set; }
        public virtual ActivityEvent Log { get; set; }

        public int BuildErrorTypeId { get; set; }
        public virtual ErrorType BuildErrorType { get; set; }
    }
}
