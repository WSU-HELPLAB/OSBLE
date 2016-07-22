using System.Data.SqlClient;
using OSBLE.Models.Courses;

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
        bool InstructorOnly { get; set; }
        bool CanReply { get; set; }
        bool CanEdit { get; set; }
        bool ShowProfilePicture { get; set; }
        string DisplayTitle { get; set; }

        void SetPrivileges(CourseUser currentUser);
    }
}
