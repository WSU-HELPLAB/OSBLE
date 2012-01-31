using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentWizard.Controllers;
using System.ComponentModel;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public class WizardComponentManager
    {

        private const string assignmentKey = "ComponentManagerStudioAssignmentKey";
        private const string activeComponentIndexKey = "ComponentManagerActiveComponentIndex";
        private const string instance = "_wcm_instance";
        private const string activeAssignmentTypeKey = "_wcm_activeAssignmentType";

        public ICollection<WizardComponent> AllComponents
        {
            get;
            protected set;
        }

        /// <summary>
        /// A list of the currently selected wizard components.  DO NOT directly modify this list.
        /// </summary>
        public ICollection<WizardComponent> SelectedComponents { get; protected set; }

        /// <summary>
        /// A list of unselected wizard components.  DO NOT directly modify this list.
        /// </summary>
        public ICollection<WizardComponent> UnselectedComponents { get; protected set; }

        public int ActiveAssignmentId
        {
            get
            {
                if (HttpContext.Current.Session[assignmentKey] != null)
                {
                    return (int)HttpContext.Current.Session[assignmentKey];
                }
                else
                {
                    HttpContext.Current.Session[assignmentKey] = 0;
                    return 0;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Session[assignmentKey] = value;
                }
            }
        }

        public AssignmentTypes ActiveAssignmentType
        {
            get
            {
                if (HttpContext.Current.Session[activeAssignmentTypeKey] != null)
                {
                    return (AssignmentTypes)HttpContext.Current.Session[activeAssignmentTypeKey];
                }
                else
                {
                    HttpContext.Current.Session[activeAssignmentTypeKey] = AssignmentTypes.Basic;
                    return AssignmentTypes.Basic;
                }
            }
            private set
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Session[activeAssignmentTypeKey] = value;
                }
            }
        }

        private int ActiveComponentIndex
        {
            get
            {
                if (HttpContext.Current.Session[activeComponentIndexKey] != null)
                {
                    return (int)HttpContext.Current.Session[activeComponentIndexKey];
                }
                else
                {
                    HttpContext.Current.Session[activeComponentIndexKey] = 0;
                    return 0;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Session[activeComponentIndexKey] = value;
                }
            }
        }

        public WizardComponent ActiveComponent
        {
            get
            {
                return SelectedComponents.ElementAt(ActiveComponentIndex);
            }
        }

        public WizardComponent GetNextComponent()
        {
            if (ActiveComponentIndex < SelectedComponents.Count - 1)
            {
                ActiveComponentIndex++;
                return ActiveComponent;
            }
            return null;
        }

        public WizardComponent GetPreviousComponent()
        {
            if (ActiveComponentIndex > 0)
            {
                ActiveComponentIndex--;
                return ActiveComponent;
            }
            return null;
        }

        public WizardComponent GetComponentByName(string name)
        {
            WizardComponent component = (from c in AllComponents
                                         where c.Name.CompareTo(name) == 0
                                         select c).FirstOrDefault();
            return component;
        }

        public WizardComponent GetComponentByType(Type type)
        {
            WizardComponent component = (from c in AllComponents
                                         where c.Controller.GetType() == type
                                         select c).FirstOrDefault();
            return component;
        }

        private WizardComponentManager()
        {
            AllComponents = new List<WizardComponent>();
            SelectedComponents = new List<WizardComponent>();
            UnselectedComponents = new List<WizardComponent>();
            RegisterComponents();
        }

        /// <summary>
        /// Singleton pattern implementation.
        /// </summary>
        /// <returns></returns>
        public static WizardComponentManager GetInstance()
        {
            //HttpContext will be null when we're not running this code in a web context,
            //I.E. testing
            if (HttpContext.Current == null)
            {
                return new WizardComponentManager();
            }
            else
            {
                if (HttpContext.Current.Session[instance] == null)
                {
                    WizardComponentManager mgr = new WizardComponentManager();
                    HttpContext.Current.Session[instance] = mgr;
                    return mgr;
                }
                else
                {
                    return HttpContext.Current.Session[instance] as WizardComponentManager;
                }
            }
        }

        /// <summary>
        /// Deactivates (set's IsSelected to false) all components.  Returns the manager
        /// to the default state.
        /// </summary>
        public void DeactivateAllComponents()
        {
            foreach (WizardComponent component in AllComponents)
            {
                component.IsSelected = false;
            }
            ActiveComponentIndex = 0;
            SelectedComponents.Clear();
        }

        /// <summary>
        /// Fired whenever a component property's value gets changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ComponentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //presently, we only care about selection change
            if (e.PropertyName.CompareTo("IsSelected") == 0)
            {
                WizardComponent component = sender as WizardComponent;
                if (component.IsSelected)
                {
                    if (UnselectedComponents.Contains(component))
                    {
                        UnselectedComponents.Remove(component);
                    }
                    if (!SelectedComponents.Contains(component))
                    {
                        SelectedComponents.Add(component);
                    }
                }
                else
                {
                    if (SelectedComponents.Contains(component))
                    {
                        SelectedComponents.Remove(component);
                    }
                    if (!UnselectedComponents.Contains(component))
                    {
                        UnselectedComponents.Add(component);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the active assignment type by trying to match the supplied parameter with possible assignment types
        /// listed in the AssignmentTypes enumeration.  Will default to AssignmentTypes.Basic if no match was found.
        /// </summary>
        /// <param name="assignmentType"></param>
        /// <returns>True if a good match was found, false otherwise.</returns>
        public bool SetActiveAssignmentType(string assignmentType)
        {
            IList<AssignmentTypes> possibleTypes = Assignment.AllAssignmentTypes;
            foreach (AssignmentTypes type in possibleTypes)
            {
                if (assignmentType.Contains(type.ToString()))
                {
                    return SetActiveAssignmentType(type);
                }
            }

            //default to basic
            SetActiveAssignmentType(AssignmentTypes.Basic);
            return false;
        }

        /// <summary>
        /// Sets the active assignment type based on the supplied parameter.
        /// </summary>
        /// <param name="assignmentType"></param>
        /// <returns>Always true</returns>
        public bool SetActiveAssignmentType(AssignmentTypes assignmentType)
        {
            ActiveAssignmentType = assignmentType;
            return true;
        }

        /// <summary>
        /// Registers all components with the component manager
        /// </summary>
        private void RegisterComponents()
        {
            //AC: Seems like there should be a better (more generic) way to handle this
            WizardComponent comp;

            //BASICS COMPONENT
            comp = new WizardComponent()
            {
                Controller = new BasicsController(),
                IsSelected = true,
                IsRequired = true
            };
            AllComponents.Add(comp);

            //TEAM ASSIGNMENTS
            comp = new WizardComponent()
            {
                Controller = new TeamController(),
                IsSelected = true,
                IsRequired = true
            };
            AllComponents.Add(comp);

            //SUBMISSIONS
            comp = new WizardComponent()
            {
                Controller = new DeliverablesController(),
                IsSelected = false,
                IsRequired = false
            };
            AllComponents.Add(comp);

            //RUBRICS
            comp = new WizardComponent()
            {
                Controller = new RubricController(),
                IsSelected = false,
                IsRequired = false
            };
            AllComponents.Add(comp);

            //LINE BY LINE REVIEWS (COMMENT CATEGOIRES)
            comp = new WizardComponent()
            {
                Controller = new CommentCategoryController(),
                IsSelected = false,
                IsRequired = false
            };
            AllComponents.Add(comp); 

            //attach event listeners to selection change
            foreach (WizardComponent component in AllComponents)
            {
                UnselectedComponents.Add(component);
                component.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(ComponentPropertyChanged);
            }
        }
    }
}