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

namespace OSBLE.Controllers.Assignments.Wizard
{
    public class BasicsController : WizardBaseController
    {
        private HttpRequestBase webRequest;

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
                //the basics page has no prereqs
                return new List<WizardBaseController>();
            }
        }

        protected override object IndexAction()
        {
            ModelState.Clear();

            //SUBMISSION CATEGORIES
            var cat = from c in (activeCourse.AbstractCourse as Course).Categories
                      where c.Name != Constants.UnGradableCatagory
                      select c;
            ViewBag.Categories = new SelectList(cat, "ID", "Name");
            return Assignment;
        }

        protected override object IndexActionPostback(HttpRequestBase request)
        {
            this.webRequest = request;
            UpdateAssignmentWithFormValues();
            if (ModelState.IsValid)
            {
                WasUpdateSuccessful = true;

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
            return new object();
        }

        private void UpdateAssignmentWithFormValues()
        {
            //probably the worst part of this assignment wizard idea is the fact that
            //you have to manually populate model data
            Assignment.AssignmentName = webRequest.Form["AssignmentName"];
            Assignment.AssignmentDescription = webRequest.Form["AssignmentDescription"];
            try
            {
                Assignment.PointsPossible = Convert.ToInt32(webRequest.Form["PointsPossible"]);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("pts", "The number of points possible must be a whole number");
            }
            try
            {
                Assignment.CategoryID = Convert.ToInt32(webRequest.Form["CategoryID"]);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("category", "Please select a valid category");
            }
            try
            {
                Assignment.ReleaseDate = DateTime.Parse(webRequest.Form["ReleaseDate"]);
                Assignment.ReleaseTime = DateTime.Parse(webRequest.Form["ReleaseTime"]);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("releaseDate", "Please provide a valid release date");
            }
            try
            {
                Assignment.DueDate = DateTime.Parse(webRequest.Form["DueDate"]);
                Assignment.DueTime = DateTime.Parse(webRequest.Form["DueTime"]);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("DueDate", "Please provide a valid due date");
            }

            //make sure that the due date comes after the release date
            if (Assignment.DueDate < Assignment.ReleaseDate)
            {
                ModelState.AddModelError("badDueDate", "The due date must come after the relase date");
            }
            try
            {
                Assignment.HoursLateWindow = Convert.ToInt32(webRequest.Form["HoursLateWindow"]);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("HoursLateWindow", "The late assignment window must be a whole number");
            }
            try
            {
                Assignment.DeductionPerHourLate = Convert.ToDouble(webRequest.Form["DeductionPerHourLate"]);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("HoursLateWindow", "Please specify a late penalty.  If none exists, please enter a value of &quot;0&quot;");
            }
            try
            {
                Assignment.IsDraft = Convert.ToBoolean(webRequest.Form["IsDraft"]);
            }
            catch (Exception ex)
            {
                Assignment.IsDraft = true;
            }
        }
    }
}
