using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models;
using OSBLE.Attributes;
using System.IO;
using LumenWorks.Framework.IO.Csv;

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
        [CanModifyCourse]
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
                List<CourseRole> roles = new List<CourseRole>();

                // Set custom role order for display
                List<CourseRole.OSBLERoles> rolesOrder = new List<CourseRole.OSBLERoles>(new CourseRole.OSBLERoles[] { 
                            CourseRole.OSBLERoles.Student,
                            CourseRole.OSBLERoles.TA,
                            CourseRole.OSBLERoles.Moderator,
                            CourseRole.OSBLERoles.Observer,
                            CourseRole.OSBLERoles.Instructor });

                foreach (CourseRole.OSBLERoles r in rolesOrder)
                {
                    roles.Add(db.CourseRoles.Find((int)r));
                }

                foreach (CourseRole role in roles)
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

            return View();
        }

        public ActionResult ImportRoster()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ImportRoster(HttpPostedFileBase file, string columnName)
        {
            if (file.ContentLength > 0)
            {
                //var fileName = Path.GetFileName(file.FileName);
                //var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName);
                //file.SaveAs(path);
                Stream s = file.InputStream;
                List<RosterEntry> rosterEntries = ParseRoster(s, columnName);

                if (rosterEntries.Count > 0)
                {
                    var students = from c in db.CoursesUsers where c.CourseID == activeCourse.CourseID && c.CourseRoleID == (int)CourseRole.OSBLERoles.Student select c;
                    foreach (CoursesUsers student in students)
                    {
                        db.CoursesUsers.Remove(student);
                    }
                    db.SaveChanges();
                    
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
                        catch
                        {
                            throw new Exception("There was an error importing the Roster");
                        }
                    }
                }

            }

            return RedirectToAction("Index");
        }

        //
        // GET: /Roster/Create
        public ActionResult Create()
        {
                //ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName");
                ViewBag.CourseID = new SelectList(db.Courses, "ID", "Name");
                ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name");
                return View();
        }

        //
        // POST: /Roster/Create

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


        //Students
        //

        //
        // GET: /Roster/Edit/5
        public ActionResult Edit(int userProfileID)
        {
            CoursesUsers coursesusers = getCoursesUsers(userProfileID);
            if (CanModifyOwnLink(coursesusers))
            {
                ViewBag.UserProfileID = new SelectList(db.UserProfiles, "ID", "UserName", coursesusers.UserProfileID);
                ViewBag.CourseID = new SelectList(db.Courses, "ID", "Prefix", coursesusers.CourseID);
                ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name", coursesusers.CourseRoleID);
                return View(coursesusers);
            }
            return RedirectToAction("Index");
        }

        //
        // POST: /Roster/Edit/5

        [HttpPost]
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
        // GET: /Roster/Delete/5
        public ActionResult Delete(int userProfileID)
        {
            CoursesUsers coursesusers = getCoursesUsers(userProfileID);
            if (CanModifyOwnLink(coursesusers))
            {
                return View(coursesusers);
            }
            return RedirectToAction("Index");
        }

        //
        // POST: /Roster/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int userProfileID)
        {
            CoursesUsers coursesusers = getCoursesUsers(userProfileID);
            if(CanModifyOwnLink(coursesusers))
            {
                db.CoursesUsers.Remove(coursesusers);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return RedirectToAction("Index");
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

        private List<RosterEntry> ParseRoster(Stream roster, string idNumberColumnName)
        {
            StreamReader sr = new StreamReader(roster);
            CachedCsvReader csvReader = new CachedCsvReader(sr, true);

            List<RosterEntry> rosterData = new List<RosterEntry>();

            string section = "Section";
            bool hasSectionInfo = false;

            hasSectionInfo = csvReader.GetFieldHeaders().Contains(section);

            csvReader.MoveToStart();
            while (csvReader.ReadNextRecord())
            {
                int sectionNum;
                RosterEntry entry = new RosterEntry();
                entry.Identification = csvReader[csvReader.GetFieldIndex(idNumberColumnName)];
                if (hasSectionInfo)
                {
                    int.TryParse(csvReader[csvReader.GetFieldIndex(section)], out sectionNum);
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
        /// This sets up everything for the coruseUser and will create a new UserProfile if it doesn't not exist.
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
                        throw new Exception("This courseUser would not be unique if added");
                    }
        }

    }
}
