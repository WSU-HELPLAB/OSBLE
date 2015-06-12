namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class SubmitEvent:ActivityEvent
    {
        public int AssignmentId { get; set; }
        public SubmitEvent() { } // NOTE!! This is required by Dapper ORM
    }
}
