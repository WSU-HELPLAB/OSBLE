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
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [CanModifyCourse]
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
        public ActionResult Index()
        {
            //Get all users for the current class
            var users = (from c in db.CoursesUsers
                         where c.CourseID == activeCourse.CourseID
                         select c);

            var usersGroupedBySection = users.GroupBy(CoursesUsers => CoursesUsers.Section).OrderBy(CoursesUsers => CoursesUsers.Key).ToList();

            List<UsersBySection> usersBySections = new List<UsersBySection>();

            foreach (var section in usersGroupedBySection)
            {
                UsersBySection userBySection = new UsersBySection();
                userBySection.SectionNumber = section.Key.ToString();
                List<UsersByRole> usersByRoles = new List<UsersByRole>();

                //Get all the users for each role
                List<AbstractRole> roles = new List<AbstractRole>();

                if (ActiveCourse.Course is Course)
                {
                    // Set custom role order for display
                    List<CourseRole.OSBLERoles> rolesOrder = new List<CourseRole.OSBLERoles>();

                    int i = (int)CourseRole.OSBLERoles.Instructor;
                    while (Enum.IsDefined(typeof(CourseRole.OSBLERoles), i))
                    {
                        rolesOrder.Add((CourseRole.OSBLERoles)i);
                        i++;
                    }

                    foreach (CourseRole.OSBLERoles r in rolesOrder)
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
                                                              where role.ID == c.CourseRole.ID
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

            ViewBag.CanEditSelf = CanModifyOwnLink(activeCourse);

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
                }

                ViewBag.Headers = headers;
                ViewBag.GuessedSection = guessedSection;
                ViewBag.GuessedIdentification = guessedIdentification;

                return View();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [CanModifyCourse]
        [NotForCommunity]
        public ActionResult ApplyRoster(string idColumn, string sectionColumn)
        {
            HttpPostedFileBase file = Session["RosterFile"] as HttpPostedFileBase;

            int rosterCount = 0;

            if ((file != null) && (idColumn != null))
            {
                Stream s = file.InputStream;
                List<RosterEntry> rosterEntries = parseRoster(s, idColumn, sectionColumn);

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
                    List<CoursesUsers> otherMembers = db.CoursesUsers.Where(c => c.CourseID == activeCourse.CourseID && c.CourseRoleID != (int)CourseRole.OSBLERoles.Student).ToList();
                    foreach (CoursesUsers member in otherMembers)
                    {
                        if (usedIdentifications.Contains(member.UserProfile.Identification))
                        {
                            ViewBag.Error = "There is a non-student (" + member.UserProfile.FirstName + " " + member.UserProfile.LastName + ") in the course with the same School ID as a student on the roster. Please check your roster and try again.";
                            return View("RosterError");
                        }
                    }

                    // No duplicate IDs, so we can remove the current course links to add the new ones.
                    var students = from c in db.CoursesUsers where c.CourseID == activeCourse.CourseID && c.CourseRoleID == (int)CourseRole.OSBLERoles.Student select c;
                    foreach (CoursesUsers student in students)
                    {
                        db.CoursesUsers.Remove(student);
                    }
                    db.SaveChanges();

                    // Attach to users or add new user profile stubs.
                    foreach (RosterEntry entry in rosterEntries)
                    {
                        CoursesUsers courseUser = new CoursesUsers();
                        courseUser.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
                        courseUser.Section = entry.Section;
                        courseUser.UserProfile = new UserProfile();
                        courseUser.UserProfile.Identification = entry.Identification;
                        try
                        {
                            createCourseUser(courseUser);
                        }
                        catch (Exception e)
                        {
                            ViewBag.Error = e.Message;
                            return View("RosterError");
                        }
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
            ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name");
            return View();
        }

        //
        // POST: /Roster/Create
        [CanModifyCourse]
        [NotForCommunity]
        [HttpPost]
        public ActionResult Create(CoursesUsers courseuser)
        {
            //if modelState isValid
            if (ModelState.IsValid && courseuser.CourseRoleID != 0)
            {
                try
                {
                    createCourseUser(courseuser);
                }
                catch
                {
                    ModelState.AddModelError("", "This ID Number already exists in this class");
                    ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name");
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
            if (!(ViewBag.ActiveCourse.Course is Community))
            {
                ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name");
            }
            else // Community Roles
            {
                ViewBag.CourseRoleID = new SelectList(db.CommunityRoles, "ID", "Name");
            }
            return View();
        }

        //
        // POST: /Roster/CreateByEmail

        [HttpPost]
        [CanModifyCourse]
        public ActionResult CreateByEmail(CoursesUsers courseuser)
        {
            //if modelState isValid
            if (ModelState.IsValid && courseuser.CourseRoleID != 0)
            {
                try
                {
                    attachCourseUserByEmail(courseuser);
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", e.Message);
                    ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name");
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
            CoursesUsers coursesusers = getCoursesUsers(userProfileID);
            if (CanModifyOwnLink(coursesusers))
            {
                ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", coursesusers.UserProfileID);
                if (activeCourse.Course is Course)
                {
                    ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name", coursesusers.CourseRoleID);
                }
                else // Community Roles
                {
                    ViewBag.CourseRoleID = new SelectList(db.CommunityRoles, "ID", "Name");
                }
                return View(coursesusers);
            }
            return RedirectToAction("Index");
        }

        //
        // POST: /Roster/Edit/5

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Edit(CoursesUsers coursesusers)
        {
            if (CanModifyOwnLink(coursesusers))
            {
                if (ModelState.IsValid)
                {
                    db.Entry(coursesusers).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
                ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", coursesusers.UserProfileID);
                ViewBag.CourseID = new SelectList(db.Courses, "ID", "Prefix", coursesusers.CourseID);
                ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name", coursesusers.CourseRoleID);
                return View(coursesusers);
            }
            return RedirectToAction("Index");
        }

        //
        // POST: /Roster/Delete/5

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Delete(int userProfileID)
        {
            CoursesUsers coursesusers = getCoursesUsers(userProfileID);
            if ((coursesusers != null) && CanModifyOwnLink(coursesusers))
            {
                db.CoursesUsers.Remove(coursesusers);
                db.SaveChanges();
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

        private CoursesUsers getCoursesUsers(int userProfileId)
        {
            return (from c in db.CoursesUsers
                    where c.CourseID == activeCourse.CourseID
                    && c.UserProfileID == userProfileId
                    select c).FirstOrDefault();
        }

        /// <summary>
        /// This says can the passed courseUser Modify the course and if so is there another teacher
        /// that can also modify this course if so it returns true else returns false
        /// </summary>
        /// <param name="courseUser"></param>
        /// <returns></returns>
        private bool CanModifyOwnLink(CoursesUsers courseUser)
        {
            var diffTeacher = (from c in db.CoursesUsers
                               where (c.CourseID == courseUser.CourseID
                               && c.CourseRole.CanModify == true
                               && c.UserProfileID != courseUser.UserProfileID)
                               select c);

            if (courseUser.UserProfile != currentUser || diffTeacher.Count() > 0)
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

        private List<RosterEntry> parseRoster(Stream roster, string idNumberColumnName, string sectionColumnName)
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

            if (userProfile == currentUser.ID)
            {
                show = true;
            }
            else
            {
                CoursesUsers cu = db.CoursesUsers.Where(c => (c.CourseID == activeCourse.CourseID) && (c.UserProfileID == userProfile)).FirstOrDefault();

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
        private void createCourseUser(CoursesUsers courseuser)
        {
            //This will return one if they exist already or null if they don't
            var user = (from c in db.UserProfiles
                        where c.Identification == courseuser.UserProfile.Identification
                        select c).FirstOrDefault();
            if (user == null)
            {
                //user doesn't exist so we got to make a new one
                //Create userProfile with the new ID
                UserProfile up = new UserProfile();
                up.CanCreateCourses = false;
                up.IsAdmin = false;
                up.SchoolID = currentUser.SchoolID;
                up.Identification = courseuser.UserProfile.Identification;
                db.UserProfiles.Add(up);
                db.SaveChanges();

                //Set the UserProfileID to point to our new student
                courseuser.UserProfile = null;
                courseuser.UserProfileID = up.ID;
                courseuser.CourseID = activeCourse.CourseID;
            }
            else
            {
                courseuser.UserProfile = user;
                courseuser.UserProfileID = user.ID;
            }
            courseuser.CourseID = activeCourse.CourseID;
            //Check uniqueness
            if ((from c in db.CoursesUsers
                 where c.CourseID == courseuser.CourseID && c.UserProfileID == courseuser.UserProfileID
                 select c).Count() == 0)
            {
                db.CoursesUsers.Add(courseuser);
                db.SaveChanges();
            }
            else
            {
                throw new Exception("Attempted to add a student with the same School ID as a non-student (Instructor, TA, etc.) already in the class. Please check your roster and try again.");
            }
        }

        /// <summary>
        /// Attempts to find an email address to match requested, and adds them to the course if it is found.
        /// </summary>
        /// <param name="courseUser"></param>
        private void attachCourseUserByEmail(CoursesUsers courseuser)
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

            courseuser.CourseID = activeCourse.CourseID;

            if ((from c in db.CoursesUsers
                 where c.CourseID == courseuser.CourseID && c.UserProfileID == courseuser.UserProfileID
                 select c).Count() == 0)
            {
                db.CoursesUsers.Add(courseuser);
                db.SaveChanges();
            }
            else
            {
                throw new Exception("This user is already in the course!");
            }
        }
    }
}