using OSBLE.Interfaces;

namespace OSBLEPlus.Logic.DomainObjects.Profiles
{
    public class User : IUser
    {
        public int UserId { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName {
            get { return string.Format("{0} {1}", FirstName, LastName); }
        }
        public int SchoolId { get; set; }
        public string Identification { get; set; }
        public bool IsAdmin { get; set; }
        public bool EmailAllActivityPosts { get; set; }
        public bool EmailAllNotifications { get; set; }
        public bool EmailNewDiscussionPosts { get; set; }
        public int DefaultCourseId { get; set; }
        public ICourse DefalutCourse { get; set; }

        public User() { } // NOTE!! This is required by Dapper ORM
    }
}
