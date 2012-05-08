using System.Data;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Courses;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
    public class CommunityController : OSBLEController
    {
        //
        // GET: /Community/

        public ActionResult Index()
        {
            return RedirectToAction("Edit");
        }

        //
        // GET: /Community/Create

        [CanCreateCourses]
        public ActionResult Create()
        {
            return View(new Community());
        }

        //
        // POST: /Community/Create

        [HttpPost]
        [CanCreateCourses]
        public ActionResult Create(Community community)
        {
            if (ModelState.IsValid)
            {
                db.Communities.Add(community);
                db.SaveChanges();

                // Make current user a leader on new community.
                CourseUser cu = new CourseUser();
                cu.AbstractCourseID = community.ID;
                cu.UserProfileID = CurrentUser.ID;
                cu.AbstractRoleID = (int)CommunityRole.OSBLERoles.Leader;

                db.CourseUsers.Add(cu);
                db.SaveChanges();

                Session["ActiveCourse"] = community.ID;

                return RedirectToAction("Index");
            }

            return View(community);
        }

        //
        // GET: /Community/Edit/5

        [RequireActiveCourse]
        [CanModifyCourse]
        [IsCommunity]
        public ActionResult Edit()
        {
            ViewBag.CurrentTab = "Community Settings";
            Community community = db.Communities.Find(activeCourse.AbstractCourseID);
            return View(community);
        }

        //
        // POST: /Community/Edit/5

        [HttpPost]
        [RequireActiveCourse]
        [CanModifyCourse]
        [IsCommunity]
        public ActionResult Edit(Community community)
        {
            ViewBag.CurrentTab = "Community Settings";

            if (community.ID != activeCourse.AbstractCourseID)
            {
                return RedirectToAction("Index");
            }

            Community updateCommunity = (Community)activeCourse.AbstractCourse;

            updateCommunity.AllowEventPosting = community.AllowEventPosting;
            updateCommunity.CalendarWindowOfTime = community.CalendarWindowOfTime;
            updateCommunity.Name = community.Name;
            updateCommunity.Nickname = community.Nickname;
            updateCommunity.Description = community.Description;

            if (ModelState.IsValid)
            {
                db.Entry(updateCommunity).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(community);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}