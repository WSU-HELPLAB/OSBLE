using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Controllers.Assignments.Wizard
{
    public class WizardComponentManager
    {
        private static WizardComponentManager _instance = null;
        private IDictionary<WizardBaseController, ICollection<WizardBaseController>> components;

        public ICollection<WizardBaseController> Components
        {
            get
            {
                return components.Keys;
            }
        }

        private WizardComponentManager()
        {
            components = new Dictionary<WizardBaseController, ICollection<WizardBaseController>>();
            RegisterComponents();
        }

        /// <summary>
        /// Singleton pattern implementation.
        /// </summary>
        /// <returns></returns>
        public static WizardComponentManager GetInstance()
        {
            if (_instance == null)
            {
                return new WizardComponentManager();
            }
            else
            {
                return _instance;
            }
        }

        /// <summary>
        /// Registers all components with the component manager
        /// </summary>
        private void RegisterComponents()
        {
            //AC: Seems like there should be a better (more generic) way to handle this
            WizardBaseController comp;

            //BASICS COMPONENT
            comp = new BasicsController();
            components.Add(comp, comp.Prerequisites);

            //TEAM ASSIGNMENTS
            comp = new TeamController();
            components.Add(comp, comp.Prerequisites);

            //RUBRICS
            comp = new RubricController();
            components.Add(comp, comp.Prerequisites);

        }

    }
}