using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Controllers;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Utility;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class BasicsController : WizardBaseController
    {
        public override string PrettyName
        {
            get { return "Basic Settings"; }
        }

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

        public override WizardBaseController Prerequisite
        {
            get
            {
                //nothing comes before the Basics Controller
                return null;
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
            if (ActiveCourseUser != null)
            {
                //SUBMISSION CATEGORIES
                categories = from c in (ActiveCourseUser.AbstractCourse as Course).Categories
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

            Assignment.HoursPerDeduction = (ActiveCourseUser.AbstractCourse as Course).HoursLatePerPercentPenalty;
            Assignment.DeductionPerUnit = (ActiveCourseUser.AbstractCourse as Course).PercentPenalty;
            Assignment.HoursLateWindow = (ActiveCourseUser.AbstractCourse as Course).HoursLateUntilZero;

            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            
            Assignment = model;
            if (ModelState.IsValid)
            {
                WasUpdateSuccessful = true;

                Assignment.CourseID = ActiveCourseUser.AbstractCourseID;

                //update our DB
                if (Assignment.ID == 0)
                {
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