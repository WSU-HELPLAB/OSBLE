using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using LumenWorks.Framework.IO.Csv;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
    [RequireActiveCourse]
    public class RosterController : OSBLEController
    {
        public RosterController()
        {
            ViewBag.CurrentTab = "Users";
        }

        public class RosterEntry
        {
            public string Identification
            {
                get;
                set;
            }

            public int Section
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }
        }

        public class UsersBySection
        {
            public string SectionNumber
            {
                get;
                set;
            }

            public List<UsersByRole> UsersByRole
            {
                get;
                set;
            }
        }

        public class UsersByRole
        {
            public string RoleName
            {
                get;
                set;
            }

            public List<UserProfile> Users
            {
                get;
                set;
            }

            public int Count
            {
                get;
                set;
            }
        }

        //
        // GET: /Roster/
        [CanModifyCourse]
        public ActionResult Index()
        {
            //Get all users for the current class
            var users = (from c in db.CourseUsers
                         where c.AbstractCourseID == ActiveCourse.AbstractCourseID
                         select c);

            var usersGroupedBySection = users.GroupBy(CourseUser => CourseUser.Section).OrderBy(CourseUser => CourseUser.Key).ToList();

            List<UsersBySection> usersBySections = new List<UsersBySection>();

            foreach (var section in usersGroupedBySection)
            {
                UsersBySection userBySection = new UsersBySection();
                userBySection.SectionNumber = section.Key.ToString();
                List<UsersByRole> usersByRoles = new List<UsersByRole>();

                //Get all the users for each role
                List<AbstractRole> roles = new List<AbstractRole>();

                if (ActiveCourse.AbstractCourse is Course)
                {
                    // Set custom role order for display
                    List<CourseRole.CourseRoles> rolesOrder = new List<CourseRole.CourseRoles>();

                    int i = (int)CourseRole.CourseRoles.Instructor;
                    while (Enum.IsDefined(typeof(CourseRole.CourseRoles), i))
                    {
                        rolesOrder.Add((CourseRole.CourseRoles)i);
                        i++;
                    }

                    foreach (CourseRole.CourseRoles r in rolesOrder)
                    {
                        roles.Add(db.CourseRoles.Find((int)r));
                    }
                }
                else
                { // Community
                    // Set custom role order for display
                    List<CommunityRole.OSBLERoles> rolesOrder = new List<CommunityRole.OSBLERoles>();

                    int i = (int)CommunityRole.OSBLERoles.Leader;
                    while (Enum.IsDefined(typeof(CommunityRole.OSBLERoles), i))
                    {
                        rolesOrder.Add((CommunityRole.OSBLERoles)i);
                        i++;
                    }

                    foreach (CommunityRole.OSBLERoles r in rolesOrder)
                    {
                        roles.Add(db.CommunityRoles.Find((int)r));
                    }
                }

                foreach (AbstractRole role in roles)
                {
                    UsersByRole usersByRole = new UsersByRole();
                    usersByRole.RoleName = role.Name;
                    usersByRole.Users = new List<UserProfile>(from c in section
                                                              where role.ID == c.AbstractRole.ID
                                                              orderby c.UserProfile.LastName
                                                              select c.UserProfile
                                                              );
                    usersByRole.Count = usersByRole.Users.Count;

                    usersByRoles.Add(usersByRole);
                }

                //reverse it so the least important people are first

                userBySection.UsersByRole = usersByRoles;

                usersBySections.Add(userBySection);
            }

            ViewBag.UsersBySections = usersBySections;

            ViewBag.CanEditSelf = CanModifyOwnLink(ActiveCourse);

            if (Request.Params["notice"] != null)
            {
                ViewBag.Notice = Request.Params["notice"];
            }

            return View();
        }

        [HttpPost]
        [CanModifyCourse]
        [NotForCommunity]
        public ActionResult ImportRoster(HttpPostedFileBase file)
        {
            if ((file != null) && (file.ContentLength > 0))
            {
                // Save file into session
                Session["RosterFile"] = file;

                Stream s = file.InputStream;
                List<string> headers = getRosterHeaders(s);
                file.InputStream.Seek(0, 0);

                string guessedSection = null;
                string guessedIdentification = null;
                string guessedName = null;
                string guessedName2 = null;

                // Guess headers for section and identification
                foreach (string header in headers)
                {
                    if (guessedSection == null)
                    {
                        if (Regex.IsMatch(header, "section", RegexOptions.IgnoreCase))
                        {
                            guessedSection = header;
                        }
                    }

                    if (guessedIdentification == null)
                    {
                        if (Regex.IsMatch(header, "\\bident", RegexOptions.IgnoreCase)
                            || Regex.IsMatch(header, "\\bid\\b", RegexOptions.IgnoreCase)
                            || Regex.IsMatch(header, "\\bnumber\\b", RegexOptions.IgnoreCase)
                            )
                        {
                            guessedIdentification = header;
                        }
                    }

                    if (guessedName == null)
                    {
                        if (Regex.IsMatch(header, "name", RegexOptions.IgnoreCase))
                        {
                            guessedName = header;
                        }
                    }
                    else if (guessedName2 == null)
                    {
                        if (Regex.IsMatch(header, "name", RegexOptions.IgnoreCase))
                        {
                            guessedName2 = header;
                        }
                    }
                }

                ViewBag.Headers = headers;
                ViewBag.GuessedSection = guessedSection;
                ViewBag.GuessedIdentification = guessedIdentification;
                ViewBag.GuessedName = guessedName;
                ViewBag.GuessedName2 = guessedName2;

                return View();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [CanModifyCourse]
        [NotForCommunity]
        public ActionResult ApplyRoster(string idColumn, string sectionColumn, string nameColumn, string name2Column)
        {
            HttpPostedFileBase file = Session["RosterFile"] as HttpPostedFileBase;

            int rosterCount = 0;

            if ((file != null) && (idColumn != null) && (nameColumn != null))
            {
                Stream s = file.InputStream;

                List<RosterEntry> rosterEntries = parseRoster(s, idColumn, sectionColumn, nameColumn, name2Column);

                if (rosterEntries.Count > 0)
                {
                    rosterCount = rosterEntries.Count;

                    // First check to make sure there are no duplicates in the ID table.
                    List<string> usedIdentifications = new List<string>();
                    foreach (RosterEntry entry in rosterEntries)
                    {
                        if (usedIdentifications.Contains(entry.Identification))
                        {
                            ViewBag.Error = "There are duplicate Student IDs in your roster. Please ensure the proper Student ID header was selected and check your roster file.";
                            return View("RosterError");
                        }
                        else
                        {
                            usedIdentifications.Add(entry.Identification);
                        }
                    }

                    // Make sure no student has the same ID as existing non-student members.
                    List<CourseUser> otherMembers = db.CourseUsers.Where(c => c.AbstractCourseID == ActiveCourse.AbstractCourseID && c.AbstractRoleID != (int)CourseRole.CourseRoles.Student).ToList();
                    foreach (CourseUser member in otherMembers)
                    {
                        if (usedIdentifications.Contains(member.UserProfile.Identification))
                        {
                            ViewBag.Error = "There is a non-student (" + member.UserProfile.FirstName + " " + member.UserProfile.LastName + ") in the course with the same School ID as a student on the roster. Please check your roster and try again.";
                            return View("RosterError");
                        }
                    }

                    //Use the list of our old students to track changes between the current and new class roster.
                    //Students that exist on the old roster but do not appear on the new roster will
                    //be removed from the course
                    var oldRoster = from c in db.CourseUsers
                                    where c.AbstractCourseID == ActiveCourse.AbstractCourseID
                                    &&
                                    c.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                    select c;
                    List<UserProfile> orphans = oldRoster.Select(cu => cu.UserProfile).ToList();
                    List<CourseUser> newRoster = new List<CourseUser>();

                    string[] names = new string[2];
                    // Attach to users or add new user profile stubs.
                    foreach (RosterEntry entry in rosterEntries)
                    {
                        CourseUser courseUser = new CourseUser();
                        courseUser.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
                        courseUser.Section = entry.Section;
                        courseUser.UserProfile = new UserProfile();
                        courseUser.UserProfile.Identification = entry.Identification;
                        if (entry.Name != null)
                        {
                            names = entry.Name.Split(',');
                            string[] parseFirstName = names[1].Trim().Split(' ');
                            courseUser.UserProfile.FirstName = parseFirstName[0];
                            courseUser.UserProfile.LastName = names[0].Trim();
                        }
                        else
                        {
                            courseUser.UserProfile.FirstName = "Pending";
                            courseUser.UserProfile.LastName = string.Format("({0})", entry.Identification);
                        }
                        newRoster.Add(courseUser);
                        createCourseUser(courseUser);
                        orphans.Remove(courseUser.UserProfile);
                    }
                    db.SaveChanges();

                    //remove all orphans
                    foreach (UserProfile orphan in orphans)
                    {
                        RemoveUserFromCourse(orphan);
                    }
                }
            }
            else if ((idColumn == null))
            {
                ViewBag.Error = "You did not specify headers for Student ID. Please try again.";
                return View("RosterError");
            }
            else
            {
                ViewBag.Error = "Your roster file was not properly loaded. Please try again.";
                return View("RosterError");
            }

            return RedirectToAction("Index", new { notice = "Roster imported with " + rosterCount.ToString() + " students." });
        }

        //
        // GET: /Roster/Create
        [CanModifyCourse]
        [NotForCommunity]
        public ActionResult Create()
        {
            ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name");
            return View();
        }

        //
        // POST: /Roster/Create
        [CanModifyCourse]
        [NotForCommunity]
        [HttpPost]
        public ActionResult Create(CourseUser courseuser)
        {
            //if modelState isValid
            if (ModelState.IsValid && courseuser.AbstractRoleID != 0)
            {
                try
                {
                    createCourseUser(courseuser);
                }
                catch
                {
                    ModelState.AddModelError("", "This ID Number already exists in this class");
                    ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name");
                    return View();
                }
            }
            return RedirectToAction("Index");
        }

        //
        // GET: /Roster/CreateByEmail
        [CanModifyCourse]
        public ActionResult CreateByEmail()
        {
            if (!(ActiveCourse.AbstractCourse is Community))
            {
                ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name");
            }
            else // Community Roles
            {
                ViewBag.AbstractRoleID = new SelectList(db.CommunityRoles, "ID", "Name");
            }
            return View();
        }

        //
        // POST: /Roster/CreateByEmail

        [HttpPost]
        [CanModifyCourse]
        public ActionResult CreateByEmail(CourseUser courseuser)
        {
            //if modelState isValid
            if (ModelState.IsValid && courseuser.AbstractRoleID != 0)
            {
                try
                {
                    attachCourseUserByEmail(courseuser);
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                    ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name");
                    return View();
                }
            }
            return RedirectToAction("Index");
        }

        //Students
        //

        //
        // GET: /Roster/Edit/5
        [CanModifyCourse]
        public ActionResult Edit(int userProfileID)
        {
            CourseUser CourseUser = getCourseUser(userProfileID);
            if (CanModifyOwnLink(CourseUser))
            {
                ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", CourseUser.UserProfileID);
                if (ActiveCourse.AbstractCourse is Course)
                {
                    ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name", CourseUser.AbstractRoleID);
                }
                else // Community Roles
                {
                    ViewBag.AbstractRoleID = new SelectList(db.CommunityRoles, "ID", "Name");
                }
                return View(CourseUser);
            }
            return RedirectToAction("Index");
        }

        //
        // POST: /Roster/Edit/5

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Edit(CourseUser CourseUser)
        {
            if (CanModifyOwnLink(CourseUser))
            {
                if (ModelState.IsValid)
                {
                    db.Entry(CourseUser).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", CourseUser.UserProfileID);
                ViewBag.AbstractCourse = new SelectList(db.Courses, "ID", "Prefix", CourseUser.AbstractCourseID);
                ViewBag.AbstractRoleID = new SelectList(db.CourseRoles, "ID", "Name", CourseUser.AbstractRoleID);
                return View(CourseUser);
            }
            return RedirectToAction("Index");
        }

        //
        // POST: /Roster/Delete/5

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Delete(int userProfileID)
        {
            CourseUser CourseUser = getCourseUser(userProfileID);
            if ((CourseUser != null) && CanModifyOwnLink(CourseUser))
            {
                RemoveUserFromCourse(CourseUser.UserProfile);
            }
            else
            {
                Response.StatusCode = 403;
            }
            return View("_AjaxEmpty");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        private CourseUser getCourseUser(int userProfileId)
        {
            return (from c in db.CourseUsers
                    where c.AbstractCourseID == ActiveCourse.AbstractCourseID
                    && c.UserProfileID == userProfileId
                    select c).FirstOrDefault();
        }

        /// <summary>
        /// This says can the passed courseUser Modify the course and if so is there another teacher
        /// that can also modify this course if so it returns true else returns false
        /// </summary>
        /// <param name="courseUser"></param>
        /// <returns></returns>
        private bool CanModifyOwnLink(CourseUser courseUser)
        {
            var diffTeacher = (from c in db.CourseUsers
                               where (c.AbstractCourseID == courseUser.AbstractCourseID
                               && c.AbstractRole.CanModify == true
                               && c.UserProfileID != courseUser.UserProfileID)
                               select c);

            if (courseUser.UserProfile != CurrentUser || diffTeacher.Count() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private List<string> getRosterHeaders(Stream roster)
        {
            StreamReader sr = new StreamReader(roster);
            CachedCsvReader csvReader = new CachedCsvReader(sr, true);

            return csvReader.GetFieldHeaders().ToList();
        }

        private List<RosterEntry> parseRoster(Stream roster, string idNumberColumnName, string sectionColumnName, string nameColumnName, string name2ColumnName)
        {
            StreamReader sr = new StreamReader(roster);
            CachedCsvReader csvReader = new CachedCsvReader(sr, true);

            List<RosterEntry> rosterData = new List<RosterEntry>();

            bool hasSectionInfo = false;

            if (sectionColumnName != null)
            {
                hasSectionInfo = csvReader.GetFieldHeaders().Contains(sectionColumnName);
            }

            csvReader.MoveToStart();
            while (csvReader.ReadNextRecord())
            {
                int sectionNum;
                RosterEntry entry = new RosterEntry();
                entry.Identification = csvReader[csvReader.GetFieldIndex(idNumberColumnName)];

                if (nameColumnName != "")
                {
                    if (name2ColumnName != "")
                    {
                        entry.Name = csvReader[csvReader.GetFieldIndex(name2ColumnName)] + ", " + csvReader[csvReader.GetFieldIndex(nameColumnName)];
                    }
                    else
                    {
                        entry.Name = csvReader[csvReader.GetFieldIndex(nameColumnName)];
                    }
                }
                else
                {
                    entry.Name = null;
                }
                if (hasSectionInfo)
                {
                    int.TryParse(csvReader[csvReader.GetFieldIndex(sectionColumnName)], out sectionNum);
                    entry.Section = sectionNum;
                }
                else
                {
                    entry.Section = 0;
                }

                rosterData.Add(entry);
            }

            return rosterData;
        }

        [HttpGet, FileCache(Duration = 3600)]
        public FileStreamResult ProfilePicture(int userProfile)
        {
            bool show = false;
            UserProfile u = db.UserProfiles.Find(userProfile);

            if (userProfile == CurrentUser.ID)
            {
                show = true;
            }
            else
            {
                CourseUser cu = db.CourseUsers.Where(c => (c.AbstractCourseID == ActiveCourse.AbstractCourseID) && (c.UserProfileID == userProfile)).FirstOrDefault();

                if (cu != null)
                {
                    show = true;
                }
            }

            if (show == true)
            {
                return new FileStreamResult(FileSystem.GetProfilePictureOrDefault(u), "image/jpeg");
            }
            else
            {
                return new FileStreamResult(FileSystem.GetDefaultProfilePicture(), "image/jpeg");
            }
        }

        /// <summary>
        /// This sets up everything for the courseUser and will create a new UserProfile if it doesn't not exist.
        /// </summary>
        /// <param name="courseuser">It must have section, role set, and a reference to UserProfile with Identification set</param>
        private void createCourseUser(CourseUser courseuser)
        {
            //This will return one if they exist already or null if they don't
            var user = (from c in db.UserProfiles
                        where c.Identification == courseuser.UserProfile.Identification
                        && c.SchoolID == ActiveCourse.UserProfile.SchoolID
                        select c).FirstOrDefault();
            if (user == null)
            {
                //user doesn't exist so we got to make a new one
                //Create userProfile with the new ID
                UserProfile up = new UserProfile();
                up.CanCreateCourses = false;
                up.IsAdmin = false;
                up.SchoolID = CurrentUser.SchoolID;
                up.Identification = courseuser.UserProfile.Identification;

                if (courseuser.UserProfile.FirstName != null)
                {
                    up.FirstName = courseuser.UserProfile.FirstName;
                    up.LastName = courseuser.UserProfile.LastName;
                }
                else
                {
                    up.FirstName = "Pending";
                    up.LastName = string.Format("({0})", up.Identification);
                }
                db.UserProfiles.Add(up);
                db.SaveChanges();

                //Set the UserProfileID to point to our new student
                courseuser.UserProfile = up;
                courseuser.UserProfileID = up.ID;
                courseuser.AbstractCourseID = ActiveCourse.AbstractCourseID;
            }
            else
            {
                if (courseuser.UserProfile.FirstName != null)
                {
                    user.FirstName = courseuser.UserProfile.FirstName;
                    user.LastName = courseuser.UserProfile.LastName;
                    db.SaveChanges();
                }
                courseuser.UserProfile = user;
                courseuser.UserProfileID = user.ID;
                db.SaveChanges();
            }
            courseuser.AbstractCourseID = ActiveCourse.AbstractCourseID;
            //Check uniqueness
            if ((from c in db.CourseUsers
                 where c.AbstractCourseID == courseuser.AbstractCourseID && c.UserProfileID == courseuser.UserProfileID
                 select c).Count() == 0)
            {
                db.CourseUsers.Add(courseuser);
                db.SaveChanges();

                //If we already have assignments in the course, we need to add the new student into the class
                int currentCourseId = ActiveCourse.AbstractCourseID;
                List<Assignment> assignments = (from a in db.Assignments
                                                where a.Category.CourseID == currentCourseId
                                                select a).ToList();

                foreach (Assignment a in assignments)
                {
                    TeamMember userMember = new TeamMember()
                    {
                        CourseUserID = user.ID
                    };

                    Team team = new Team();
                    team.Name = courseuser.UserProfile.LastName + "," + courseuser.UserProfile.FirstName;
                    team.TeamMembers.Add(userMember);

                    db.Teams.Add(team);
                    db.SaveChanges();

                    AssignmentTeam assignmentTeam = new AssignmentTeam()
                    {
                        AssignmentID = a.ID,
                        Team = team,
                        TeamID = team.ID
                    };

                    db.AssignmentTeams.Add(assignmentTeam);
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Attempts to find an email address to match requested, and adds them to the course if it is found.
        /// </summary>
        /// <param name="courseUser"></param>
        private void attachCourseUserByEmail(CourseUser courseuser)
        {
            UserProfile up = db.UserProfiles.Where(u => u.UserName == courseuser.UserProfile.UserName).FirstOrDefault();

            if (up != null)
            {
                courseuser.UserProfile = up;
                courseuser.UserProfileID = up.ID;
            }
            else
            {
                throw new Exception("No user exists with that email address!");
            }

            courseuser.AbstractCourseID = ActiveCourse.AbstractCourseID;

            if ((from c in db.CourseUsers
                 where c.AbstractCourseID == courseuser.AbstractCourseID && c.UserProfileID == courseuser.UserProfileID
                 select c).Count() == 0)
            {
                db.CourseUsers.Add(courseuser);
                db.SaveChanges();
            }
            else
            {
                throw new Exception("This user is already in the course!");
            }
        }
    }
}