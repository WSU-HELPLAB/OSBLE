namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class AskForHelpEvent : ActivityEvent
    {
        public string Code { get; set; }
        public string UserComment { get; set; }

        public AskForHelpEvent() { } // NOTE!! This is required by Dapper ORM
    }
}
