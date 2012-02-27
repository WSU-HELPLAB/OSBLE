using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentWizard.Controllers;
using System.ComponentModel;
using System.Reflection;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public class WizardComponentManager
    {

        private const string assignmentKey = "ComponentManagerStudioAssignmentKey";
        private const string activeComponentIndexKey = "ComponentManagerActiveComponentIndex";
        private const string instance = "_wcm_instance";
        private const string activeAssignmentTypeKey = "_wcm_activeAssignmentType";

        private WizardComponentManager()
        {
            AllComponents = new List<WizardBaseController>();
            SelectedComponents = new List<WizardBaseController>();
            UnselectedComponents = new List<WizardBaseController>();
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

        public List<WizardBaseController> AllComponents
        {
            get;
            protected set;
        }

        /// <summary>
        /// A list of the currently selected wizard components.  DO NOT directly modify this list.
        /// </summary>
        public List<WizardBaseController> SelectedComponents { get; protected set; }

        /// <summary>
        /// A list of unselected wizard components.  DO NOT directly modify this list.
        /// </summary>
        public List<WizardBaseController> UnselectedComponents { get; protected set; }

        public void SortComponents()
        {
            if (AllComponents[0].SortOrder != 0)
            {
                SelectedComponents.Sort(new WizardSortOrderComparer());
                UnselectedComponents.Sort(new WizardSortOrderComparer());
            }
            else
            {
                SelectedComponents.Sort(new WizardPrerequisiteComparer());
                UnselectedComponents.Sort(new WizardPrerequisiteComparer());
            }
        }

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

        public WizardBaseController ActiveComponent
        {
            get
            {
                return SelectedComponents.ElementAt(ActiveComponentIndex);
            }
        }

        public WizardBaseController GetNextComponent()
        {
            if (ActiveComponentIndex < SelectedComponents.Count - 1)
            {
                ActiveComponentIndex++;
                return ActiveComponent;
            }
            return null;
        }

        public WizardBaseController GetPreviousComponent()
        {
            if (ActiveComponentIndex > 0)
            {
                ActiveComponentIndex--;
                return ActiveComponent;
            }
            return null;
        }

        public ICollection<WizardBaseController> GetComponentsForAssignmentType(AssignmentTypes type)
        {
            return AllComponents
                .Where(a => a.ValidAssignmentTypes.Contains(type))
                .ToList();
        }

        public WizardBaseController GetComponentByName(string name)
        {
            WizardBaseController component = (from c in AllComponents
                                         where c.ControllerName.CompareTo(name) == 0
                                         select c).FirstOrDefault();
            return component;
        }

        public WizardBaseController GetComponentByType(Type type)
        {
            WizardBaseController component = (from c in AllComponents
                                         where c.GetType() == type
                                         select c).FirstOrDefault();
            return component;
        }

        /// <summary>
        /// Deactivates (set's IsSelected to false) all components.  Returns the manager
        /// to the default state.
        /// </summary>
        public void DeactivateAllComponents()
        {
            foreach (WizardBaseController component in AllComponents)
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
                WizardBaseController component = sender as WizardBaseController;
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
            //use reflection to find all available components
            string componentNamespace = "OSBLE.Areas.AssignmentWizard.Controllers";
            List<Type> componentObjects = (from type in Assembly.GetExecutingAssembly().GetTypes()
                           where 
                           type.IsSubclassOf(typeof(WizardBaseController))
                           && 
                           type.Namespace.CompareTo(componentNamespace) == 0
                           select type).ToList();

            foreach (Type component in componentObjects)
            {
                WizardBaseController controller = Activator.CreateInstance(component) as WizardBaseController;
                AllComponents.Add(controller);
            }

            //If we have already sorted in the past, use the component's sort order to make things go faster.
            //Otherwise, do it the long way
            if (AllComponents[0].SortOrder != 0)
            {
                AllComponents.Sort(new WizardSortOrderComparer());            
            }
            else
            {
                AllComponents.Sort(new WizardPrerequisiteComparer());            
            }
            
            //attach event listeners to selection change
            //and apply a sort order for faster sorting in the future
            int counter = 1;
            foreach (WizardBaseController component in AllComponents)
            {
                component.SortOrder = counter;
                UnselectedComponents.Add(component);
                component.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(ComponentPropertyChanged);
                counter++;
            }
        }
    }
}