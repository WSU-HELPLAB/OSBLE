using OSBLE.Controllers;
using OSBLE.Interfaces;


namespace OSBLE.Utility
{
    //AJ: This class converts an OSBLE User to an IUser
    public class OSBLEConvertIUser
    {
        // this user is OSBLE's User type
        public static IUser GetIUser()
        {
            // need a controller to access CurrentUser
            HomeController hc = new HomeController();

            // use long form to prevent future confusion
            OSBLEPlus.Logic.DomainObjects.Profiles.User newUser = new OSBLEPlus.Logic.DomainObjects.Profiles.User();

            newUser.IDefaultCourseId = 0; // need to pull from DB
            newUser.DefalutCourse = null; // need to figure out a function to convert from course to ICourse
            newUser.Email = null; // need user's email
            newUser.EmailAllActivityPosts = false; // based off of preferences
            newUser.EmailSelfActivityPosts = false;
            newUser.EmailAllNotifications = false;
            newUser.EmailNewDiscussionPosts = false;
            newUser.FirstName = null;
            newUser.Identification = null; // unsure need to figure this out
            newUser.IsAdmin = false; // most likely false, but need to get from the DB
            newUser.LastName = null;
            newUser.ISchoolId = -1; // need to pull from DB
            newUser.IUserId = -1; // need to pull from DB

            return newUser;
        }
    }

    public class OSBLEConvertCourse
    {
        public static ICourse GetICourse()
        {
            HomeController hc = new HomeController();

            //ICourse temp;

            //temp.CourseId; // courseId from DB
            //temp.Description; // Description from the instructor
            //temp.EndDate; // When the semester ends
            //temp.Name; // full name of the course
            //temp.NamePrefix; // abbreviated name of the course
            //temp.Number;  // Course Number e.g. 111 for CptS 111
            //temp.Semester; // Fall/Spring/Summer
            //temp.StartDate; // when the couse starts
            //temp.Year; // year Course takes place

            return null;
        }
    }
}