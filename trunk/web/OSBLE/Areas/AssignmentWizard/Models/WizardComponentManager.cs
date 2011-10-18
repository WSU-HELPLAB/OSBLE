using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentWizard.Controllers;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public class WizardComponentManager
    {

        private const string assignmentKey = "ComponentManagerStudioAssignmentKey";
        private const string activeComponentIndexKey = "ComponentManagerActiveComponentIndex";
        private const string instance = "_wcm_instance";

        public ICollection<WizardComponent> AllComponents
        {
            get;
            protected set;
        }

        public ICollection<WizardComponent> SelectedComponents { get; protected set; }

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
                HttpContext.Current.Session[assignmentKey] = value;
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
                HttpContext.Current.Session[activeComponentIndexKey] = value;
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
            RegisterComponents();
        }

        /// <summary>
        /// Singleton pattern implementation.
        /// </summary>
        /// <returns></returns>
        public static WizardComponentManager GetInstance()
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

            //SUBMISSIONS
            comp = new WizardComponent()
            {
                Controller = new DeliverablesController(),
                IsSelected = false,
                IsRequired = false
            };
            AllComponents.Add(comp);

            //TEAM ASSIGNMENTS
            comp = new WizardComponent()
            {
                Controller = new TeamController(),
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

        }
    }
}