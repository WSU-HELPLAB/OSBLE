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
    public class PeerReviewController : Controller
    {
        private OSBLEContext db = new OSBLEContext();

        //
        // GET: /PeerReview/

        public ViewResult Index()
        {
            var assignmentactivities = db.AssignmentActivities.Include(p => p.AbstractAssignment);
            return View(assignmentactivities.ToList());
        }

        //
        // GET: /PeerReview/Details/5

        public ViewResult Details(int id)
        {
            PeerReviewActivity peerreviewactivity = db.AssignmentActivities.Find(id) as PeerReviewActivity;
            return View(peerreviewactivity);
        }

        //
        // GET: /PeerReview/Create

        public ActionResult Create()
        {
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name");
            return View();
        } 

        //
        // POST: /PeerReview/Create

        [HttpPost]
        public ActionResult Create(PeerReviewActivity peerreviewactivity)
        {
            if (ModelState.IsValid)
            {
                db.AssignmentActivities.Add(peerreviewactivity);
                db.SaveChanges();
                return RedirectToAction("Index");  
            }

            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", peerreviewactivity.AbstractAssignmentID);
            return View(peerreviewactivity);
        }
        
        //
        // GET: /PeerReview/Edit/5
 
        public ActionResult Edit(int id)
        {
            PeerReviewActivity peerreviewactivity = db.AssignmentActivities.Find(id) as PeerReviewActivity;
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", peerreviewactivity.AbstractAssignmentID);
            return View(peerreviewactivity);
        }

        //
        // POST: /PeerReview/Edit/5

        [HttpPost]
        public ActionResult Edit(PeerReviewActivity peerreviewactivity)
        {
            if (ModelState.IsValid)
            {
                db.Entry(peerreviewactivity).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.AbstractAssignmentID = new SelectList(db.AbstractAssignments, "ID", "Name", peerreviewactivity.AbstractAssignmentID);
            return View(peerreviewactivity);
        }

        //
        // GET: /PeerReview/Delete/5
 
        public ActionResult Delete(int id)
        {
            PeerReviewActivity peerreviewactivity = db.AssignmentActivities.Find(id) as PeerReviewActivity;
            return View(peerreviewactivity);
        }

        //
        // POST: /PeerReview/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {            
            PeerReviewActivity peerreviewactivity = db.AssignmentActivities.Find(id) as PeerReviewActivity;
            db.AssignmentActivities.Remove(peerreviewactivity);
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