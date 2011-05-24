using System.Data;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models;
using OSBLE.Attributes;

namespace OSBLE.Controllers
{
    [Authorize]
    public class CourseController : OSBLEController
    {
        //
        // GET: /Course/

        public ActionResult Index()
        {
            return RedirectToAction("Edit");
        }

        //
        // GET: /Course/Create

        [CanCreateCourses]
        public ActionResult Create()
        {
                return View();
        }

        //
        // POST: /Course/Create

        [HttpPost]
        [CanCreateCourses]
        public ActionResult Create(Course course)
        {
                if (ModelState.IsValid)
                {
                    CoursesUsers courseUser = new CoursesUsers();
                    courseUser.CourseRoleID = 1;
                    courseUser.UserProfileID = currentUser.ID;
                    courseUser.CourseID = course.ID;
                    db.Courses.Add(course);
                    db.CoursesUsers.Add(courseUser);
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }

            return View(course);
        }

        //
        // GET: /Course/Edit/5

        [RequireActiveCourse]
        [CanModifyCourse]
        public ActionResult Edit()
        {
            setTab();
            Course course = db.Courses.Find(activeCourse.CourseID);
            return View(course);
        }

        //
        // POST: /Course/Edit/5

        [HttpPost]
        [RequireActiveCourse]
        [CanModifyCourse]
        public ActionResult Edit(Course course)
        {
            setTab();
            if (ModelState.IsValid)
            {
                db.Entry(course).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(course);
        }

        //
        // GET: /Course/Delete/5

        [RequireActiveCourse]
        [CanModifyCourse]
        public ActionResult Delete()
        {
            setTab();
            Course course = db.Courses.Find(ActiveCourse.CourseID);
            return View(course);
        }

        //
        // POST: /Course/Delete/5

        [RequireActiveCourse]
        [CanModifyCourse]
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed()
        {
            setTab();
            Course course = db.Courses.Find(ActiveCourse.CourseID);
            db.Courses.Remove(course);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        private void setTab()
        {
            if (ActiveCourse.Course.IsCommunity)
            {
                ViewBag.CurrentTab = "Community Settings";
            }
            else
            {
                ViewBag.CurrentTab = "Course Settings";
            }
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

    }
}