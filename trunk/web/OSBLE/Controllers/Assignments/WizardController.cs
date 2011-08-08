using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Controllers.Assignments.Wizard;

namespace OSBLE.Controllers.Assignments
{
    public class WizardController : OSBLEController
    {
        private WizardComponentManager manager;
        private ICollection<WizardBaseController> selectedComponents;
        private const string selectedComponentsKey = "SelectedComponentsKey";

        public WizardController()
        {
            manager = WizardComponentManager.GetInstance();
            if (Session[selectedComponentsKey] != null)
            {
                selectedComponents = Session[selectedComponentsKey] as ICollection<WizardBaseController>;
            }
            else
            {
                selectedComponents = new List<WizardBaseController>();
            }
        }

        public ActionResult Index(string step, int? assignmentId)
        {
            //display start page if we're not on any particular step or if the step provided is
            //invalid
            WizardBaseController component = (from c in manager.Components
                                              where c.ControllerName.CompareTo(step) == 0
                                              select c).FirstOrDefault();
            if (component == null)
            {
                return View(manager.Components);
            }
            else
            {
                return component.Index();
            }
        }

    }
}
