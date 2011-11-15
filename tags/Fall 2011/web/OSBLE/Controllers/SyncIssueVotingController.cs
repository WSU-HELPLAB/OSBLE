using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Controllers
{
    public class SyncIssueVotingController : OSBLEController
    {
        //
        // GET: /IssueVoting/

        public ViewResult Index()
        {
            var assignmentactivities = db.AbstractAssignmentActivities.Include(i => i.AbstractAssignment);
            return View(assignmentactivities.ToList());
        }

        //
        // GET: /IssueVoting/Details/5

        public ViewResult Details(int id)
        {
            SyncIssueVotingActivity syncissuevotingactivity = db.AbstractAssignmentActivities.Find(id) as SyncIssueVotingActivity;
            return View(syncissuevotingactivity);
        }

        //
        // GET: /IssueVoting/Create

        public ActionResult Create()
        {
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name");
            return View();
        }

        //
        // POST: /IssueVoting/Create

        [HttpPost]
        public ActionResult Create(SyncIssueVotingActivity syncissuevotingactivity)
        {
            if (ModelState.IsValid)
            {
                string setgrade = Request.Params["SetGrade"];

                // had to use hard coded strings because otherwise through an error about constant values.
                switch (setgrade)
                {
                    case "PercentOfIssues":
                        syncissuevotingactivity.Setgrade = SyncIssueVotingActivity.SetGrade.PercentOfIssues;
                        break;
                    case "PercentAgreementWModerator":
                        syncissuevotingactivity.Setgrade = SyncIssueVotingActivity.SetGrade.PercentAgreementWModerator;
                        break;
                    case "Manually":
                        syncissuevotingactivity.Setgrade = SyncIssueVotingActivity.SetGrade.Manually;
                        break;
                    default:
                        break;
                };

                db.AbstractAssignmentActivities.Add(syncissuevotingactivity);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", syncissuevotingactivity.AbstractAssignmentID);
            return View(syncissuevotingactivity);
        }

        //
        // GET: /IssueVoting/Edit/5

        public ActionResult Edit(int id)
        {
            SyncIssueVotingActivity syncissuevotingactivity = db.AbstractAssignmentActivities.Find(id) as SyncIssueVotingActivity;
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", syncissuevotingactivity.AbstractAssignmentID);
            return View(syncissuevotingactivity);
        }

        //
        // POST: /IssueVoting/Edit/5

        [HttpPost]
        public ActionResult Edit(SyncIssueVotingActivity syncissuevotingactivity)
        {
            if (ModelState.IsValid)
            {
                db.Entry(syncissuevotingactivity).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", syncissuevotingactivity.AbstractAssignmentID);
            return View(syncissuevotingactivity);
        }

        //
        // GET: /IssueVoting/Delete/5

        public ActionResult Delete(int id)
        {
            SyncIssueVotingActivity syncissuevotingactivity = db.AbstractAssignmentActivities.Find(id) as SyncIssueVotingActivity;
            return View(syncissuevotingactivity);
        }

        //
        // POST: /IssueVoting/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            SyncIssueVotingActivity syncissuevotingactivity = db.AbstractAssignmentActivities.Find(id) as SyncIssueVotingActivity;
            db.AbstractAssignmentActivities.Remove(syncissuevotingactivity);
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