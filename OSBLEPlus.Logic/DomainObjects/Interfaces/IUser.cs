namespace OSBLEPlus.Logic.DomainObjects.Interfaces
{
    public interface IUser
    {
        int UserId { get; set; }
        string Email { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        string FullName { get; }
        int SchoolId { get; set; }
        string Identification { get; set; }
        bool IsAdmin { get; set; }
        bool EmailAllActivityPosts { get; set; }
        bool EmailAllNotifications { get; set; }
        bool EmailNewDiscussionPosts { get; set; }
        int DefaultCourseId { get; set; }
        IProfileCourse DefalutCourse { get; set; }
    }
}
