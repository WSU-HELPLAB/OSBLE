using System;
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
            ViewBag.AssignmentTypeRadioName = "AssignmentType";
            manager = WizardComponentManager.GetInstance();
        }

        #region action results

        public ActionResult Index(int? assignmentId)
        {
            //existing assignments skip this step
            if (assignmentId != null)
            {
                Assignment assignment = db.Assignments.Find(assignmentId);
                
                //prime the manager for the new assignment
                manager.ActiveAssignmentId = assignment.ID;
                manager.SetActiveAssignmentType((AssignmentTypes)assignment.AssignmentTypeID);

                //load in any secondary (non-required) components
                ActivateAssignmentComponents(assignment);

                //now, load in essential components
                List<WizardBaseController> components = (from comp in manager.GetComponentsForAssignmentType(assignment.Type)
                                                        where comp.IsRequired == true
                                                        select comp).ToList();
                foreach (WizardBaseController component in components)
                {
                    component.IsSelected = true;
                }

                //finally, request that the list be sorted
                manager.SortComponents();

                //begin wizard
                return RedirectToRoute(AssignmentWizardAreaRegistration.AssignmentWizardRoute, new { controller = manager.ActiveComponent.ControllerName });
            }
            return View(Assignment.AllAssignmentTypes);
        }

        [HttpPost]
        public ActionResult Index(ICollection<AssignmentTypes> assignments)
        {
            string keyName = ViewBag.AssignmentTypeRadioName;
            if (Request.Form.AllKeys.Contains(keyName))
            {
                //set the assignment type and carry on to the next screen.
                manager.SetActiveAssignmentType(Request.Form[keyName]);
                return RedirectToRoute(new { controller = "Home", action = "SelectComponent" });
            }
            else
            {
                //send back to the old page if we didn't get anything.
                return View(Assignment.AllAssignmentTypes);
            }
        }

        public ActionResult SelectComponent(int? assignmentId)
        {
            //if our assignmentId isn't null, then we need to pull that assignment
            //from the DB
            if (assignmentId != null)
            {
                int aid = (int)assignmentId;
                manager.ActiveAssignmentId = aid;
                ActivateAssignmentComponents(db.Assignments.Find(aid));
                Assignment assignment = db.Assignments.Find(aid);
                manager.SetActiveAssignmentType((AssignmentTypes)assignment.AssignmentTypeID);
            }
            else
            {
                manager.ActiveAssignmentId = 0;
                manager.DeactivateAllComponents();
            }
            ViewBag.BeginWizardButton = beginWizardButton;
            return View(manager.GetComponentsForAssignmentType(manager.ActiveAssignmentType));
        }

        [HttpPost]
        public ActionResult SelectComponent(ICollection<WizardBaseController> components)
        {
            ActivateSelectedComponents();
            return RedirectToRoute(AssignmentWizardAreaRegistration.AssignmentWizardRoute, new { controller = manager.ActiveComponent.ControllerName });
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

        public ActionResult ContextLost(int? assignmentId)
        {
            Assignment model = new Assignment();
            if (assignmentId != null)
            {
                model = db.Assignments.Find((int)assignmentId);
            }
            return View(model);
        }

        #endregion

        #region helper methods
        private void ActivateAssignmentComponents(Assignment assignment)
        {
            manager.DeactivateAllComponents();
            
            //RUBRICS
            if (assignment.HasRubric)
            {
                manager.GetComponentByType(typeof(RubricController)).IsSelected = true;
            }
            
            //COMMENT CATEGORIES
            if (assignment.HasCommentCategories)
            {
                manager.GetComponentByType(typeof(CommentCategoryController)).IsSelected = true;
            }

            //TEAM ASSIGNMENTS
            if (assignment.HasTeams)
            {
                manager.GetComponentByType(typeof(TeamController)).IsSelected = true;
            }

            //DELIVERABLES
            if (assignment.HasDeliverables)
            {
                manager.GetComponentByType(typeof(DeliverablesController)).IsSelected = true;
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
                    WizardBaseController comp = manager.GetComponentByName(Request.Form[key]);
                    comp.IsSelected = true;
                }
            }

            //if we're loading a previous assignment, we need to go through and 
            //remove links to any unselected items
            if (manager.ActiveAssignmentId != 0)
            {
                Assignment assignment = db.Assignments.Find(manager.ActiveAssignmentId);

                //RUBRICS
                if (manager.UnselectedComponents.Contains(manager.GetComponentByType(typeof(RubricController))))
                {
                    assignment.Rubric = null;
                    assignment.RubricID = null;
                }

                //COMMENT CATEGORIES
                if (manager.UnselectedComponents.Contains(manager.GetComponentByType(typeof(CommentCategoryController))))
                {
                    assignment.CommentCategory = null;
                    assignment.CommentCategoryID = null;
                }

                //DELIVERABLES
                if (manager.UnselectedComponents.Contains(manager.GetComponentByType(typeof(DeliverablesController))))
                {
                    assignment.Deliverables = null;
                }

                db.Entry(assignment).State = System.Data.EntityState.Modified;
                db.SaveChanges();
            }
        }
        #endregion
    }
}
