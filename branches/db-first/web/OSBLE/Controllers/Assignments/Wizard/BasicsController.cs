using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Utility;

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
            }
            else
            {
                WasUpdateSuccessful = false;
            }
            return new object();
        }

        private void UpdateAssignmentWithFormValues()
        {
            Assignment.Name = webRequest.Form["Name"];
            Assignment.Description = webRequest.Form["Description"];
            try
            {
                Assignment.PointsPossible = Convert.ToInt32(webRequest.Form["PointsPossible"]);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("pts", "The number of points possible must be a whole number.");
            }
            try
            {
                Assignment.CategoryID = Convert.ToInt32(webRequest.Form["CategoryID"]);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("pts", "Please select a valid category.");
            }
        }
    }
}
