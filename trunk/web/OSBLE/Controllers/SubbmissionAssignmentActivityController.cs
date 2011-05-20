using System.Data;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    public class SubbmissionAssignmentActivityController : OSBLEController
    {
        //
        // GET: /SubbmissionAssignmentActivity/

        public ViewResult Index()
        {
            return View(db.AssignmentActivities.ToList());
        }

        //
        // GET: /SubbmissionAssignmentActivity/Details/5

        public ViewResult Details(string id)
        {
            SubmissionActivitySetting submissionactivitysetting = db.AssignmentActivities.Find(id) as SubmissionActivitySetting;
            return View(submissionactivitysetting);
        }

        //
        // GET: /SubbmissionAssignmentActivity/Create

        public ActionResult Create()
        {
            return View();
        }

        //
        // POST: /SubbmissionAssignmentActivity/Create

        [HttpPost]
        public ActionResult Create(SubmissionActivitySetting submissionactivitysetting)
        {
            if (ModelState.IsValid)
            {
                db.AssignmentActivities.Add(submissionactivitysetting);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(submissionactivitysetting);
        }

        //
        // GET: /SubbmissionAssignmentActivity/Edit/5

        public ActionResult Edit(string id)
        {
            SubmissionActivitySetting submissionactivitysetting = db.AssignmentActivities.Find(id) as SubmissionActivitySetting;
            return View(submissionactivitysetting);
        }

        //
        // POST: /SubbmissionAssignmentActivity/Edit/5

        [HttpPost]
        public ActionResult Edit(SubmissionActivitySetting submissionactivitysetting)
        {
            if (ModelState.IsValid)
            {
                db.Entry(submissionactivitysetting).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(submissionactivitysetting);
        }

        //
        // GET: /SubbmissionAssignmentActivity/Delete/5

        public ActionResult Delete(string id)
        {
            SubmissionActivitySetting submissionactivitysetting = db.AssignmentActivities.Find(id) as SubmissionActivitySetting;
            return View(submissionactivitysetting);
        }

        //
        // POST: /SubbmissionAssignmentActivity/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(string id)
        {
            SubmissionActivitySetting submissionactivitysetting = db.AssignmentActivities.Find(id) as SubmissionActivitySetting;
            db.AssignmentActivities.Remove(submissionactivitysetting);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}