using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Reflection;
using OSBLE.Models.Assignments;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Remoting;
using OSBLE.Controllers;
using OSBLE.Areas.AssignmentWizard.Models;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public abstract class WizardBaseController : OSBLEController, IComparable
    {
        public static string previousWizardButton = "PreviousWizardButton";
        public static string nextWizardButton = "NextWizardButton";
        private WizardComponentManager manager;

        /// <summary>
        /// Returns the controller's name.  Must be unique.  The GET method should
        /// return a static string.
        /// </summary>
        public abstract string ControllerName { get; }

        /// <summary>
        /// Returns a brief description of the controller's purpose.  Used mainly on the "Start"
        /// page
        /// </summary>
        public abstract string ControllerDescription { get; }

        /// <summary>
        /// Returns a list of WizardBaseControllers that must preceed the current controller.  
        /// For example, most controllers will expect an assignment to have at least a name.  The
        /// "Basics" controller handles setting up assignment basics and so other controllers should
        /// list "Basics" as being a prerequisite.
        /// </summary>
        public abstract ICollection<WizardBaseController> Prerequisites { get; }

        public Assignment Assignment { get; set; }

        public bool WasUpdateSuccessful { get; set; }

        public WizardBaseController()
        {
            WasUpdateSuccessful = true;
        }

        public void SetUpViewBag()
        {
            ViewBag.Components = manager.SelectedComponents;
            ViewBag.ActiveComponent = manager.ActiveComponent;
            ViewBag.Assignment = Assignment;
            ViewBag.PreviousWizardButton = WizardBaseController.previousWizardButton;
            ViewBag.NextWizardButton = WizardBaseController.nextWizardButton;
            ViewBag.Title = "Assignment Creation Wizard";
        }

        public virtual ActionResult Index()
        {
            Assignment = new Assignment();
            manager = WizardComponentManager.GetInstance();
            if (manager.ActiveAssignmentId != 0)
            {
                Assignment = db.Assignments.Find(manager.ActiveAssignmentId);
            }
            else
            {
                Assignment = new Assignment();
            }
            SetUpViewBag();
            return View();
        }

        [HttpPost]
        protected ActionResult Index(dynamic model)
        {
            manager = WizardComponentManager.GetInstance();
            if (WasUpdateSuccessful)
            {
                //update the assignment ID.  Probably not necessary when working
                //with existing assignments, but needed for new assignments.
                manager.ActiveAssignmentId = Assignment.ID;

                //Check manager context.  If for some reason the user lost their
                //SESSION context, we need to detect this and redirect them
                //to a safe place.  Otherwise, they will get an error when we
                //try to redirect them to a non-existant page
                if (manager.SelectedComponents.Count == 0)
                {
                    return RedirectToRoute(new { controller = "Home", action = "ContextLost", assignmentId = Assignment.ID });
                }

                string errorPath = "";
                string action = "";
                int id = Assignment.ID;
                WizardComponent comp = null;
                if (Request.Form.AllKeys.Contains(previousWizardButton))
                {
                    comp = manager.GetPreviousComponent();
                    errorPath = "Home";
                    action = "Index";
                }
                else
                {
                    comp = manager.GetNextComponent();
                    errorPath = "Home";
                    action = "Summary";
                }

                if (comp != null)
                {
                    return RedirectToRoute(AssignmentWizardAreaRegistration.AssignmentWizardRoute, 
                                          new { 
                                              controller = manager.ActiveComponent.Name 
                                              }
                                          );
                }
                else
                {
                    return RedirectToRoute(AssignmentWizardAreaRegistration.AssignmentWizardRoute, 
                                          new { 
                                              controller = errorPath,
                                              action = action,
                                              assignmentId = id
                                              }
                                          );
                }
            }
            SetUpViewBag();
            return View(model);
        }

        [HttpPost]
        public ActionResult QuickNav()
        {
            string componentName = Request.Form["ComponentName"];
            manager = WizardComponentManager.GetInstance();
            WizardComponent componentToFind = manager.GetComponentByName(componentName);

            //start at the beginning
            while (manager.GetPreviousComponent() != null) ;

            //from the beginning, find the component that we want to jump to
            bool found = false;
            while (!found)
            {
                if (manager.ActiveComponent == null)
                {
                    found = true;
                }
                else if (manager.ActiveComponent.Name.CompareTo(componentToFind.Name) == 0)
                {
                    found = true;
                }
                else
                {
                    manager.GetNextComponent();
                }
            }

            //redirect to the component
            return RedirectToRoute(AssignmentWizardAreaRegistration.AssignmentWizardRoute,
                                    new
                                    {
                                        controller = componentToFind.Name,
                                        action = "Index"
                                    }
                                  );
        }

        public int CompareTo(object obj)
        {
            return this.ToString().CompareTo(obj.ToString());   
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.ControllerName, this.ControllerDescription);
        }
    }
}
