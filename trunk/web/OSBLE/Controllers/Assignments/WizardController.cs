﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Areas.AssignmentWizard.Controllers;

namespace OSBLE.Controllers.Assignments
{
    public class WizardController : OSBLEController
    {
        private WizardComponentManager manager;
        public static string selectedComponentsKey = "WizardSelectedComponentsKey";
        public static string beginWizardButton = "StartWizardButton";
        public static string previousWizardButton = "PreviousWizardButton";
        public static string nextWizardButton = "NextWizardButton";

        public Assignment ActiveAssignment
        {
            get
            {
                if (manager.ActiveAssignmentId != 0)
                {
                    return db.Assignments.Find(manager.ActiveAssignmentId);
                }
                else
                {
                    return new Assignment();
                }
            }
        }

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
            BuildViewBag();
        }

        private void BuildViewBag()
        {
            ViewBag.BeginWizardButton = beginWizardButton;
            ViewBag.PreviousWizardButton = previousWizardButton;
            ViewBag.NextWizardButton = nextWizardButton;
            ViewBag.Title = "Assignment Creation Wizard";
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
                    manager.ActiveAssignmentId = aid;
                }
                else
                {
                    manager.ActiveAssignmentId = 0;
                }
                return View(manager.AllComponents);
            }
            else
            {
                BuildViewBag();
                component.Controller.Assignment = ActiveAssignment;
                return component.Controller.Index();
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
        public ActionResult Index(dynamic model)
        {
            /*
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
                //The following throws an error when the SESSION state gets wiped
                try
                {
                    manager.ActiveComponent.Controller.Assignment = ActiveAssignment;
                    manager.ActiveComponent.Controller.Index(Request);
                }
                catch (Exception ex)
                {
                    //if we lost our session data, just return to the base wizard screen
                    return RedirectToRoute("AssignmentWizard", new { step = "Start" });
                }

                if (manager.ActiveComponent.Controller.WasUpdateSuccessful)
                {
                    //update the assignment ID.  Probably not necessary when working
                    //with existing assignments, but needed for new assignments.
                    manager.ActiveAssignmentId = manager.ActiveComponent.Controller.Assignment.ID;

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
                    //AC: Seems a bit ugly to call this twice.  Might want to rethink.
                    return manager.ActiveComponent.Controller.Index(Request);
                }
            }
             * */
            return View();
        }
    }
}