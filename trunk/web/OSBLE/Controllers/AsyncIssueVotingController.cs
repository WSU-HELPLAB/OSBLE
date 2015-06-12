using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Controllers
{
    public class AsyncIssueVotingController : OSBLEController
    {
        //
        // GET: /AsyncIssueVoting/

        public ViewResult Index()
        {
            var assignmentactivities = db.AbstractAssignmentActivities.Include(a => a.AbstractAssignment);
            return View(assignmentactivities.ToList());
        }

        //
        // GET: /AsyncIssueVoting/Details/5

        public ViewResult Details(int id)
        {
            AsyncIssueVotingActivity asyncissuevotingactivity = db.AbstractAssignmentActivities.Find(id) as AsyncIssueVotingActivity;
            return View(asyncissuevotingactivity);
        }

        //
        // GET: /AsyncIssueVoting/Create

        public ActionResult Create()
        {
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name");
            return View();
        }

        //
        // POST: /AsyncIssueVoting/Create

        [HttpPost]
        public ActionResult Create(AsyncIssueVotingActivity asyncissuevotingactivity)
        {
            string setgrade = Request.Params["SetGrade"];

            // had to use hard coded strings because otherwise through an error about constant values.
            switch (setgrade)
            {
                case "PercentOfIssues":
                    asyncissuevotingactivity.Setgrade = IssueVotingActivity.SetGrade.PercentOfIssues;
                    break;
                case "PercentAgreementWModerator":
                    asyncissuevotingactivity.Setgrade = IssueVotingActivity.SetGrade.PercentAgreementWModerator;
                    break;
                case "Manually":
                    asyncissuevotingactivity.Setgrade = IssueVotingActivity.SetGrade.Manually;
                    break;
                default:
                    break;
            };

            if (ModelState.IsValid)
            {
                db.AbstractAssignmentActivities.Add(asyncissuevotingactivity);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", asyncissuevotingactivity.AbstractAssignmentID);
            return View(asyncissuevotingactivity);
        }

        //
        // GET: /AsyncIssueVoting/Edit/5

        public ActionResult Edit(int id)
        {
            AsyncIssueVotingActivity asyncissuevotingactivity = db.AbstractAssignmentActivities.Find(id) as AsyncIssueVotingActivity;
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", asyncissuevotingactivity.AbstractAssignmentID);
            return View(asyncissuevotingactivity);
        }

        //
        // POST: /AsyncIssueVoting/Edit/5

        [HttpPost]
        public ActionResult Edit(AsyncIssueVotingActivity asyncissuevotingactivity)
        {
            if (ModelState.IsValid)
            {
                db.Entry(asyncissuevotingactivity).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", asyncissuevotingactivity.AbstractAssignmentID);
            return View(asyncissuevotingactivity);
        }

        //
        // GET: /AsyncIssueVoting/Delete/5

        public ActionResult Delete(int id)
        {
            AsyncIssueVotingActivity asyncissuevotingactivity = db.AbstractAssignmentActivities.Find(id) as AsyncIssueVotingActivity;
            return View(asyncissuevotingactivity);
        }

        //
        // POST: /AsyncIssueVoting/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            AsyncIssueVotingActivity asyncissuevotingactivity = db.AbstractAssignmentActivities.Find(id) as AsyncIssueVotingActivity;
            db.AbstractAssignmentActivities.Remove(asyncissuevotingactivity);
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