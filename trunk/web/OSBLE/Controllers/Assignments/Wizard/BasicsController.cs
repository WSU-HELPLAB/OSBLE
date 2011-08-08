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

        public override string ControllerName
        {
            get { return "Basics"; }
        }

        public override string ControllerDescription 
        {
            get
            {
                return "Basic Assignment Information";
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

        protected override object IndexAction(int assignmentId = 0)
        {
            //SUBMISSION CATEGORIES
            var cat = from c in (activeCourse.AbstractCourse as Course).Categories
                      where c.Name != Constants.UnGradableCatagory
                      select c;
            ViewBag.Categories = new SelectList(cat, "ID", "Name");

            if (assignmentId != 0)
            {
                return db.StudioAssignments.Find(assignmentId);
            }
            else
            {
                return new StudioAssignment();
            }

        }

        protected override object IndexActionPostback(object model)
        {
            return new object();
        }
    }
}
