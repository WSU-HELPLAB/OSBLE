﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Controllers;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Models.Assignments;

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

        #region action results

        public ActionResult Index(int? assignmentId)
        {
            //if our assignmentId isn't null, then we need to pull that assignment
            //from the DB
            if (assignmentId != null)
            {
                int aid = (int)assignmentId;
                manager.ActiveAssignmentId = aid;
                ActivateAssignmentComponents(db.Assignments.Find(aid));
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
            return RedirectToRoute(AssignmentWizardAreaRegistration.AssignmentWizardRoute, new { controller = manager.ActiveComponent.Name });
        }

        public ActionResult Summary(int? assignmentId)
        {
            Assignment assignment = new Assignment();
            if (assignmentId != null)
            {
                assignment = db.Assignments.Find((int)assignmentId);
            }
            else
            {
                assignment = db.Assignments.Find(manager.ActiveAssignmentId);
            }
            return View(assignment);
        }

        #endregion

        #region helper methods
        private void ActivateAssignmentComponents(Assignment assignment)
        {
            manager.DeactivateAllComponents();
            if (assignment.RubricID != null)
            {
                manager.GetComponentByType(typeof(RubricController)).IsSelected = true;
            }
        }

        private void ActivateSelectedComponents()
        {
            manager.DeactivateAllComponents();
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
        #endregion
    }
}
