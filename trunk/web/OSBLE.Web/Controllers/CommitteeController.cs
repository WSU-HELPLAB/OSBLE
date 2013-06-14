using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Courses;
using OSBLE.Attributes;

namespace OSBLE.Controllers
{
    public class CommitteeController : OSBLEController
    {
        //
        // GET: /Committee/

        public ActionResult Index()
        {
            return View();
        }

        [CanCreateCourses]
        public ActionResult Create()
        {
            // Make a list of schools
            List<System.Web.Mvc.SelectListItem> schoolNames = 
                new List<System.Web.Mvc.SelectListItem>();
            foreach (OSBLE.Models.School s in db.Schools)
            {
                schoolNames.Add(new System.Web.Mvc.SelectListItem()
                {
                    Text = s.Name,
                    Value = s.ID.ToString()
                });
            }
            ViewBag.SchoolList = schoolNames;
            
            return View(new AssessmentCommittee());
        }

        [HttpPost]
        [CanCreateCourses]
        public ActionResult Create(AssessmentCommittee committee)
        {
            if (ModelState.IsValid)
            {
                db.Committees.Add(committee);
                db.SaveChanges();

                // Make current user a leader on new community.
                CourseUser cu = new CourseUser();
                cu.AbstractCourseID = committee.ID;
                cu.UserProfileID = CurrentUser.ID;
                cu.AbstractRoleID = AssessmentCommitteeChairRole.RoleID;

                db.CourseUsers.Add(cu);
                db.SaveChanges();

                Cache["ActiveCourse"] = committee.ID;

                return RedirectToAction("Index", "Home");
            }

            return View(committee);
        }
    }
}
