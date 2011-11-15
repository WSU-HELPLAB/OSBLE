﻿using System;
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
        public static string selectedComponentsKey = "WizardSelectedComponentsKey";
        public static string beginWizardButton = "StartWizardButton";
        public static string previousWizardButton = "PreviousWizardButton";
        public static string nextWizardButton = "NextWizardButton";

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
                return View(manager.AllComponents);
            }
            else
            {
                BuildViewBag();
                component.Controller.Assignment = db.StudioAssignments.Find(manager.ActiveAssignmentId);
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
                manager.ActiveComponent.Controller.Assignment = db.StudioAssignments.Find(manager.ActiveAssignmentId);
                manager.ActiveComponent.Controller.Index(Request);
                
                //Not having an ID of 0 indicates that the record previously existed in the DB
                if (manager.ActiveAssignmentId != 0)
                {
                    //Saving goes here (maybe)
                    //db.Entry(manager.ActiveAssignment).State = System.Data.EntityState.Modified;
                    //db.SaveChanges();
                }
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
        }
    }
}
