using System;
using System.Collections.Generic;
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

        public ActionResult Error()
        {
            return View();
        }

        public ActionResult CoursesRemove()
        {
            List<Course> courses = new List<Course>();
            using (LocalContext db = new LocalContext())
            {
                courses = (from u in db.Courses select u).ToList();
                foreach (Course course in courses)
                {
                    db.Courses.Remove(course);
                }
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
    }
}
