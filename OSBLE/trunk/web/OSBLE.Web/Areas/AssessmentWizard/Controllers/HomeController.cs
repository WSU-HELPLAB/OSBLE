using OSBLE.Areas.AssessmentWizard.Models;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Attributes;
using OSBLE.Controllers;
using OSBLE.Models.Assessments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Areas.AssessmentWizard.Controllers
{
    [CanCreateCourses]
    public class HomeController : OSBLEController
    {
        private AssessmentWizardComponentManager manager;
        public static string beginWizardButton = "StartWizardButton";

        public HomeController()
        {
            ViewBag.AssessmentTypeRadioName = "AssessmentType";
            manager = new AssessmentWizardComponentManager(CurrentUser);
        }

        #region action results

        public ActionResult Index(int? assessmentId)
        {
            //existing assignments skip this step
            if (assessmentId != null)
            {
                Assessment assessment = db.Assessments.Find(assessmentId);

                //prime the manager for the new assignment
                manager.ActiveAssessmentId = assessment.ID;
                manager.SetActiveAssessmentType((AssessmentType)assessment.AssessmentTypeID);
                manager.IsNewAssessment = false;

                //load in any secondary (non-required) components
                ActivateAssessmentComponents(assessment);

                //now, load in essential components
                List<AssessmentBaseController> components = (from comp in manager.GetComponentsForAssignmentType(assessment.Type)
                                                         where comp.IsRequired == true
                                                         select comp).ToList();
                foreach (AssessmentBaseController component in components)
                {
                    component.IsSelected = true;
                }

                //finally, request that the list be sorted
                manager.SortComponents();

                //begin wizard
                return RedirectToRoute(AssessmentWizardAreaRegistration.AssessmentWizardRoute, new { controller = manager.ActiveComponent.ControllerName });
            }
            manager.IsNewAssessment = true;
            return View(Assessment.AllAssessmentTypes.OrderBy(e => e.ToString()).ToList());
        }

        [HttpPost]
        public ActionResult Index(ICollection<AssessmentType> assignments)
        {
            string keyName = ViewBag.AssessmentTypeRadioName;
            if (Request.Form.AllKeys.Contains(keyName))
            {
                //set the assignment type and carry on to the next screen.
                manager.SetActiveAssessmentType(Request.Form[keyName]);
                return RedirectToRoute(new { controller = "Home", action = "SelectComponent", area = "AssessmentWizard" });
            }
            else
            {
                //send back to the old page if we didn't get anything.
                return View(Assessment.AllAssessmentTypes);
            }
        }

        public ActionResult SelectComponent(int? assignmentId)
        {
            //if our assignmentId isn't null, then we need to pull that assignment
            //from the DB
            if (assignmentId != null && assignmentId != 0)
            {
                int aid = (int)assignmentId;
                manager.ActiveAssessmentId = aid;
                ActivateAssessmentComponents(db.Assessments.Find(aid));
                Assessment assignment = db.Assessments.Find(aid);
                manager.SetActiveAssessmentType((AssessmentType)assignment.AssessmentTypeID);
            }
            else
            {
                manager.ActiveAssessmentId = 0;
                manager.DeactivateAllComponents();
            }
            ViewBag.BeginWizardButton = beginWizardButton;
            ViewBag.AssignmentType = manager.ActiveAssessmentType.Explode();
            return View(manager.GetComponentsForAssignmentType(manager.ActiveAssessmentType));
        }

        [HttpPost]
        public ActionResult SelectComponent(ICollection<AssessmentBaseController> components)
        {
            ActivateSelectedComponents();
            return RedirectToRoute(AssessmentWizardAreaRegistration.AssessmentWizardRoute, new { controller = manager.ActiveComponent.ControllerName });
        }

        public ActionResult Summary(int? assessmentId)
        {
            Assessment assessment = new Assessment();
            if (assessmentId != null)
            {
                assessment = db.Assessments.Find((int)assessmentId);
            }
            else
            {
                assessment = db.Assessments.Find(manager.ActiveAssessmentId);
            }
            return View(assessment);
        }

        public ActionResult ContextLost(int? assessmentId)
        {
            Assessment model = new Assessment();
            if (assessmentId != null)
            {
                model = db.Assessments.Find((int)assessmentId);
            }
            return View(model);
        }

        #endregion

        #region helper methods
        private void ActivateAssessmentComponents(Assessment assignment)
        {
            manager.DeactivateAllComponents();

            //AC TODO: Add components based on assignment properties.  See 
            // AssignmentWizard -> HomeController for examples.
        }

        private void ActivateSelectedComponents()
        {
            manager.DeactivateAllComponents();
            string componentPrefix = "component_";
            foreach (string key in Request.Form.AllKeys)
            {
                if (key.Substring(0, componentPrefix.Length) == componentPrefix)
                {
                    IWizardBaseController comp = manager.GetComponentByName(Request.Form[key]);
                    comp.IsSelected = true;
                }
            }

            //if we're loading a previous assignment, we need to go through and 
            //remove links to any unselected items
            if (manager.ActiveAssessmentId != 0)
            {
                Assessment assessment = db.Assessments.Find(manager.ActiveAssessmentId);

                //AC TODO: Add components based on assignment properties.  See 
                // AssignmentWizard -> HomeController for examples.
                db.Entry(assessment).State = System.Data.EntityState.Modified;
                db.SaveChanges();
            }
        }
        #endregion
    }
}
