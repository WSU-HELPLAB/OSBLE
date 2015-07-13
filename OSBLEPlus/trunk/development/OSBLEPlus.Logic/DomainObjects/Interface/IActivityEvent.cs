namespace OSBLEPlus.Logic.DomainObjects.Interface
{
    public interface IActivityEvent : IEventLog
    {
        int EventId { get; set; }
        string SolutionName { get; set; }
        string EventName { get; }
        string GetInsertScripts();

        // for posting
        bool CanDelete { get; set; }
        bool CanMail { get; set; }
        bool CanReply { get; set; }
        bool ShowProfilePicture { get; set; }
        string DisplayTitle { get; set; }
        
    }
}
