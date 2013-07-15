using OSBLE.Areas.AssessmentWizard.Models;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Attributes;
using OSBLE.Controllers;
using OSBLE.Models.Assessments;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Areas.AssessmentWizard.Controllers
{
    public abstract class AssessmentBaseController : OSBLEController, IWizardBaseController
    {
        protected AssessmentWizardComponentManager manager;
        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        /// <summary>
        /// Returns the controller's name.  Must be unique.  The GET method should
        /// return a static string.
        /// </summary>
        public abstract string ControllerName { get; }

        /// <summary>
        /// Returns the controller's pretty name.  By default, it's just the same as ControllerName.
        /// However, if you wanted to do something more fancy (ex: display 'foobar' as 'foo bar') you could 
        /// do that here.
        /// </summary>
        public virtual string PrettyName { get { return ControllerName; } }

        /// <summary>
        /// To be used by the WizardComponentManager to aid in component sorting.  It's okay to access
        /// the number if need be, but try not to set the value unless you know what you're doing.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Returns a brief description of the controller's purpose.  Used mainly on the "Start"
        /// page
        /// </summary>
        public abstract string ControllerDescription { get; }

        /// <summary>
        /// Returns a concrete WizardBaseController that must preceed the current controller.  
        /// For example, most controllers will expect an assignment to have at least a name.  The
        /// "Basics" controller handles setting up assignment basics and so other controllers should
        /// list "Basics" as being a prerequisite.
        /// </summary>
        public abstract IWizardBaseController Prerequisite { get; }

        /// <summary>
        /// Provides a list of assignment types in which the current component is relevant.  Several
        /// components are relevant in all assignment types, but others only make sense in a small subset
        /// </summary>
        public abstract ICollection<AssessmentType> ValidAssessmentTypes { get; }

        /// <summary>
        /// Shorthand for returning all AssignmentTypes in the system.
        /// </summary>
        protected ICollection<AssessmentType> AllAssessmentTypes
        {
            get
            {
                return Enum.GetValues(typeof(AssessmentType)).Cast<AssessmentType>().ToList();
            }
        }

        /// <summary>
        /// Used to hold an instance of the current assessment to be used by the component
        /// </summary>
        public Assessment Assessment { get; set; }

        /// <summary>
        /// Whether or not the component successfully updated the Assignment in the database
        /// </summary>
        public bool WasUpdateSuccessful { get; set; }


        private bool _isSelected = false;

        /// <summary>
        /// UI.  Whether or not the current component is selected by the user in the Assignment Wizard
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                _isSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        /// <summary>
        /// UI Whether or not the current component is required during assignment creation.  
        /// By default, no component is required.  To make component required, override this 
        /// property in your subclass.
        /// </summary>
        public virtual bool IsRequired
        {
            get
            {
                return false;
            }
        }

        public AssessmentBaseController()
        {
            WasUpdateSuccessful = true;
            SortOrder = 0;
        }

        private void SetUpViewBag()
        {
            ViewBag.Components = manager.SelectedComponents;
            ViewBag.ActiveComponent = manager.ActiveComponent;
            ViewBag.Assessment = Assessment;
            if (manager.IsNewAssessment)
            {
                ViewBag.IsNewAssignment = true;

                //AC Note: clean this up.
                if (Assessment != null)
                {
                    if (string.IsNullOrEmpty(Assessment.AssessmentName) == false)
                    {
                        ViewBag.Title = string.Format(@"Create New {0} Assignment ""{1}"": {2}", manager.ActiveAssessmentType.Explode(), Assessment.AssessmentName, manager.ActiveComponent.PrettyName);
                    }
                    else
                    {
                        ViewBag.Title = string.Format(@"Create New {0} Assignment: {1}", manager.ActiveAssessmentType.Explode(), manager.ActiveComponent.PrettyName);
                    }
                }
                else
                {
                    ViewBag.Title = string.Format(@"Create New {0} Assignment: {1}", manager.ActiveAssessmentType.Explode(), manager.ActiveComponent.PrettyName);
                }
            }
            else
            {
                if (Assessment != null)
                {
                    if (string.IsNullOrEmpty(Assessment.AssessmentName) == false)
                    {
                        ViewBag.Title = string.Format(@"Edit {0} Assignment ""{1}"": {2}", manager.ActiveAssessmentType.Explode(), Assessment.AssessmentName, manager.ActiveComponent.PrettyName);
                    }
                    else
                    {
                        ViewBag.Title = string.Format(@"Edit {0} Assignment: {1}", manager.ActiveAssessmentType.Explode(), manager.ActiveComponent.PrettyName);
                    }
                }
                else
                {
                    ViewBag.Title = string.Format(@"Edit {0} Assignment: {1}", manager.ActiveAssessmentType.Explode(), manager.ActiveComponent.PrettyName);
                }
                ViewBag.IsNewAssignment = false;
            }
        }

        [CanModifyCourse]
        public virtual ActionResult Index()
        {
            Assessment = new Assessment();
            manager = new AssessmentWizardComponentManager(CurrentUser);
            if (manager.ActiveAssessmentId != 0)
            {
                Assessment = db.Assessments.Find(manager.ActiveAssessmentId);
            }
            else
            {
                Assessment = new Assessment();
            }
            SetUpViewBag();
            return View();
        }

        [HttpPost]
        protected ActionResult PostBack(dynamic model)
        {
            manager = new AssessmentWizardComponentManager(CurrentUser);
            if (WasUpdateSuccessful)
            {
                //update the assignment ID.  Probably not necessary when working
                //with existing assignments, but needed for new assignments.
                manager.ActiveAssessmentId = Assessment.ID;

                //Check manager context.  If for some reason the user lost their
                //SESSION context, we need to detect this and redirect them
                //to a safe place.  Otherwise, they will get an error when we
                //try to redirect them to a non-existant page
                if (manager.SelectedComponents.Count == 0)
                {
                    return RedirectToRoute(new { controller = "Home", action = "ContextLost", assignmentId = Assessment.ID });
                }

                string errorPath = "Home";
                string action = "ContextLost";
                int id = Assessment.ID;
                IWizardBaseController comp = null;
                if (Request.Form.AllKeys.Contains(WizardNavButtons.PreviousButton.ToString()))
                {
                    comp = manager.GetPreviousComponent();
                    errorPath = "Home";
                    action = "SelectComponent";
                }
                else if (Request.Form.AllKeys.Contains(WizardNavButtons.NextButton.ToString()))
                {
                    comp = manager.GetNextComponent();
                    errorPath = "Home";
                    action = "Summary";
                }
                else if (Request.Form.AllKeys.Contains(WizardNavButtons.SaveAndExitButton.ToString()))
                {
                    return RedirectToRoute("Default",
                                          new
                                          {
                                              controller = "Assignment",
                                              action = "index"
                                          }
                                          );
                }
                else
                {
                    //not having any form keys means that we're using the QuickNav as it won't send back
                    //any submit button data
                    return QuickNav();
                }

                if (comp != null)
                {
                    return RedirectToRoute(AssessmentWizardAreaRegistration.AssessmentWizardRoute,
                                          new
                                          {
                                              controller = manager.ActiveComponent.ControllerName
                                          }
                                          );
                }
                else
                {
                    return RedirectToRoute(AssessmentWizardAreaRegistration.AssessmentWizardRoute,
                                          new
                                          {
                                              controller = errorPath,
                                              action = action,
                                              assignmentId = id
                                          }
                                          );
                }
            }
            try
            {
                SetUpViewBag();
            }
            catch (Exception)
            {
                return RedirectToRoute(new { controller = "Home", action = "ContextLost", assignmentId = Assessment.ID });
            }
            return View(model);
        }

        [HttpPost]
        public ActionResult QuickNav()
        {
            string componentName = Request.Form["ComponentName"];
            manager = new AssessmentWizardComponentManager(CurrentUser);
            IWizardBaseController componentToFind = manager.GetComponentByName(componentName);

            //start at the beginning
            while (manager.GetPreviousComponent() != null) ;

            //empty component name = start at the beginning (component selection)
            if (componentName.Length == 0)
            {
                return RedirectToRoute(AssessmentWizardAreaRegistration.AssessmentWizardRoute,
                                    new
                                    {
                                        controller = "Home",
                                        action = "SelectComponent",
                                        assignmentId = manager.ActiveAssessmentId
                                    }
                                  );
            }

            //from the beginning, find the component that we want to jump to
            bool found = false;
            while (!found)
            {
                if (manager.ActiveComponent == null)
                {
                    found = true;
                }
                else if (manager.ActiveComponent.ControllerName.CompareTo(componentToFind.ControllerName) == 0)
                {
                    found = true;
                }
                else
                {
                    manager.GetNextComponent();
                }
            }

            //redirect to the component
            return RedirectToRoute(AssessmentWizardAreaRegistration.AssessmentWizardRoute,
                                    new
                                    {
                                        controller = componentToFind.ControllerName,
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
            return string.Format("{0}", this.ControllerName);
        }

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public bool Equals(IWizardBaseController other)
        {
            if (this.ControllerName.CompareTo(other.ControllerName) == 0)
            {
                return true;
            }
            return false;
        }
    }

}
