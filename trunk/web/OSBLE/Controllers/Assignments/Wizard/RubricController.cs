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
            get { return "Grading Rubric"; }
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

        protected override object IndexAction(int assignmentId = 0)
        {
            throw new NotImplementedException();
        }

        protected override object IndexActionPostback(object model)
        {
            throw new NotImplementedException();
        }
    }
}
