using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Controllers;
using OSBLE.Areas.AssignmentWizard.Models;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class HomeController : OSBLEController
    {
        private WizardComponentManager manager;
        public static string beginWizardButton = "StartWizardButton";

        public HomeController()
        {
            manager = WizardComponentManager.GetInstance();
        }

        public ActionResult Index(int? assignmentId)
        {
            //if our assignmentId isn't null, then we need to pull that assignment
            //from the DB
            if (assignmentId != null)
            {
                int aid = (int)assignmentId;
                manager.ActiveAssignmentId = aid;
            }
            else
            {
                manager.ActiveAssignmentId = 0;
            }
            ViewBag.BeginWizardButton = beginWizardButton;
            return View(manager.AllComponents);
        }

        [HttpPost]
        public ActionResult Index(ICollection<WizardComponent> components)
        {
            ActivateSelectedComponents();
            return RedirectToRoute("AssignmentWizard_default", new { controller = manager.ActiveComponent.Name });
        }

        private void ActivateSelectedComponents()
        {
            string componentPrefix = "component_";
            foreach (string key in Request.Form.AllKeys)
            {
                if (key.Substring(0, componentPrefix.Length) == componentPrefix)
                {
                    WizardComponent comp = manager.GetComponentByName(Request.Form[key]);
                    comp.IsSelected = true;
                    manager.SelectedComponents.Add(comp);
                }
            }
        }
    }
}
