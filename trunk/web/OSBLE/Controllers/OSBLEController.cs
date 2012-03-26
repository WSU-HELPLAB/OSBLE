using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.SessionState;
using OSBLE.Models;

using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.Assignments;
using System.Configuration;

namespace OSBLE.Controllers
{
    [SessionState(SessionStateBehavior.Default)]
    public abstract class OSBLEController : Controller
    {
        protected OSBLEContext db = new OSBLEContext();

        protected UserProfile currentUser = null;

        public UserProfile CurrentUser
        {
            get { return currentUser; }
        }

        protected CourseUser activeCourse = null;

        public CourseUser ActiveCourse
        {
            get { return activeCourse; }
        }

        protected HttpContext context = System.Web.HttpContext.Current;
        protected List<CourseUser> currentCourses = new List<CourseUser>();

        protected bool DashboardSingleCourseMode;

        /// <summary>
        /// Defines a menu item tab
        /// </summary>
        public class MenuItem
        {
            public string Name { get; set; }

            public string Controller { get; set; }

            public string Action { get; set; }

            public bool ModifierOnly { get; set; }

            public bool GraderOnly { get; set; }

            public bool ViewerOnly { get; set; }

            public bool AdminOnly { get; set; }

            public bool NotInCommunityPage { get; set; }

            public bool CommunityOnlyPage { get; set; }

            /// <summary>
            /// Creates a menu item that everyone can access.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="controller"></param>
            /// <param name="action"></param>
            public MenuItem(string name, string controller, string action)
            {
                this.Name = name;
                this.Controller = controller;
                this.Action = action;

                this.ModifierOnly = false;
                this.ViewerOnly = false;
                this.AdminOnly = false;
                this.NotInCommunityPage = false;
                this.CommunityOnlyPage = false;
            }

            /// <summary>
            /// Creates a menu item with particular privileges set for its display.
            /// NOTE: This only affects the display of the menu item. You still need to use attributes to validate pages!
            /// </summary>
            /// <param name="name">Displayed name of menu item</param>
            /// <param name="controller">Target controller</param>
            /// <param name="action">Target Action</param>
            /// <param name="modifierOnly">Only course modifiers (instructors) can see this tab</param>
            /// <param name="graderOnly">Only course graders (instructors/TAs) can see this tab</param>
            /// <param name="viewerOnly">Only course viewers can see this tab</param>
            /// <param name="adminOnly">Only admins can see this tab</param>
            /// <param name="notInCommunityPage">This tab should not appear in communities</param>
            /// <param name="communityOnlyPage">This tab should only appear on communities</param>
            public MenuItem(string name, string controller, string action, bool modifierOnly, bool graderOnly, bool viewerOnly, bool adminOnly, bool notInCommunityPage, bool communityOnlyPage)
            {
                this.Name = name;
                this.Controller = controller;
                this.Action = action;

                this.ModifierOnly = modifierOnly;
                this.GraderOnly = graderOnly;
                this.ViewerOnly = viewerOnly;
                this.AdminOnly = adminOnly;
                this.NotInCommunityPage = notInCommunityPage;
                this.CommunityOnlyPage = communityOnlyPage;
            }
        }

        /// <summary>
        /// Provides common data for all controllers in the OSBLE app, such as profile and current course information.
        /// </summary>
        public OSBLEController()
        {
            Initialize();
        }

        public OSBLEController(OSBLEContext context)
        {
            db = context;
            Initialize();
        }

        private void Initialize()
        {
            // If logged in, feed user profile to view.
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["RequireLoginValidation"]) == true)
            {
                if (context.User.Identity.IsAuthenticated)
                {
                    setupInitialDatabaseData();

                    setupMenu();

                    setCurrentUserProfile();

                    GetEnrolledCourses();

                    setCourseListTitle();

                    setDashboardDisplayMode();
                }
            }
        }

        /// <summary>
        /// Checks to see if the Course/Community roles have been populated.
        /// Also adds WSU to schools if none exist.
        /// This is different from the sample data generation in OSBLEContext, which
        /// is meant for development purposes only.
        /// </summary>
        private void setupInitialDatabaseData()
        {
            if (db.AbstractRoles.Count() == 0)
            {
                db.SeedRoles();
                db.SaveChanges();
            }

            if (db.Schools.Count() == 0)
            {
                db.Schools.Add(
                        new School()
                        {
                            Name = "Washington State University"
                        }
                    );

                db.SaveChanges();
            }
        }

        /// <summary>
        /// Sets title of course list title based on whether or not
        /// user is in any communities. Will display "Course" for courses
        /// or "Course/Community" if at least one community is present.
        /// </summary>
        private void setCourseListTitle()
        {
            if (currentCourses.Where(c => c.AbstractCourse is Community).Count() > 0)
            {
                ViewBag.CourseListTitle = "Course/Community";
            }
            else
            {
                ViewBag.CourseListTitle = "Course";
            }
            ViewBag.CurrentCourses = currentCourses;
        }

        /// <summary>
        /// Sets ViewBag flag for whether or not the current session
        /// is displaying only the active course in the dashboard feed,
        /// or displaying all courses.
        /// </summary>
        private void setDashboardDisplayMode()
        {
            // if not set or erroniously set
            //     set the activity feed to display a single course
            // otherwise
            //     do nothing because it has been set by the user (call to HomeController's SetDashboardMode method)
            if ((context.Session["DashboardSingleCourseMode"] == null) || (context.Session["DashboardSingleCourseMode"].GetType() != typeof(Boolean)))
            {
                context.Session["DashboardSingleCourseMode"] = true;
            }

            DashboardSingleCourseMode = ViewBag.DashboardSingleCourseMode = context.Session["DashboardSingleCourseMode"];
        }

        /// <summary>
        /// Creates menu items (with permissions) for tabbed main menu on most OSBLE screens.
        /// </summary>
        private void setupMenu()
        {
            List<MenuItem> menu = new List<MenuItem>();

            menu.Add(new MenuItem("Dashboard", "Home", "Index"));
            menu.Add(new MenuItem("Assignments", "Assignment", "Index", false, false, false, false, true, false));
            menu.Add(new MenuItem("Grades", "Gradebook", "Index", false, false, false, false, true, false));
            menu.Add(new MenuItem("Users", "Roster", "Index", true, false, false, false, false, false));
            menu.Add(new MenuItem("Course Settings", "Course", "Edit", true, true, true, false, true, false));
            menu.Add(new MenuItem("Community Settings", "Community", "Edit", true, true, true, false, false, true));
            menu.Add(new MenuItem("Administration", "Admin", "Index", false, false, false, true, false, false));

            ViewBag.Menu = menu;
        }

        /// <summary>
        /// Sets currentCourses for the current user, which is a list of
        /// courses/communities they are enrolled in or have access to.
        /// Also, if a user is invalid, it will clear their session and log them out.
        /// </summary>
        protected void GetEnrolledCourses()
        {
            // If current user is valid, get course list for user.
            if (currentUser != null)
            {
                // Sends the ViewBag the amount of unread mail messages the user has.
                SetUnreadMessageCount();
                List<CourseUser> allUsersCourses = db.CourseUsers.Where(cu => cu.UserProfileID == currentUser.ID).ToList();

                // Get list of courses this user is connected to. Remove inactive (for anyone other than instructors or observers) or hidden (for all) courses.
                currentCourses = allUsersCourses.Where(cu => (cu.AbstractCourse is Course) &&
                    (((cu.AbstractCourse as Course).Inactive == false) ||
                    (cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor) ||
                    (cu.AbstractRoleID == (int)CourseRole.CourseRoles.Observer)))
                    // Order first by descending start date (newest first)
                        .OrderByDescending(cu => (cu.AbstractCourse as Course).StartDate)
                    // Order next by whether the course is inactive, placing inactive courses underneath active.
                        .OrderBy(cu => (cu.AbstractCourse as Course).Inactive).ToList();

                // Add communities under courses, ordered by name
                currentCourses = currentCourses.Concat(allUsersCourses.Where(cu => cu.AbstractCourse is Community).OrderBy(cu => cu.AbstractCourse.Name).ToList()).ToList();

                // Only consider non-hidden courses as the active course.
                List<CourseUser> activeCoursePool = currentCourses.Where(cu => cu.Hidden == false).ToList();

                int activeCourseID;

                var sessionAc = context.Session["ActiveCourse"];

                if (sessionAc == null || !(sessionAc is int))
                {
                    // On login or invalid ActiveCourse, set to user's default course.
                    activeCourseID = currentUser.DefaultCourse;
                }
                else if (sessionAc is int)
                {
                    // If ActiveCourse is valid in session, try it for our active course.
                    activeCourseID = (int)sessionAc;
                }
                else
                {
                    activeCourseID = 0;
                }

                // Load currently selected course, as long as user is actually a member of said course.
                // Otherwise, load first course.
                if ((activeCourse = activeCoursePool.Where(cu => cu.AbstractCourseID == activeCourseID).FirstOrDefault()) == null)
                {
                    activeCourse = activeCoursePool.FirstOrDefault();
                }

                if (activeCourse != null)
                {
                    context.Session["ActiveCourse"] = activeCourse.AbstractCourseID;
                    ViewBag.ActiveCourse = activeCourse;
                }
            }
            else // User invalid. Logout.
            {
                context.Session.Clear(); // Clear session
                FormsAuthentication.SignOut();
            }
        }

        private void setCurrentUserProfile()
        {
            string userName = context.User.Identity.Name;
            ViewBag.CurrentUser = currentUser = db.UserProfiles.Where(u => u.UserName == userName).FirstOrDefault();
        }

        public void SetUnreadMessageCount()
        {
            ViewBag.UnreadMessageCount = (int)db.Mails.Where(m => (m.ToUserProfileID == currentUser.ID) && (m.Read == false)).Count();
        }

        public static List<UserProfile> GetAllUsers(AssignmentTeam team)
        {
            List<UserProfile> users = new List<UserProfile>();

            //Searching through the Assignment teams and adding all the Users
            foreach (TeamMember tm in team.Team.TeamMembers)
            {
                users.Add(tm.CourseUser.UserProfile);
            }
            return users;
        }


        /// <summary>
        /// Returns a list of all the course users in the team
        /// </summary>
        public static List<CourseUser> GetAllCourseUsers(AssignmentTeam team)
        {
            List<CourseUser> users = new List<CourseUser>();

            foreach (TeamMember tm in team.Team.TeamMembers)
            {
                users.Add(tm.CourseUser);
            }
            return users;
        }

        public static TeamMember GetTeamUser(Assignment assignment, UserProfile user)
        {
            TeamMember teamMember = new TeamMember();
            List<AssignmentTeam> assignmentTeams = assignment.AssignmentTeams.ToList();
            foreach (AssignmentTeam at in assignmentTeams)
            {
                foreach (TeamMember tm in at.Team.TeamMembers)
                {
                    if (tm.CourseUser.UserProfileID == user.ID)
                    {
                        teamMember = tm;
                    }
                }
            }

            return teamMember;
        }

        /// <summary>
        /// Returns the Assignment team or null if a team does not exist
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static AssignmentTeam GetAssignmentTeam(Assignment assignment, UserProfile user)
        {
            AssignmentTeam returnValue = new AssignmentTeam();
            foreach (AssignmentTeam at in assignment.AssignmentTeams)
            {
                foreach (TeamMember tm in at.Team.TeamMembers)
                {
                    if (tm.CourseUser.UserProfileID == user.ID)
                    {
                        returnValue = at;
                    }
                }
            }
            return returnValue;
        }

        public DateTime? GetDueDate(Assignment assignment)
        {
            //var nextActivity = GetNextActivity(activity);
            //if (nextActivity != null)
            //{
            //    return nextActivity.ReleaseDate;
            //}
            return null;
        }

        protected DateTime? GetSubmissionTime(Course course, Assignment assignment, AssignmentTeam team)
        {
            DirectoryInfo submissionFolder = new DirectoryInfo(FileSystem.GetTeamUserSubmissionFolder(false, course, assignment.ID, team));

            DateTime? timeSubmitted;

            if (submissionFolder.Exists)
            {
                //unfortunately LastWriteTime for a directory does not take into account it's file or
                //sub directories and these we need to check to see when the last file was written too.
                timeSubmitted = submissionFolder.LastWriteTime;
                foreach (FileInfo file in submissionFolder.GetFiles())
                {
                    if (file.LastWriteTime > timeSubmitted)
                    {
                        timeSubmitted = file.LastWriteTime;
                    }
                }

                //if no files, return null
                if (submissionFolder.GetFiles().Count() == 0)
                {
                    timeSubmitted = null;
                }

                return timeSubmitted;
            }
            else
            {
                return null;
            }
        }

        public TimeSpan? calculateLateness(Course course, Assignment assignment, AssignmentTeam team)
        {
            DateTime? dueDate = assignment.DueDate;
            
            DateTime? submissionTime = GetSubmissionTime(course, assignment, team);

            if (submissionTime == null)
            {
                return null;
            }

            TimeSpan? lateness = dueDate - submissionTime;
            if (lateness.Value.TotalMinutes >= 0)
            {
                return null;
            }
            return lateness;
        }

        //Returns the positive percentage to deduct from the students grade.
        public double CalcualateLatePenaltyPercent(Assignment assignment, TimeSpan lateness)
        {
            double returnVal;
            if (lateness.TotalHours < assignment.HoursLateWindow)
            {
                returnVal = lateness.TotalHours * assignment.DeductionPerUnit;

                if (returnVal > 100)
                {
                    returnVal = 100;
                }
                return Math.Abs(returnVal);
            }
            //The assignment is automatic 0.
            else
            {
                return 100;
            }            
        }

        protected string[] GetFileExtensions(DeliverableType deliverableType)
        {
            Type type = deliverableType.GetType();

            FieldInfo fi = type.GetField(deliverableType.ToString());

            //we get the attributes of the selected language
            FileExtensions[] attrs = (fi.GetCustomAttributes(typeof(FileExtensions), false) as FileExtensions[]);

            //make sure we have more than (should be exactly 1)
            if (attrs.Length > 0 && attrs[0] is FileExtensions)
            {
                return attrs[0].Extensions;
            }
            else
            {
                //throw and exception if not decorated with any attrs because it is a requirement
                throw new Exception("Languages must have be decorated with a FileExtensionAttribute");
            }
        }

        protected List<SelectListItem> GetListOfDeliverableTypes()
        {
            List<SelectListItem> fileTypes = new List<SelectListItem>();
            int i = 0;
            DeliverableType deliverable = (DeliverableType)i;
            while (Enum.IsDefined(typeof(DeliverableType), i))
            {
                Type type = deliverable.GetType();

                FieldInfo fi = type.GetField(deliverable.ToString());

                //we get the attributes of the selected language
                FileExtensions[] attrs = (fi.GetCustomAttributes(typeof(FileExtensions), false) as FileExtensions[]);

                //make sure we have more than (should be exactly 1)
                if (attrs.Length > 0 && attrs[0] is FileExtensions)
                {
                    //we get the first attributes value which should be the fileExtension
                    string s = deliverable.ToString();
                    s += " (";
                    s += string.Join(", ", attrs[0].Extensions);
                    s += ")";

                    SelectListItem sli = new SelectListItem();

                    sli.Text = s;
                    sli.Value = i.ToString();

                    fileTypes.Add(sli);
                }
                else
                {
                    //throw and exception if not decorated with any attrs because it is a requirement
                    throw new Exception("Languages must have be decorated with a FileExtensionAttribute");
                }

                i++;
                deliverable = (DeliverableType)i;
            }

            return fileTypes;
        }

        /// <summary>
        /// Removes the provided user from the active course
        /// </summary>
        /// <param name="user"></param>
        public void RemoveUserFromCourse(UserProfile user)
        {
            CourseUser cu =  (from c in db.CourseUsers
                    where c.AbstractCourseID == activeCourse.AbstractCourseID
                    && c.UserProfileID == user.ID
                    select c).FirstOrDefault();
            if (cu != null)
            {
                db.CourseUsers.Remove(cu);
            }
            db.SaveChanges();

            List<AssignmentTeam> teamsWithNoMembers = (from at in db.AssignmentTeams
                                                       where at.Team.TeamMembers.Count == 0
                                                       select at).ToList();
            foreach (AssignmentTeam team in teamsWithNoMembers)
            {
                db.AssignmentTeams.Remove(team);
            }
            db.SaveChanges();
        }

        /// <summary>
        /// Given a courseId, returns a list of random ordered courseusers.
        /// </summary>
        /// <param name="courseId"></param>
        /// <returns></returns>
        public List<CourseUser> GetAnonymizedCourseUserList(int courseId)
        {
            List<Tuple<CourseUser, string>> Users = new List<Tuple<CourseUser, string>>();
            List<Tuple<CourseUser, string>> Anonymized = new List<Tuple<CourseUser, string>>();
            List<CourseUser> UserList = new List<CourseUser>();
            List<CourseUser> returnList = new List<CourseUser>();

            if (courseId > 0)
            {
                UserList = (from user in db.CourseUsers
                         where user.AbstractCourseID == courseId &&
                         user.AbstractRole.CanSubmit
                         orderby user.ID
                         select user).ToList();

                foreach (CourseUser u in UserList)
                {
                    char[] rev = u.ID.ToString().ToCharArray();
                    Array.Reverse(rev);
                    Tuple<CourseUser, string> user = new Tuple<CourseUser, string>(u, new string(rev));
                    Users.Add(user);
                }

                Anonymized = Users.OrderBy(u => u.Item2.ToString()).ToList();
                foreach (Tuple<CourseUser, string> x in Anonymized)
                {
                    returnList.Add(x.Item1);
                }
            }
            return returnList;
        }
    }
}