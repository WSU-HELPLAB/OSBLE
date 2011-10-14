using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class CommentCategoryController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "CommentCategory"; }
        }

        public override string ControllerDescription
        {
            get { return "This assignment will employ a line-by-line review"; }
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

        public override ActionResult Index()
        {
            base.Index();

            return View();
        }

    }
}
