using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AbetApp.Models;
using HtmlAgilityPack;

namespace AbetApp.Controllers
{
    public class HomeController : Controller
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            List<Course> courses = new List<Course>();
            using (LocalContext db = new LocalContext())
            {
                 courses = (from u in db.Courses select u).ToList();
            }
            return View(courses);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Index(FormCollection collection)
        {
            string url = collection["url"].ToString();

            List<Course> courseList = new List<Course>();

            try
            {
                //Create new HtmlWeb item
                HtmlWeb hw = new HtmlWeb();

                //Create html doc from url provided
                HtmlDocument doc = hw.Load(url);

                //Parse the course titles
                foreach (HtmlNode course in doc.DocumentNode.SelectNodes("//span[@class='course_header']"))
                {
                    string courseTitle = course.InnerText;
                    Course tmpCourse = new Course();
                    tmpCourse.Title = courseTitle;
                    courseList.Add(tmpCourse);
                }

                //Index counter
                int evalPlanIndex = 0;

                //Parse the course descriptions
                foreach (HtmlNode courseDiscrip in doc.DocumentNode.SelectNodes("//span[@class='course_data']"))
                {
                    //Get the descriptions
                    string courseDescrip = courseDiscrip.InnerText;
                    courseList[evalPlanIndex].Data = courseDescrip;
                    evalPlanIndex++;
                }

                using (LocalContext db = new LocalContext())
                {
                    Dictionary<int, Course> CourseDict = new Dictionary<int, Course>();
                    List<Course> cleanList = new List<Course>();
                    foreach (Course course in courseList)
                    {
                        course.CourseNum = ParseData.ParseCourseNum(course.Title);
                        course.Title = ParseData.ParseTitle(course.Title);
                        course.Major = "CPTS";
                        if (CourseDict.ContainsKey(course.CourseNum) != true)
                        {
                            CourseDict.Add(course.CourseNum, course);
                            db.Courses.Add(course);
                            cleanList.Add(course);
                        }
                    }
                    db.SaveChanges();
                    ParseData.ParsePreReq(CourseDict, cleanList);
                }
            }
            catch (Exception e)
            {
                return RedirectToAction("Error");
            }
            return RedirectToAction("Index");
        }

        public ActionResult Delete(int id)
        {
            Course course = new Course();
            List<CourseRelation> relations = new List<CourseRelation>();
            using (LocalContext db = new LocalContext())
            {
                course = db.Courses.Find(id);
                if (course == null)
                {
                    return HttpNotFound();
                }

                relations = (from u in db.CourseRelations select u).ToList();

                foreach (CourseRelation relation in relations)
                {
                    if ((relation.ChildCourseId == id) || (relation.ParentCourseId == id))
                    {
                        db.CourseRelations.Remove(relation);
                    }
                }
                db.Courses.Remove(course);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            Course course = new Course();
            using (LocalContext db = new LocalContext())
            {
                course = db.Courses.Find(id);
                if (course == null)
                {
                    return HttpNotFound();
                }
            }
            return View(course);
        }

        [HttpPost]
        public ActionResult Edit(Course course)
        {
            using (LocalContext db = new LocalContext())
            {
                if (ModelState.IsValid)
                {
                    db.Entry(course).State = EntityState.Modified;
                    db.SaveChanges();
                    return RedirectToAction("Index");
                }
            }
            return View(course);
        }

        public ActionResult PreReqs(int id)
        {
            List<CourseRelation> relations = new List<CourseRelation>();
            using (LocalContext db = new LocalContext())
            {
                relations = (from u in db.CourseRelations select u).ToList();
            }
            return View(relations);
        }


        public ActionResult Error()
        {
            return View();
        }

        public ActionResult CoursesRemove()
        {
            List<Course> courses = new List<Course>();
            List<CourseRelation> courseRelation = new List<CourseRelation>();
            using (LocalContext db = new LocalContext())
            {
                courses = (from u in db.Courses select u).ToList();
                foreach (Course course in courses)
                {
                    db.Courses.Remove(course);
                }
                db.SaveChanges();

                courseRelation = (from u in db.CourseRelations select u).ToList();
                foreach (CourseRelation relation in courseRelation)
                {
                    db.CourseRelations.Remove(relation);
                }
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
