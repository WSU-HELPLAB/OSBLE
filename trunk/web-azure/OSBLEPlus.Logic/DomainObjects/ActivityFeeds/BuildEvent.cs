namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class BuildEvent : ActivityEvent
    {
        public string CriticalErrorName { get; set; }

        public BuildEvent() { } // NOTE!! This is required by Dapper ORM
    }
}
