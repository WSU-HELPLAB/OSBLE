using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    public class SyncIssueVotingController : OSBLEController
    {
        //
        // GET: /IssueVoting/

        public ViewResult Index()
        {
            var assignmentactivities = db.AssignmentActivities.Include(i => i.AbstractAssignment);
            return View(assignmentactivities.ToList());
        }

        //
        // GET: /IssueVoting/Details/5

        public ViewResult Details(int id)
        {
            SyncIssueVotingActivity syncissuevotingactivity = db.AssignmentActivities.Find(id) as SyncIssueVotingActivity;
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
            string setgrade = Request.Params["SetGrade"];

            // the peer reveiw activity wasnt getting the right ID so we assigned it here
            syncissuevotingactivity.peerReviewActivity.AbstractAssignmentID = syncissuevotingactivity.AbstractAssignmentID;

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

            foreach (var modelStateValue in ViewData.ModelState.Values)
            {
                foreach (var error in modelStateValue.Errors)
                {
                    // Do something useful with these properties
                    var errorMessage = error.ErrorMessage;
                    var exception = error.Exception;
                }
            }


            if (ModelState.IsValid)
            {
                db.AssignmentActivities.Add(syncissuevotingactivity);
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
            SyncIssueVotingActivity syncissuevotingactivity = db.AssignmentActivities.Find(id) as SyncIssueVotingActivity;
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
            SyncIssueVotingActivity syncissuevotingactivity = db.AssignmentActivities.Find(id) as SyncIssueVotingActivity;
            return View(syncissuevotingactivity);
        }

        //
        // POST: /IssueVoting/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            SyncIssueVotingActivity syncissuevotingactivity = db.AssignmentActivities.Find(id) as SyncIssueVotingActivity;
            db.AssignmentActivities.Remove(syncissuevotingactivity);
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
