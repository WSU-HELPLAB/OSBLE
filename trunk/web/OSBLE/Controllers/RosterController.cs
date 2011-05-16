using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models;
using OSBLE.Attributes;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    public class RosterController : OSBLEController
    {
        public RosterController()
        {
            ViewBag.CurrentTab = "Users";
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

                //Since we are using the db above we need a new one for the below operation
                using (OSBLEContext db2 = new OSBLEContext())
                {
                    //Get all the users for each role
                    foreach (CourseRole role in db2.CourseRoles)
                    {
                        UsersByRole usersByRole = new UsersByRole();
                        usersByRole.RoleName = role.Name;
                        usersByRole.Users = new List<UserProfile>(from c in section
                                                                  where role.ID == c.CourseRole.ID
                                                                  select c.UserProfile);
                        usersByRole.Count = usersByRole.Users.Count;

                        usersByRoles.Add(usersByRole);
                    }
                }


                //reverse it so the least important people are first
                usersByRoles.Reverse();

                userBySection.UsersByRole = usersByRoles;

                usersBySections.Add(userBySection);
            }



            ViewBag.UsersBySections = usersBySections;

            ViewBag.CanEditSelf = CanModifyOwnLink(activeCourse);

            return View();
        }
        //
        // GET: /Roster/Create
        [CanModifyCourse]
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
        [CanModifyCourse]
        public ActionResult Create(CoursesUsers courseuser)
        {
            //if modelState isValid
                if (ModelState.IsValid && courseuser.CourseRoleID != 0)
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
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        ModelState.AddModelError("", "This ID Number already exists in this class");
                        ViewBag.CourseRoleID = new SelectList(db.CourseRoles, "ID", "Name");
                        return View();
                    }
            }
            return RedirectToAction("Index");
        }

        //
        // GET: /Roster/Edit/5
        [CanModifyCourse]
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
        // GET: /Roster/Delete/5
        [CanModifyCourse]
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
        [CanModifyCourse]
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
    }
}
