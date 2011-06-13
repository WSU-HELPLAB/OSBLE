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
    public class IssueVotingController : Controller
    {
        private OSBLEContext db = new OSBLEContext();

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
            IssueVotingActivity issuevotingactivity = db.AssignmentActivities.Find(id) as IssueVotingActivity;
            return View(issuevotingactivity);
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
        public ActionResult Create(IssueVotingActivity issuevotingactivity)
        {
            if (ModelState.IsValid)
            {
                db.AssignmentActivities.Add(issuevotingactivity);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", issuevotingactivity.AbstractAssignmentID);
            return View(issuevotingactivity);
        }
        
        //
        // GET: /IssueVoting/Edit/5
 
        public ActionResult Edit(int id)
        {
            IssueVotingActivity issuevotingactivity = db.AssignmentActivities.Find(id) as IssueVotingActivity;
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", issuevotingactivity.AbstractAssignmentID);
            return View(issuevotingactivity);
        }

        //
        // POST: /IssueVoting/Edit/5

        [HttpPost]
        public ActionResult Edit(IssueVotingActivity issuevotingactivity)
        {
            if (ModelState.IsValid)
            {
                db.Entry(issuevotingactivity).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", issuevotingactivity.AbstractAssignmentID);
            return View(issuevotingactivity);
        }

        //
        // GET: /IssueVoting/Delete/5
 
        public ActionResult Delete(int id)
        {
            IssueVotingActivity issuevotingactivity = db.AssignmentActivities.Find(id) as IssueVotingActivity;
            return View(issuevotingactivity);
        }

        //
        // POST: /IssueVoting/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {            
            IssueVotingActivity issuevotingactivity = db.AssignmentActivities.Find(id) as IssueVotingActivity;
            db.AssignmentActivities.Remove(issuevotingactivity);
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