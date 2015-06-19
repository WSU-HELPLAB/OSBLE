using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.SessionState;

using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Utility;

namespace OSBLE.Controllers
{
    [SessionState(SessionStateBehavior.Default)]
    public abstract class OSBLEController : Controller
    {

        protected OSBLEContext db = new OSBLEContext();

        public FileCache Cache { get; private set; }
        private UserProfile _currentUser = OsbleAuthentication.CurrentUser;
        public UserProfile CurrentUser
        {
            get
            {
                return _currentUser ?? (_currentUser = OsbleAuthentication.CurrentUser);
            }
        }

        public CourseUser ActiveCourseUser { get; protected set; }

        protected HttpContext context = System.Web.HttpContext.Current;
        protected List<CourseUser> currentCourses = new List<CourseUser>();

        protected bool DashboardSingleCourseMode;

        /// <summary>
        /// Defines a menu item tab
        /// 
        /// I don't know why the original author of this didn't just call it a "tab", 
        /// but that's what this object represents, a tab in the OSBLE interface. Tabs 
        /// show up for different users under different circumstances and such 
        /// visibility is affected by the role of the viewer as well as the course 
        /// type (course, commmunity, or assessment committee).
        /// </summary>
        public class MenuItem
        {
            public string Name { get; set; }

            public string Controller { get; set; }

            public string Action { get; set; }
            public string Area { get; set; }

            public bool ModifierOnly { get; set; }

            public bool GraderOnly { get; set; }

            public bool ViewerOnly { get; set; }

            public bool AdminOnly { get; set; }

            public bool NotInCommunityPage { get; set; }

            public bool CommunityOnlyPage { get; set; }

            public bool ShownInAssessmentCommittees { get; set; }

            public bool ShownInAssessmentCommitteesOnly { get; set; }

            /// <summary>
            /// Creates a menu item that everyone can access.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="controller"></param>
            /// <param name="action"></param>
            public MenuItem(string name, string controller, string action)
            {
                Name = name;
                Controller = controller;
                Action = action;

                ModifierOnly = false;
                ViewerOnly = false;
                AdminOnly = false;
                NotInCommunityPage = false;
                CommunityOnlyPage = false;
                ShownInAssessmentCommittees = true;
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
            /// <param name="shownInAssessmentCommittees">This tab appears in assessment comittees</param>
            /// <param name="assessmentComitteesOnly"></param>
            /// <param name="area">allow area views to be launched by menu item</param>
            public MenuItem(string name, string controller, string action, bool modifierOnly, bool graderOnly,
                bool viewerOnly, bool adminOnly, bool notInCommunityPage, bool communityOnlyPage,
                bool shownInAssessmentCommittees = false, bool assessmentComitteesOnly = false, string area = "")
            {
                Name = name;
                Controller = controller;
                Action = action;
                Area = area;

                ModifierOnly = modifierOnly;
                GraderOnly = graderOnly;
                ViewerOnly = viewerOnly;
                AdminOnly = adminOnly;
                NotInCommunityPage = notInCommunityPage;
                CommunityOnlyPage = communityOnlyPage;
                ShownInAssessmentCommittees = shownInAssessmentCommittees;
                ShownInAssessmentCommitteesOnly = assessmentComitteesOnly;
            }

            public bool IsVisibleTo(UserProfile currentUser, CourseUser activeCourse)
            {
                // If the course is an assessment committee...
                if (activeCourse.AbstractCourse is AssessmentCommittee)
                {
                    return null != activeCourse && ShownInAssessmentCommittees;
                }

                if (((currentUser != null) && (!AdminOnly || currentUser.IsAdmin))
                    &&
                    (!ModifierOnly || ((activeCourse != null) && activeCourse.AbstractRole.CanModify))
                    &&
                    (!GraderOnly || ((activeCourse != null) && activeCourse.AbstractRole.CanGrade))
                    &&
                    (!ViewerOnly || ((activeCourse != null) && activeCourse.AbstractRole.CanSeeAll))
                    &&
                    ((!CommunityOnlyPage && !NotInCommunityPage) ||
                     (NotInCommunityPage && activeCourse != null && !(activeCourse.AbstractCourse is Community)
                      || (CommunityOnlyPage && activeCourse != null && activeCourse.AbstractCourse is Community)))
                    )
                {
                    return !ShownInAssessmentCommitteesOnly;
                }

                return false;
            }
        }

        /// <summary>
        /// Provides common data for all controllers in the OSBLE app, such as profile and current course information.
        /// </summary>
        public OSBLEController()
        {
            ActiveCourseUser = null;
            Initialize();
        }

        public OSBLEController(OSBLEContext context)
        {
            ActiveCourseUser = null;
            db = context;
            Initialize();
        }

        private void Initialize()
        {
            // If logged in, feed user profile to view.
            if (Convert.ToBoolean(ConfigurationManager.AppSettings["RequireLoginValidation"]) == true)
            {
                if (OsbleAuthentication.CurrentUser != null && OsbleAuthentication.CurrentUser.IsApproved)
                {
                    Cache = FileCacheHelper.GetCacheInstance(OsbleAuthentication.CurrentUser);

                    SetupMenu();

                    SetCurrentUserProfile();

                    GetEnrolledCourses();

                    SetCourseListTitle();

                    SetDashboardDisplayMode();
                }
            }
        }

        public void UpdateCacheInstance(UserProfile user)
        {
            Cache = FileCacheHelper.GetCacheInstance(user);
        }

        /// <summary>
        /// Sets title of course list title based on whether or not
        /// user is in any communities. Will display "Course" for courses
        /// or "Course/Community" if at least one community is present.
        /// </summary>
        private void SetCourseListTitle()
        {
            var hasCommunities = currentCourses.Any(c => c.AbstractCourse is Community);
            var hasCommittees = currentCourses.Any(c => c.AbstractCourse is AssessmentCommittee);

            if (hasCommunities)
            {
                ViewBag.CourseListTitle = hasCommittees ? "Course/Community/Committee" : "Course/Community";
            }
            else if (hasCommittees)
            {
                ViewBag.CourseListTitle = "Course/Committee";
            }
            else
            {
                ViewBag.CourseListTitle = "Course";
            }
            if (currentCourses == null)
            {
                currentCourses = new List<CourseUser>();
            }
            ViewBag.CurrentCourses = currentCourses;
        }

        /// <summary>
        /// Sets ViewBag flag for whether or not the current session
        /// is displaying only the active course in the dashboard feed,
        /// or displaying all courses.
        /// </summary>
        private void SetDashboardDisplayMode()
        {
            // if not set or erroniously set
            //     set the activity feed to display a single course
            // otherwise
            //     do nothing because it has been set by the user (call to HomeController's SetDashboardMode method)
            var returnValue = false;
            bool? cacheResult;
            try { cacheResult = (bool?)Cache["DashboardSingleCourseMode"]; }
            catch (Exception) // IOException
            {
                // this occurs when the Cache file is being used by another program, usually this is because an instance of
                // the FileCache is calculating the size or cleaning
                cacheResult = null;
            }
            if (cacheResult == null || bool.TryParse(cacheResult.ToString(), out returnValue))
            {
                returnValue = true;
                Cache["DashboardSingleCourseMode"] = returnValue;
            }

            DashboardSingleCourseMode = ViewBag.DashboardSingleCourseMode = returnValue;
        }

        /// <summary>
        /// Creates menu items (with permissions) for tabbed main menu on most OSBLE screens.
        /// </summary>
        private void SetupMenu()
        {
            var menu = new List<MenuItem>
            {
                new MenuItem("Dashboard", "Home", "Index"),
                new MenuItem("Assignments", "Assignment", "Index", false, false, false, false, true, false, false, false),
                new MenuItem("Assessments", "Committee", "Index", false, false, false, false, true, false, true, true),
                new MenuItem("Grades", "Gradebook", "Index", false, false, false, false, true, false, false),
                new MenuItem("Users", "Roster", "Index", true, false, false, false, false, false, true),
                new MenuItem("Course Settings", "Course", "Edit", true, true, true, false, true, false, false, false),
                new MenuItem("Community Settings", "Community", "Edit", true, true, true, false, false, true, false,
                    false),
                new MenuItem("Committee Settings", "Committee", "Edit", true, true, true, false, false, true, true, true),
                new MenuItem("Administration", "Admin", "Index", false, false, false, true, false, false, false),
                new MenuItem("Analytics", "Calendar", "Index", false, false, false, true, false, false, false, false,
                    "Analytics")
            };

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
            if (CurrentUser != null)
            {
                // Sends the ViewBag the amount of unread mail messages the user has.
                SetUnreadMessageCount();
                var allUsersCourses = db.CourseUsers.Where(cu => cu.UserProfileID == CurrentUser.ID).ToList();

                // Get list of courses this user is connected to. Remove inactive (for anyone other than instructors or observers) or hidden (for all) courses.
                currentCourses = allUsersCourses.Where(cu => (cu.AbstractCourse is Course)
                    &&
                    cu.AbstractCourse.IsDeleted == false
                    &&
                    (((cu.AbstractCourse as Course).Inactive == false) ||
                    (cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor) ||
                    (cu.AbstractRoleID == (int)CourseRole.CourseRoles.Observer)))
                    .Where(cu => cu.AbstractRoleID != (int)CourseRole.CourseRoles.Withdrawn)
                    // Order first by descending start date (newest first)
                        .OrderByDescending(cu => (cu.AbstractCourse as Course).StartDate)
                    // Order next by whether the course is inactive, placing inactive courses underneath active.
                        .OrderBy(cu => (cu.AbstractCourse as Course).Inactive).ToList();

                // Add communities under courses, ordered by name
                currentCourses = currentCourses
                    .Concat(allUsersCourses
                    .Where(cu => cu.AbstractCourse is Community
                        && cu.AbstractCourse.IsDeleted == false
                    )
                    .OrderBy(cu => cu.AbstractCourse.Name)
                    .ToList())
                    .ToList();

                // Add committees under communities, ordered by name
                currentCourses = currentCourses
                    .Concat(allUsersCourses
                    .Where(cu => cu.AbstractCourse is AssessmentCommittee
                        && cu.AbstractCourse.IsDeleted == false
                    )
                    .OrderBy(cu => cu.AbstractCourse.Name)
                    .ToList())
                    .ToList();

                // Only consider non-hidden courses as the active course.
                var activeCoursePool = currentCourses.Where(cu => cu.Hidden == false).ToList();

                int activeCourseId;

                //int sessionAc = 0;// = Cache["ActiveCourse"];
                var sessionAc = Cache["ActiveCourse"];


                if (sessionAc == null || !(sessionAc is int))
                {
                    // On login or invalid ActiveCourse, set to user's default course.
                    activeCourseId = CurrentUser.DefaultCourse;
                }
                else if (sessionAc is int)
                {
                    // If ActiveCourse is valid in session, try it for our active course.
                    activeCourseId = (int)sessionAc;
                }
                else
                {
                    activeCourseId = 0;
                }

                // Load currently selected course, as long as user is actually a member of said course.
                // Otherwise, load first course.
                if ((ActiveCourseUser = activeCoursePool.FirstOrDefault(cu => cu.AbstractCourseID == activeCourseId)) == null)
                {
                    ActiveCourseUser = activeCoursePool.FirstOrDefault();
                }

                if (ActiveCourseUser != null)
                {
                    Cache["ActiveCourse"] = ActiveCourseUser.AbstractCourseID;
                    ViewBag.ActiveCourse = ActiveCourseUser;
                    ViewBag.ActiveCourseUser = ActiveCourseUser;
                }
            }
            else // User invalid. Logout.
            {
                context.Session.Clear(); // Clear session
                FormsAuthentication.SignOut();
            }
        }

        private void SetCurrentUserProfile()
        {
            ViewBag.UserSchool = db.Schools.Single(s => s.ID == OsbleAuthentication.CurrentUser.SchoolID).Name;
            ViewBag.CurrentUser = OsbleAuthentication.CurrentUser;
        }

        public void SetUnreadMessageCount()
        {
            ViewBag.UnreadMessageCount = db.Mails.Count(m => (m.ToUserProfileID == CurrentUser.ID) && (m.Read == false) && (m.DeleteFromInbox == false));
        }

        public static List<UserProfile> GetAllUsers(AssignmentTeam team)
        {
            //Searching through the Assignment teams and adding all the Users
            return team.Team.TeamMembers.Select(tm => tm.CourseUser.UserProfile).ToList();
        }


        /// <summary>
        /// Returns a list of all the course users in the team
        /// </summary>
        public static List<CourseUser> GetAllCourseUsers(AssignmentTeam team)
        {
            return team.Team.TeamMembers.Select(tm => tm.CourseUser).ToList();
        }

        public static TeamMember GetTeamUser(Assignment assignment, UserProfile user)
        {
            var teamMember = new TeamMember();
            var assignmentTeams = assignment.AssignmentTeams.ToList();
            foreach (var tm in from at in assignmentTeams from tm in at.Team.TeamMembers where tm.CourseUser.UserProfileID == user.ID select tm)
            {
                teamMember = tm;
            }

            return teamMember;
        }

        /// <summary>
        /// Retrieves the team for the provided user profile / assignment combo.
        /// If the user is not apart of any team, then he will be added to a new 
        /// team with him as the sole member.
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static AssignmentTeam GetAssignmentTeam(Assignment assignment, CourseUser user)
        {
            return user.TeamMemberships
                       .SelectMany(t => t.Team.UsedAsAssignmentTeam)
                       .FirstOrDefault(a => a.AssignmentID == assignment.ID);
        }


        /// <summary>
        /// Returns a discussion team for a discussion assignment. 
        /// Note: this isn't reliable for Critical Review Discussions as a user can be on multiple discussion teams
        /// </summary>
        /// <param name="assignment">Discussion Assignment</param>
        /// <param name="user"></param>
        /// <returns>discussionteam user is on</returns>
        public static DiscussionTeam GetDiscussionTeam(Assignment assignment, CourseUser user)
        {
            return (from dt in assignment.DiscussionTeams from tm in dt.Team.TeamMembers where user.ID == tm.CourseUserID select dt).FirstOrDefault();
        }

        /// <summary>
        /// Returns the late penalty as a string. I.e. an assignmentTeam with 80% late penalty will yield the string
        /// "80.00 %"
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public static string GetLatePenaltyAsString(IAssignmentTeam team)
        {
            return (GetLatePenalty(team) / 100.0).ToString("P");
        }

        /// <summary>
        /// This gets the late penalty for a regular assignment. Will not currently work for Critical Review assignments
        /// Returns the late penalty as a percentage. I.e. 80% late penalty will return as 80.0
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public static double GetLatePenalty(IAssignmentTeam team)
        {
            var latePenalty = 0.0;
            DateTime? submissionTime = null;
            var dueDate = team.Assignment.DueDate + TimeSpan.FromMinutes(1); //Need initial value to get compiler to be quiet.
            if (team.Assignment.HasDeliverables)
            {
                //Handle late penaly based off deliverable submission time
                submissionTime = FileSystem.GetSubmissionTime(team);

                //Adding 1 minute to dueDate. This is to keep submissions turned in at 12:00am for an assignment with a due date of 12:00am as non-late.
                dueDate = team.Assignment.DueDate + TimeSpan.FromMinutes(1);
            }
            else if (team.Assignment.Type == AssignmentTypes.DiscussionAssignment || team.Assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
            {
                //Handle late penalty based off of initial post due date.

                //Note that the team sent in is a non-db saved, forged team. The teamID corrisponds to the correct team, but there is only one teammember
                //and that is the user we want the late penaly for.

                var cuID = team.Team.TeamMembers.First().CourseUserID;

                using (OSBLEContext db = new OSBLEContext())
                {
                    submissionTime = (from dp in db.DiscussionPosts
                                      where dp.AssignmentID == team.AssignmentID &&
                                      dp.CourseUserID == cuID
                                      orderby dp.Posted
                                      select dp.Posted).FirstOrDefault();
                }

                dueDate = team.Assignment.DiscussionSettings.InitialPostDueDate + TimeSpan.FromMinutes(1);
            }

            TimeSpan lateness;

            if (submissionTime != null && submissionTime != DateTime.MinValue)
            {
                lateness = (DateTime)submissionTime - dueDate;
            }
            else //if the assignment has not been submitted, use the current time to calculate late penalty.
            {
                lateness = DateTime.UtcNow - dueDate;
            }

            if (lateness.TotalHours >= team.Assignment.HoursLateWindow)
            {
                //The document (or post) is too late to be accepted. Therefor 100% late penalty
                latePenalty = 100;
            }
            else if (lateness.TotalHours <= 0)
            {
                //The document (or post) wasnt late at all, no late penalty
                latePenalty = 0;
            }
            else
            {
                //The document (or post) was late, but less than HoursLateWindow

                //So, to calculate the late penalty,we want integer division here to keep units whole. 
                //Example of applying late penalty: A submission is 25hours late, HoursPerDeduction is 24
                //gets 2 deductions. 1 for the (0,24] hour late range, and then another for the (24,48] hour late range.
                //Notice begining of ranges are non-inclusive. This was handled by adding 1 minute to dueDate above.

                var numberOfDeductions = 1;
                numberOfDeductions += ((int)lateness.TotalHours / (int)team.Assignment.HoursPerDeduction);
                latePenalty = numberOfDeductions * team.Assignment.DeductionPerUnit;

                //cannot have higher than 100% late penalty
                if (latePenalty > 100)
                {
                    latePenalty = 100;
                }
            }

            return latePenalty;

        }

        public static string[] GetFileExtensions(DeliverableType deliverableType)
        {
            var type = deliverableType.GetType();

            var fi = type.GetField(deliverableType.ToString());

            //we get the attributes of the selected language
            var attrs = (fi.GetCustomAttributes(typeof(FileExtensions), false) as FileExtensions[]);

            //make sure we have more than (should be exactly 1)
            if (attrs != null && (attrs.Length > 0 && attrs[0] != null))
            {
                return attrs[0].Extensions;
            }

            //throw and exception if not decorated with any attrs because it is a requirement
            throw new Exception("Languages must have be decorated with a FileExtensionAttribute");
        }

        protected List<SelectListItem> GetListOfDeliverableTypes()
        {
            var fileTypes = new List<SelectListItem>();
            var i = 0;
            var deliverable = (DeliverableType)i;
            while (Enum.IsDefined(typeof(DeliverableType), i))
            {
                var type = deliverable.GetType();

                var fi = type.GetField(deliverable.ToString());

                //we get the attributes of the selected language
                var attrs = (fi.GetCustomAttributes(typeof(FileExtensions), false) as FileExtensions[]);

                //make sure we have more than (should be exactly 1)
                if (attrs != null && (attrs.Length > 0 && attrs[0] != null))
                {
                    //we get the first attributes value which should be the fileExtension
                    var s = deliverable.ToString();
                    s += " (";
                    s += string.Join(", ", attrs[0].Extensions);
                    s += ")";

                    var sli = new SelectListItem
                    {
                        Text = s,
                        Value = i.ToString()
                    };

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
            var cu = (from c in db.CourseUsers
                      where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                      && c.UserProfileID == user.ID
                      select c).FirstOrDefault();
            if (cu != null)
            {
                db.CourseUsers.Remove(cu);
            }
            db.SaveChanges();

            var teamsWithNoMembers = (from at in db.AssignmentTeams
                                      where at.Team.TeamMembers.Count == 0
                                      select at).ToList();
            foreach (var team in teamsWithNoMembers)
            {
                db.AssignmentTeams.Remove(team);
            }
            db.SaveChanges();
        }

        /// <summary>
        /// Sets a user's status to "withdrawn" for the current course
        /// </summary>
        /// <param name="user"></param>
        public void WithdrawUserFromCourse(UserProfile user)
        {
            var cu = (from c in db.CourseUsers
                      where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                      && c.UserProfileID == user.ID
                      select c).FirstOrDefault();
            if (cu != null)
            {
                if (cu.AbstractRole.GetType() == typeof(CommunityRole))
                {
                    RemoveUserFromCourse(user);
                }
                cu.AbstractRoleID = (int)CourseRole.CourseRoles.Withdrawn;
                db.Entry(cu).State = EntityState.Modified;
            }
            db.SaveChanges();
        }
    }
}
