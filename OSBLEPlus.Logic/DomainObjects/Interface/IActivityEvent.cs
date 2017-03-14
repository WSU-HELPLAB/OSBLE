using System.Data.SqlClient;
using OSBLE.Models.Courses;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLEPlus.Logic.DomainObjects.Interface
{
    public interface IActivityEvent : IEventLog
    {
        int EventId { get; set; }
        string SolutionName { get; set; }
        string EventName { get; }
        SqlCommand GetInsertCommand();

        // for posting
        bool CanDelete { get; set; }
        bool CanMail { get; set; }
        bool HideMail { get; set; }
        string EventVisibilityGroups { get; set; }
        string EventVisibleTo { get; set; }
        bool IsAnonymous { get; set; }
        bool CanReply { get; set; }
        bool CanEdit { get; set; }
        bool ShowProfilePicture { get; set; }
        string DisplayTitle { get; set; }

        void SetPrivileges(CourseUser currentUser, ActivityEvent activityEvent = null);
    }
}
