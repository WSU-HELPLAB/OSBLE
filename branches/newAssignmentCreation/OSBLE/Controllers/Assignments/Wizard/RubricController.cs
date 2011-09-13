using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Controllers.Assignments.Wizard
{
    public class RubricController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "Rubric";  }
        }

        public override string ControllerDescription
        {
            get { return "The instructor will use a grading rubric"; }
        }

        public override ICollection<WizardBaseController> Prerequisites
        {
            get
            {
                List<WizardBaseController> prereqs = new List<WizardBaseController>();
                prereqs.Add(new BasicsController());
                return prereqs;
            }
        }

        protected override object IndexAction()
        {
            return new object();
        }

        protected override object IndexActionPostback()
        {
            return new object();
        }
    }
}
