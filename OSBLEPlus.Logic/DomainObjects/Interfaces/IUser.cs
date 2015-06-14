namespace OSBLEPlus.Logic.DomainObjects.Interfaces
{
    public interface IUser
    {
        int UserId { get; set; }
        string Email { get; set; }
        string FirstName { get; set; }
        string LastName { get; set; }
        int SchoolId { get; set; }
        string Identification { get; set; }
        bool IsAdmin { get; set; }
        bool EmailAllActivityPosts { get; set; }
        bool EmailAllNotifications { get; set; }
        bool EmailNewDiscussionPosts { get; set; }
        int DefaultCourseId { get; set; }
        ICourse DefalutCourse { get; set; }
    }
}
