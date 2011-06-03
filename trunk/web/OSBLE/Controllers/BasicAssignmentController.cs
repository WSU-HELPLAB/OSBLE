using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.ViewModels;
using OSBLE.Models.Courses;
using OSBLE.Models.Assignments.Activities;


namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class BasicAssignmentController : OSBLEController
    {
        public BasicAssignmentController()
            : base()
        {
            ViewBag.CurrentTab = "Assignments";
        }

        //
        // GET: /Assignment/
        
        /*
        public ViewResult Index()
        {
            //This is what it was but no longer works
            var abstractgradables = db.AbstractGradables.ToList();
            return View(abstractgradables);
            //return View();
        }*/

        //
        // GET: /Assignment/Details/5

        /*
        public ViewResult Details(int id)
        {
            
            SubmissionActivity assignment = db.AbstractGradables.Find(id) as SubmissionActivity;
            return View(assignment);
            
        }
        */
        //
        // GET: /Assignment/Create

        [CanModifyCourse]
        public ActionResult Create()
        {
            //we create a basic assignment that is a StudioAssignment with a submission and a stop
            List<Deliverable> Deliverables = new List<Deliverable>();
            BasicAssignmentViewModel basic = new BasicAssignmentViewModel();

            // Copy default Late Policy settings
            Course active = activeCourse.Course as Course;
            basic.BasicAssignment.HoursLatePerPercentPenalty = active.HoursLatePerPercentPenalty;
            basic.BasicAssignment.HoursLateUntilZero = active.HoursLateUntilZero;
            basic.BasicAssignment.PercentPenalty = active.PercentPenalty;
            basic.BasicAssignment.MinutesLateWithNoPenalty = active.MinutesLateWithNoPenalty;

            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes());
            ViewBag.Categories = new SelectList(db.Categories, "ID", "Name");
            ViewBag.Deliverables = Deliverables;
            return View(basic);
        }

        //
        // POST: /Assignment/Create

        [HttpPost]
        public ActionResult Create(BasicAssignmentViewModel basic)
        {
            if (basic.Submission.ReleaseDate >= basic.Stop.ReleaseDate)
            {
                ModelState.AddModelError("time", "The due date must come after the release date");
            }
            if (ModelState.IsValid)
            {
                db.BasicAssignments.Add(basic.BasicAssignment);
                db.SaveChanges();

                basic.BasicAssignment.AssignmentActivities.Add(basic.Submission);
                basic.BasicAssignment.AssignmentActivities.Add(basic.Stop);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(db.Categories, "ID", "Name", basic.BasicAssignment.CategoryID);
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes());
            ViewBag.Deliverable = new SelectList(db.Deliverables, "ID", "Name");
            return View(basic);
        }

        //
        // GET: /Assignment/Edit/5

        /*
        public ActionResult Edit(int id)
        {
            SubmissionActivity assignment = db.Categories.Find(id) as SubmissionActivity;
            ViewBag.WeightID = new SelectList(db.Categories, "ID", "Name", assignment.WeightID);
            return View(assignment);
        }
        */

        //
        // POST: /Assignment/Edit/5

        /*
        [HttpPost]
        public ActionResult Edit(SubmissionActivity assignment)
        {
            if (ModelState.IsValid)
            {
                db.Entry(assignment).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.WeightID = new SelectList(db.Categories, "ID", "Name", assignment.CategoryID);
            return View(assignment);
        }
        */

        //
        // GET: /Assignment/Delete/5

        /*
        public ActionResult Delete(int id)
        {
            SubmissionActivity assignment = db.AbstractGradables.Find(id) as SubmissionActivity;
            return View(assignment);
        }
        */

        //
        // POST: /Assignment/Delete/5

        /*
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            SubmissionActivity assignment = db.AbstractGradables.Find(id) as SubmissionActivity;
            db.AbstractGradables.Remove(assignment);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
         */

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        private List<string> GetListOfDeliverableTypes()
        {
            List<string> fileTypes = new List<string>();
            int i = 0;
            DeliverableType deliverable = (DeliverableType)i;
            while (Enum.IsDefined(typeof(DeliverableType), i))
            {
                Type type = deliverable.GetType();

                FieldInfo fi = type.GetField(deliverable.ToString());

                //we get the attributes of the selected language
                FileExtensions[] attrs = (fi.GetCustomAttributes(typeof(FileExtensions), false) as FileExtensions[]);

                //make sure we have more than (should be exactly 1)
                if (attrs.Length > 0 && attrs[0] is FileExtensions)
                {
                    //we get the first attributes value which should be the fileExtension
                    string s = deliverable.ToString();
                    s += " (";
                    s += string.Join(", ", attrs[0].Extensions);
                    s += ")";
                    fileTypes.Add(s);
                }
                else
                {
                    //throw and exception if not decorated with any attrs because it is a requirement
                    throw new Exception("Languages must have be decorated with a FileExtensionAttribute");
                }

                i++;
                deliverable = (DeliverableType)i;
            }

            return fileTypes;
        }
    }
}
