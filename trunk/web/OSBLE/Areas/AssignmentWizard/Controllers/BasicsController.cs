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

        public override ActionResult Index()
        {
            base.Index();
            ModelState.Clear();
            Assignment.Type = manager.ActiveAssignmentType;

            //If the assignment is new and has default zero values, overwrite them with Course default late penalty values
            if (Assignment.HoursLateWindow == 0 && Assignment.DeductionPerUnit == 0 && Assignment.HoursPerDeduction == 0)
            {
                Assignment.HoursPerDeduction = (ActiveCourseUser.AbstractCourse as Course).HoursLatePerPercentPenalty;
                Assignment.DeductionPerUnit = (ActiveCourseUser.AbstractCourse as Course).PercentPenalty;
                Assignment.HoursLateWindow = (ActiveCourseUser.AbstractCourse as Course).HoursLateUntilZero;
            }
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
                else //editing preexisting assingment
                {
                    if (Assignment.AssociatedEventID.HasValue)
                    {
                        //If the assignment is being edited, update it's associated event.
                        OSBLE.Models.HomePage.Event assignmentsEvent = db.Events.Find(Assignment.AssociatedEventID);
                        if (assignmentsEvent != null)
                        {
                            assignmentsEvent.Description = Assignment.AssignmentDescription;
                            assignmentsEvent.EndDate = Assignment.DueDate;
                            assignmentsEvent.EndTime = Assignment.DueTime;
                            assignmentsEvent.StartDate = Assignment.ReleaseDate;
                            assignmentsEvent.StartTime = Assignment.ReleaseTime;
                            assignmentsEvent.Title = Assignment.AssignmentName;
                            db.Entry(assignmentsEvent).State = System.Data.EntityState.Modified;
                        }
                    }
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
            return base.PostBack(Assignment);
        }
    }
}