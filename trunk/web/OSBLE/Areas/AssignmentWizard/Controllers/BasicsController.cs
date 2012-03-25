using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Utility;
using System.Data.Entity.Validation;
using System.Diagnostics;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class BasicsController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "Basics"; }
        }

        public override string ControllerDescription 
        {
            get
            {
                return "Basic assignment information";
            }
        }

        public override ICollection<WizardBaseController> Prerequisites
        {
            get
            {
                List<WizardBaseController> prereqs = new List<WizardBaseController>();
                return prereqs;
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get 
            {
                return base.AllAssignmentTypes;
            }
        }

        public override bool IsRequired
        {
            get
            {
                return true;
            }
        }

        private void BuildViewBag()
        {
            IEnumerable<Category> categories = new List<Category>();
            //activeCourse should only be null when testing
            if (activeCourse != null)
            {
                //SUBMISSION CATEGORIES
                categories = from c in (activeCourse.AbstractCourse as Course).Categories
                              where c.Name != Constants.UnGradableCatagory
                              select c;
            }
            ViewBag.Categories = new SelectList(categories, "ID", "Name");
        }

        public override ActionResult Index()
        {
            base.Index();
            ModelState.Clear();
            BuildViewBag();
            Assignment.Type = manager.ActiveAssignmentType;

            Assignment.HoursPerDeduction = (ActiveCourse.AbstractCourse as Course).HoursLatePerPercentPenalty;
            Assignment.DeductionPerUnit = (ActiveCourse.AbstractCourse as Course).PercentPenalty;
            Assignment.HoursLateWindow = (ActiveCourse.AbstractCourse as Course).HoursLateUntilZero;

            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = model;
            if (ModelState.IsValid)
            {
                WasUpdateSuccessful = true;

                //update our DB
                if (Assignment.ID == 0)
                {
                    //calcuate column order for gradebook organization
                    int lastColumnNumber = (from assignments in db.Assignments
                                            orderby assignments.ColumnOrder descending
                                            select assignments.ColumnOrder).FirstOrDefault();
                    Assignment.ColumnOrder = (lastColumnNumber + 1);
                    db.Assignments.Add(Assignment);
                }
                else
                {

                    db.Entry(Assignment).State = System.Data.EntityState.Modified;
                }
                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            Trace.TraceInformation("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
            }
            else
            {
                WasUpdateSuccessful = false;
            }
            BuildViewBag();
            return base.PostBack(Assignment);
        }
    }
}
