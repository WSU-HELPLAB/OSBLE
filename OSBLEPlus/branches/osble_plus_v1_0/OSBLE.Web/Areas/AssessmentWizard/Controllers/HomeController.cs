using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;

using OSBLE.Areas.AssessmentWizard.Models;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Attributes;
using OSBLE.Controllers;
using OSBLE.Models.Assessments;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssessmentWizard.Controllers
{
    [CanCreateCourses]
    public class HomeController : OSBLEController
    {
        private readonly AssessmentWizardComponentManager _manager;
        public static string BeginWizardButton = "StartWizardButton";

        public HomeController()
        {
            ViewBag.AssessmentTypeRadioName = "AssessmentType";
            _manager = new AssessmentWizardComponentManager(CurrentUser);
        }

        #region action results

        public ActionResult Index(int? assessmentId)
        {
            //existing assignments skip this step
            if (assessmentId != null)
            {
                Assessment assessment = db.Assessments.Find(assessmentId);

                //prime the manager for the new assignment
                _manager.ActiveAssessmentId = assessment.ID;
                _manager.SetActiveAssessmentType((AssessmentType)assessment.AssessmentTypeID);
                _manager.IsNewAssessment = false;

                //load in any secondary (non-required) components
                ActivateAssessmentComponents(assessment);

                //now, load in essential components
                List<AssessmentBaseController> components = (from comp in _manager.GetComponentsForAssignmentType(assessment.Type)
                                                         where comp.IsRequired == true
                                                         select comp).ToList();
                foreach (AssessmentBaseController component in components)
                {
                    component.IsSelected = true;
                }

                //finally, request that the list be sorted
                _manager.SortComponents();

                //begin wizard
                return RedirectToRoute(AssessmentWizardAreaRegistration.AssessmentWizardRoute, new { controller = _manager.ActiveComponent.ControllerName });
            }
            _manager.IsNewAssessment = true;
            return View(Assessment.AllAssessmentTypes.OrderBy(e => e.ToString()).ToList());
        }

        [HttpPost]
        public ActionResult Index(ICollection<AssessmentType> assignments)
        {
            string keyName = ViewBag.AssessmentTypeRadioName;
            if (!Request.Form.AllKeys.Contains(keyName)) return View(Assessment.AllAssessmentTypes);

            //set the assignment type and carry on to the next screen.
            _manager.SetActiveAssessmentType(Request.Form[keyName]);
            return RedirectToRoute(new { controller = "Home", action = "SelectComponent", area = "AssessmentWizard" });

            //send back to the old page if we didn't get anything.
        }

        public ActionResult SelectComponent(int? assignmentId)
        {
            //if our assignmentId isn't null, then we need to pull that assignment
            //from the DB
            if (assignmentId != null && assignmentId != 0)
            {
                var aid = (int)assignmentId;
                _manager.ActiveAssessmentId = aid;
                ActivateAssessmentComponents(db.Assessments.Find(aid));
                var assignment = db.Assessments.Find(aid);
                _manager.SetActiveAssessmentType((AssessmentType)assignment.AssessmentTypeID);
            }
            else
            {
                _manager.ActiveAssessmentId = 0;
                _manager.DeactivateAllComponents();
            }
            ViewBag.BeginWizardButton = BeginWizardButton;
            ViewBag.AssignmentType = _manager.ActiveAssessmentType.Explode();
            return View(_manager.GetComponentsForAssignmentType(_manager.ActiveAssessmentType));
        }

        [HttpPost]
        public ActionResult SelectComponent(ICollection<AssessmentBaseController> components)
        {
            ActivateSelectedComponents();
            return RedirectToRoute(AssessmentWizardAreaRegistration.AssessmentWizardRoute, new { controller = _manager.ActiveComponent.ControllerName });
        }

        public ActionResult Summary(int? assessmentId)
        {
            var assessment = assessmentId.HasValue
                           ? db.Assessments.Find(assessmentId.Value)
                           : db.Assessments.Find(_manager.ActiveAssessmentId);
            return View(assessment);
        }

        public ActionResult ContextLost(int? assessmentId)
        {
            var model = new Assessment();
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
            _manager.DeactivateAllComponents();

            //AC TODO: Add components based on assignment properties.  See 
            // AssignmentWizard -> HomeController for examples.
        }

        private void ActivateSelectedComponents()
        {
            _manager.DeactivateAllComponents();
            const string componentPrefix = "component_";
            foreach (var comp in from key in Request.Form.AllKeys
                                 where key.Substring(0, componentPrefix.Length) == componentPrefix
                                 select _manager.GetComponentByName(Request.Form[key]))
            {
                comp.IsSelected = true;
            }

            //if we're loading a previous assignment, we need to go through and 
            //remove links to any unselected items
            if (_manager.ActiveAssessmentId != 0)
            {
                var assessment = db.Assessments.Find(_manager.ActiveAssessmentId);

                //AC TODO: Add components based on assignment properties.  See 
                // AssignmentWizard -> HomeController for examples.
                db.Entry(assessment).State = EntityState.Modified;
                db.SaveChanges();
            }
        }
        #endregion
    }
}
