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
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities.Scores;

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

        protected CoursesUsers activeCourse = null;

        public CoursesUsers ActiveCourse
        {
            get { return activeCourse; }
        }

        protected HttpContext context = System.Web.HttpContext.Current;
        protected List<CoursesUsers> currentCourses = new List<CoursesUsers>();

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
            // If logged in, feed user profile to view.

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
                List<CoursesUsers> allUsersCourses = db.CoursesUsers.Where(cu => cu.UserProfileID == currentUser.ID).ToList();

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
                List<CoursesUsers> activeCoursePool = currentCourses.Where(cu => cu.Hidden == false).ToList();

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

        public static List<UserProfile> GetAllUsers(TeamUserMember teamUser)
        {
            List<UserProfile> users = new List<UserProfile>();

            if (teamUser is UserMember)
            {
                users.Add((teamUser as UserMember).UserProfile);
            }
            else if (teamUser is TeamMember)
            {
                foreach (TeamUserMember member in (teamUser as TeamMember).Team.Members)
                {
                    users.AddRange(GetAllUsers(member));
                }
            }
            return users;
        }

        public static TeamUserMember GetTeamUser(AbstractAssignmentActivity activity, UserProfile user)
        {
            var teamUser = (from c in activity.TeamUsers where c.Contains(user) == true select c).FirstOrDefault();

            return teamUser;
        }

        public AbstractAssignmentActivity GetNextActivity(AbstractAssignmentActivity activity)
        {
            var list = (from c in activity.AbstractAssignment.AssignmentActivities orderby activity.ReleaseDate select c).ToList();
            int index = list.IndexOf(activity);
            if (index + 1 < list.Count)
            {
                return list[index + 1];
            }
            return null;
        }

        public DateTime? GetDueDate(AbstractAssignmentActivity activity)
        {
            var nextActivity = GetNextActivity(activity);
            if (nextActivity != null)
            {
                return nextActivity.ReleaseDate;
            }
            return null;
        }

        protected DateTime? GetSubmissionTime(Course course, AbstractAssignmentActivity activity, TeamUserMember teamUser)
        {
            DirectoryInfo submissionFolder = new DirectoryInfo(FileSystem.GetTeamUserSubmissionFolder(false, course, activity.ID, teamUser));

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

        public TimeSpan? calculateLateness(Course course, AbstractAssignmentActivity activity, TeamUserMember teamUser)
        {
            DateTime? dueDate = GetDueDate(activity);
            DateTime? submissionTime = GetSubmissionTime(course, activity, teamUser);

            TimeSpan? lateness = dueDate - submissionTime;

            return lateness;
        }

        protected double CalcualateLatePenaltyPercent(AbstractAssignmentActivity activity, TimeSpan lateness)
        {
            //Purposefully lose of data being nice
            if (activity.MinutesLateWithNoPenalty > (int)lateness.TotalMinutes)
            {
                //Purposefully lose of data as this doesn't come into effect until you have past it
                if (activity.HoursLateUntilZero < (int)(lateness.TotalHours))
                {
                    //100 percent of points deducted for being late.... ouch :p
                    return 100.00;
                }
                else
                {
                    //If the lateness is a negative number we need to find the absolute value of 
                    //that number for our calculations
                    if (lateness.TotalHours < 0)
                    {
                        lateness = lateness.Negate();
                    }

                    if (lateness.TotalHours < activity.HoursLatePerPercentPenalty)
                    {
                        return activity.PercentPenalty;
                    }

                    else
                    {
                        //Double the hours late per percent penalty and loop through until the lateness is 
                        //less than the hours.
                        int hoursLatePerPercentPenalty = activity.HoursLatePerPercentPenalty + activity.HoursLatePerPercentPenalty;
                        while (lateness.TotalHours > hoursLatePerPercentPenalty)
                        {
                            hoursLatePerPercentPenalty += activity.HoursLatePerPercentPenalty;
                        }

                        //return the percent penalty times the number of hours late.
                        return activity.PercentPenalty * (hoursLatePerPercentPenalty / activity.HoursLatePerPercentPenalty);
                    }                    
                }
            }
            return 0;
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
            //The relationship between users and courses is expressed in CoursesUsers, but
            //there exists plenty of other relationships between users and other course
            //particulars.  Perhaps this isn't good design, but we're kind of stuck at this point.
            //In order to keep the course from having a bunch of orphaned items, we must manually
            //delete some additional information.

            //might as well delete the big daddy to start
            CoursesUsers cu =  (from c in db.CoursesUsers
                    where c.AbstractCourseID == activeCourse.AbstractCourseID
                    && c.UserProfileID == user.ID
                    select c).FirstOrDefault();
            if (cu != null)
            {
                db.CoursesUsers.Remove(cu);
            }

            //remove this user from any assignments.
            var activities = (from category in db.Categories
                        join assignment in db.AbstractAssignments on category.ID equals assignment.CategoryID
                        join activity in db.AbstractAssignmentActivities on assignment.ID equals activity.AbstractAssignmentID
                        where category.CourseID == activeCourse.AbstractCourseID
                        select activity).SelectMany(a => a.TeamUsers).ToList();
            
            foreach (TeamUserMember teamUser in activities)
            {
                if (teamUser is TeamMember)
                {
                    TeamMember member = teamUser as TeamMember;
                    member.Team.Remove(user);

                    //AC: What should be done if that team is now empty?
                    //    I initially tried to remove the team, but that seemed to cause
                    //    some sort of runtime error.
                }
                else
                {
                    if (teamUser.Contains(user))
                    {
                        db.TeamUsers.Remove(teamUser);
                    }
                }
            }
            db.SaveChanges();
        }

        /// <summary>
        /// Changes userId's grade for the given assignmentId to value. Note: Value is the pre-modified value for the grade.
        /// </summary>
        public void ModifyGrade(double value, string userId, int assignmentActivityId)
        {
            //Continue if we have a valid gradable ID
            if (assignmentActivityId != 0)
            {
                double latePenalty = 0.0;
                //Get student
                var user = (from u in db.UserProfiles where u.Identification == userId select u).FirstOrDefault();

                if (user != null)
                {
                    double rawValue = value;
                    List<Score> gradableQuery = (from g in db.Scores
                                                 where g.AssignmentActivityID == assignmentActivityId
                                                 select g).ToList();

                    Score grades = (from grade in gradableQuery
                                    where grade.TeamUserMember.Contains(user)
                                    select grade).FirstOrDefault();

                    var assignmentQuery = from a in db.AbstractAssignmentActivities
                                          where a.ID == assignmentActivityId
                                          select a;

                    var currentAssignment = assignmentQuery.FirstOrDefault();
                    var teamuser = from c in currentAssignment.TeamUsers where c.Contains(user) select c;
                    Category currentCategory = currentAssignment.AbstractAssignment.Category;

                    if (grades != null) //there is a score in the db for the userId
                    {
                        TimeSpan? lateness = calculateLateness(currentAssignment.AbstractAssignment.Category.Course, currentAssignment, teamuser.First());
                        if (lateness != null) //asigning late penalty if there is lateness
                        {
                            latePenalty = CalcualateLatePenaltyPercent(currentAssignment, (TimeSpan)lateness);
                            latePenalty = (100 - latePenalty) / 100;
                            value = value * latePenalty;
                        }

                        if (currentCategory.MaxAssignmentScore >= 0) //capping to max score if there is a cap
                        {
                            if (((value / grades.AssignmentActivity.PointsPossible) * 100) > currentCategory.MaxAssignmentScore)
                            {
                                value = (currentAssignment.PointsPossible * (currentCategory.MaxAssignmentScore / 100));
                            }
                        }

                        if (grades.Points == value)
                        {
                            //Don't do anything to the points because our value coming in equals the points in the db.
                            //However, we do need to set the raw value in case that changed.
                            grades.RawPoints = rawValue;
                            db.SaveChanges();
                        }
                        else
                        {
                            if (grades.ManualLatePenaltyPercent >= 0) //assinging late penalty again...?
                            {
                                value = (value * ((100 - grades.ManualLatePenaltyPercent) / 100));
                            }

                            grades.Points = value;
                            grades.AddedPoints = 0;
                            grades.LatePenaltyPercent = latePenalty;
                            grades.StudentPoints = -1;
                            grades.RawPoints = rawValue;
                            db.SaveChanges();
                        }
                    }


                    else //there was no Score in the db for the userId. Creating a new one and assigning value.
                    {
                        /*We dont have to consider
                        manual late penalty percent if there was no score because scores are created if there is a manual
                        late penalty percent given.*/
                        if (teamuser.Count() > 0) //theres at least 1 teamuser
                        {
                            TimeSpan? lateness = calculateLateness(currentAssignment.AbstractAssignment.Category.Course, currentAssignment, teamuser.First());
                            if (lateness != null) //calculating late penalty if there is lateness
                            {
                                latePenalty = CalcualateLatePenaltyPercent(currentAssignment, (TimeSpan)lateness);
                                latePenalty = (100 - latePenalty) / 100;
                                value = value * latePenalty;
                            }


                            if (currentCategory.MaxAssignmentScore > 0) //setting the scores max if the category has a max
                            {
                                if (((value / currentAssignment.PointsPossible) * 100) > currentCategory.MaxAssignmentScore)
                                {
                                    value = (currentAssignment.PointsPossible * (currentCategory.MaxAssignmentScore / 100));
                                }
                            }


                            Score newScore = new Score() //CREATING THE SCORE and adding to the db
                            {
                                TeamUserMember = teamuser.First(),
                                Points = value,
                                AssignmentActivityID = currentAssignment.ID,
                                PublishedDate = DateTime.Now,
                                isDropped = false,
                                LatePenaltyPercent = latePenalty,
                                StudentPoints = -1,
                                ManualLatePenaltyPercent = -1,
                                RawPoints = rawValue
                            };
                            db.Scores.Add(newScore);
                            db.SaveChanges();
                        }
                    }
                    if (currentAssignment.addedPoints > 0) //Adds points if there were any to add
                    {
                        ApplyAddPoints(assignmentActivityId, currentAssignment.addedPoints);
                    }
                }
            }
        }

        /// <summary>
        /// Adds points to an assignment
        /// </summary>
        public void ApplyAddPoints(int assignmentActivityId, double number)
        {
            if (ModelState.IsValid)
            {
                if (assignmentActivityId > 0)
                {
                    List<Score> grades = (from grade in db.Scores
                                          where grade.AssignmentActivityID == assignmentActivityId &&
                                          grade.Points >= 0
                                          select grade).ToList();

                    var assignment = (from assigns in db.AbstractAssignmentActivities
                                      where assigns.ID == assignmentActivityId
                                      orderby assigns.ColumnOrder
                                      select assigns).FirstOrDefault();

                    if (grades.Count() > 0)
                    {
                        foreach (Score item in grades)
                        {
                            if (item.AddedPoints > 0)
                            {
                                item.Points -= assignment.addedPoints;
                            }
                            item.Points += number;
                            item.AddedPoints = number;
                        }
                        assignment.addedPoints = number;
                        db.SaveChanges();
                    }
                }
            }
        }
    }
}