using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Controllers.Assignments.Wizard
{
    public class TeamController : WizardBaseController
    {

        public override string ControllerName
        {
            get { return "Team"; }
        }

        public override string ControllerDescription
        {
            get { return "The assignment is team-based"; }
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
            return Assignment;
        }

        protected override object IndexActionPostback(HttpRequestBase request)
        {
            return new object();
        }
    }
}
