using System.Data;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    public class CourseController : OSBLEController
    {
        private OSBLEContext db = new OSBLEContext();

        //
        // GET: /Course/

        public ViewResult Index()
        {
            ViewBag.CurrentUser = currentUser;

            ViewBag.Courses = from d in currentCourses select d.Course;

            return View(db.Courses.ToList());
        }

        //
        // GET: /Course/Create

        public ActionResult Create()
        {
            if (currentUser.CanCreateCourses)
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index");
            }
        }

        //
        // POST: /Course/Create

        [HttpPost]
        public ActionResult Create(Course course)
        {
            if (currentUser.CanCreateCourses)
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
            }
            else
            {
                return RedirectToAction("Index");
            }

            return View(course);
        }

        //
        // GET: /Course/Edit/5

        public ActionResult Edit(int id)
        {
            if (canModifyCourse(id))
            {
                Course course = db.Courses.Find(id);
                return View(course);
            }
            return RedirectToAction("Index");
        }

        //
        // POST: /Course/Edit/5

        [HttpPost]
        public ActionResult Edit(Course course)
        {
            if (canModifyCourse(course.ID))
            {
                if (ModelState.IsValid)
                {
                    db.Entry(course).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            else
            {
                return RedirectToAction("Index");
            }
            return View(course);
        }

        //
        // GET: /Course/Delete/5

        public ActionResult Delete(int id)
        {
            if (canModifyCourse(id))
            {
                Course course = db.Courses.Find(id);
                return View(course);
            }
            return RedirectToAction("Index");
        }

        //
        // POST: /Course/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            if (canModifyCourse(id))
            {
                Course course = db.Courses.Find(id);
                db.Courses.Remove(course);
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

        protected bool canModifyCourse(int courseID)
        {
            var courseUsers = (from c in currentCourses where c.CourseID == courseID select c).FirstOrDefault();
            if (courseUsers == null)
            {
                return false;
            }
            else
            {
                return courseUsers.CourseRole.CanModify;
            }
        }
    }
}