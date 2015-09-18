using OSBLE.Models.Courses;
namespace OSBLE.Interfaces
{
    public interface IUser
    {
        int IUserId { get; set; }
        string Email { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string FullName { get; }
        int ISchoolId { get; set; }
        string Identification { get; set; }
        bool IsAdmin { get; set; }
        bool EmailAllActivityPosts { get; set; }
        bool EmailSelfActivityPosts { get; set; }
        bool EmailAllNotifications { get; set; }
        bool EmailNewDiscussionPosts { get; set; }
        int IDefaultCourseId { get; set; }
        ICourse DefalutCourse { get; set; }
        string DisplayName(CourseUser viewingUser);
    }
}
