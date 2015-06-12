﻿using System;
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
using OSBLE.Utility;
using System.Runtime.Caching;

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
                //reduces db calls
                if (_currentUser == null)
                {
                    _currentUser = OsbleAuthentication.CurrentUser;
                }
                return _currentUser;
            }
        }

        private CourseUser activeCourseUser = null;

        public CourseUser ActiveCourseUser
        {
            get
            {
                return activeCourseUser;
            }
            protected set
            {
                activeCourseUser = value;
            }
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
                if (OsbleAuthentication.CurrentUser != null && OsbleAuthentication.CurrentUser.IsApproved)
                {
                    Cache = FileCacheHelper.GetCacheInstance(OsbleAuthentication.CurrentUser);

                    setupInitialDatabaseData();

                    setupMenu();

                    setCurrentUserProfile();

                    GetEnrolledCourses();

                    setCourseListTitle();

                    setDashboardDisplayMode();
                }
            }
        }

        public void UpdateCacheInstance(UserProfile user)
        {
            Cache = FileCacheHelper.GetCacheInstance(user);
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
        private void setDashboardDisplayMode()
        {
            // if not set or erroniously set
            //     set the activity feed to display a single course
            // otherwise
            //     do nothing because it has been set by the user (call to HomeController's SetDashboardMode method)
            bool returnValue = false;
            object cacheResult = Cache["DashboardSingleCourseMode"];
            if(cacheResult == null || bool.TryParse(cacheResult.ToString(), out returnValue))
            {
                returnValue = true;
                Cache["DashboardSingleCourseMode"] = returnValue;
            }

            DashboardSingleCourseMode = ViewBag.DashboardSingleCourseMode = returnValue;
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
            if (CurrentUser != null)
            {
                // Sends the ViewBag the amount of unread mail messages the user has.
                SetUnreadMessageCount();
                List<CourseUser> allUsersCourses = db.CourseUsers.Where(cu => cu.UserProfileID == CurrentUser.ID).ToList();

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

                //int sessionAc = 0;// = Cache["ActiveCourse"];
                var sessionAc = Cache["ActiveCourse"];


                if (sessionAc == null || !(sessionAc is int))
                {
                    // On login or invalid ActiveCourse, set to user's default course.
                    activeCourseID = CurrentUser.DefaultCourse;
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
                if ((ActiveCourseUser = activeCoursePool.Where(cu => cu.AbstractCourseID == activeCourseID).FirstOrDefault()) == null)
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

        private void setCurrentUserProfile()
        {
            ViewBag.CurrentUser = OsbleAuthentication.CurrentUser;
        }

        public void SetUnreadMessageCount()
        {
            ViewBag.UnreadMessageCount = (int)db.Mails.Where(m => (m.ToUserProfileID == CurrentUser.ID) && (m.Read == false) && (m.DeleteFromInbox == false)).Count();
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
        /// Retrieves the team for the provided user profile / assignment combo.
        /// If the user is not apart of any team, then he will be added to a new 
        /// team with him as the sole member.
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static AssignmentTeam GetAssignmentTeam(Assignment assignment, CourseUser user)
        {
            var basic = user.TeamMemberships
                                             .SelectMany(t => t.Team.UsedAsAssignmentTeam);
            var query = basic
                                             .Where(a => a.AssignmentID == assignment.ID);
            AssignmentTeam returnValue = user.TeamMemberships
                                             .SelectMany(t => t.Team.UsedAsAssignmentTeam)
                                             .Where(a => a.AssignmentID == assignment.ID)
                                             .FirstOrDefault();
            return returnValue;
        }

        
        /// <summary>
        /// Returns a discussion team for a discussion assignment. 
        /// Note: this isn't reliable for Critical Review Discussions as a user can be on multiple discussion teams
        /// </summary>
        /// <param name="assignment">Discussion Assignment</param>
        /// <returns>discussionteam user is on</returns>
        public static DiscussionTeam GetDiscussionTeam(Assignment assignment, CourseUser user)
        {
            foreach(DiscussionTeam dt in assignment.DiscussionTeams)
            {
                foreach(TeamMember tm in dt.Team.TeamMembers)
                {
                    if(user.ID == tm.CourseUserID)
                    {
                        return dt;
                    }
                }
            }
            return null;
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
            double latePenalty = 0.0;
            DateTime? submissionTime = null;
            DateTime dueDate = team.Assignment.DueDate + TimeSpan.FromMinutes(1); //Need initial value to get compiler to be quiet.
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

                int cuID = team.Team.TeamMembers.FirstOrDefault().CourseUserID;

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
                lateness = (DateTime)submissionTime - (DateTime)dueDate;
            }
            else //if the assignment has not been submitted, use the current time to calculate late penalty.
            {
                lateness = DateTime.Now - (DateTime)dueDate;
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

                int numberOfDeductions = 1;
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
                    where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID
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
    }
}
