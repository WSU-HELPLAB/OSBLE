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
    public class AsyncIssueVotingController : OSBLEController
    {
        //
        // GET: /AsyncIssueVoting/

        public ViewResult Index()
        {
            var assignmentactivities = db.AssignmentActivities.Include(a => a.AbstractAssignment);
            return View(assignmentactivities.ToList());
        }

        //
        // GET: /AsyncIssueVoting/Details/5

        public ViewResult Details(int id)
        {
            AsyncIssueVotingActivity asyncissuevotingactivity = db.AssignmentActivities.Find(id) as AsyncIssueVotingActivity;
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
            switch(setgrade)
            {
                case "PercentOfIssues":
                    asyncissuevotingactivity.setgrade = IssueVotingActivity.SetGrade.PercentOfIssues;
                    break;
                case "PercentAgreementWModerator":
                    asyncissuevotingactivity.setgrade = IssueVotingActivity.SetGrade.PercentAgreementWModerator;
                    break;
                case "Manually":
                    asyncissuevotingactivity.setgrade = IssueVotingActivity.SetGrade.Manually;
                    break;
            };

            if (ModelState.IsValid)
            {
                db.AssignmentActivities.Add(asyncissuevotingactivity);
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
            AsyncIssueVotingActivity asyncissuevotingactivity = db.AssignmentActivities.Find(id) as AsyncIssueVotingActivity;
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
            AsyncIssueVotingActivity asyncissuevotingactivity = db.AssignmentActivities.Find(id) as AsyncIssueVotingActivity;
            return View(asyncissuevotingactivity);
        }

        //
        // POST: /AsyncIssueVoting/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            AsyncIssueVotingActivity asyncissuevotingactivity = db.AssignmentActivities.Find(id) as AsyncIssueVotingActivity;
            db.AssignmentActivities.Remove(asyncissuevotingactivity);
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