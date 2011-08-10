using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Controllers.Assignments.Wizard;
using OSBLE.Models.Wizard;
using OSBLE.Models.Assignments;

namespace OSBLE.Controllers.Assignments
{
    public class WizardController : OSBLEController
    {
        private WizardComponentManager manager;
        private const string selectedComponentsKey = "WizardSelectedComponentsKey";
        private const string beginWizardButton = "StartWizardButton";
        private const string previousWizardButton = "PreviousWizardButton";
        private const string nextWizardButton = "NextWizardButton";

        public ICollection<WizardBaseController> SelectedComponents
        {
            get
            {
                if (context.Session[selectedComponentsKey] != null)
                {
                    return context.Session[selectedComponentsKey] as ICollection<WizardBaseController>;
                }
                else
                {
                    return new List<WizardBaseController>();
                }
            }
            set
            {
                context.Session[selectedComponentsKey] = value; 
            }
        }

        public WizardController()
        {
            manager = WizardComponentManager.GetInstance();
            ViewBag.BeginWizardButton = beginWizardButton;
            ViewBag.PreviousWizardButton = previousWizardButton;
            ViewBag.NextWizardButton = nextWizardButton;
        }

        public ActionResult Index(string step, int? assignmentId)
        {
            //display start page if we're not on any particular step or if the step provided is
            //invalid
            WizardComponent component = manager.GetComponentByName(step);
            if (component == null)
            {
                //if our assignmentId isn't null, then we need to pull that assignment
                //from the DB
                if (assignmentId != null)
                {
                    int aid = (int)assignmentId;
                    manager.ActiveAssignment = (from assign in db.StudioAssignments
                                                where assign.ID == aid
                                                select assign).FirstOrDefault();
                    db.Entry(manager.ActiveAssignment).State = System.Data.EntityState.Detached;
                }
                return View(manager.AllComponents);
            }
            else
            {
                component.Controller.Assignment = manager.ActiveAssignment;
                ActionResult result = component.Controller.Index();
                ViewBag.Content = result.Capture(ControllerContext);
                return View("Wizard");
            }
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

        [HttpPost]
        public ActionResult Index(object model)
        {
            //Three possibilities:
            // 1: The user clicked the "Begin" button at which we must now figure out what 
            //    our assignment will contain and begin our process.
            // 2: The user clicked the "Previous" button on an active wizard page.  When
            //    this happens, we need to go to the previous page in our wizard.
            // 3: The user clicked on "Next" button on an active wizard page.  Go to the
            //    next page.
            if (Request.Form.AllKeys.Contains(beginWizardButton))
            {
                ActivateSelectedComponents();
                return RedirectToRoute("AssignmentWizard", new { step = manager.ActiveComponent.Name });
            }
            else
            {
                manager.ActiveComponent.Controller.Index(Request);
                manager.ActiveAssignment = manager.ActiveComponent.Controller.Assignment;
                if (manager.ActiveComponent.Controller.WasUpdateSuccessful)
                {
                    string errorPath = "";
                    WizardComponent comp = null;
                    if(Request.Form.AllKeys.Contains(previousWizardButton))
                    {
                        comp = manager.GetPreviousComponent();
                        errorPath = "Start";
                    }
                    else
                    {
                        comp = manager.GetNextComponent();
                        errorPath = "WizardSummary";
                    }

                    if (comp != null)
                    {
                        return RedirectToRoute("AssignmentWizard", new { step = manager.ActiveComponent.Name });
                    }
                    else
                    {
                        return RedirectToRoute("AssignmentWizard", new { step = errorPath });
                    }
                }
                else
                {
                    return RedirectToRoute("AssignmentWizard", new { step = manager.ActiveComponent.Name });
                }
            }

            return View();
        }
    }
}
