using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;
using OSBLE.Areas.AssignmentWizard.Controllers;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Caching;
using OSBLE.Utility;
using System.Collections.ObjectModel;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public class WizardComponentManager
    {

        private const string assignmentKey = "ComponentManagerStudioAssignmentKey";
        private const string activeComponentIndexKey = "ComponentManagerActiveComponentIndex";
        private const string instance = "_wcm_instance";
        private const string activeAssignmentTypeKey = "_wcm_activeAssignmentType";
        private const string isNewAssignmentKey = "_wcm_isNewAssignment";
        private const string componentsCacheString = "sortedComponents";
        private const string managerCookieString = "_wcm_managerCookie";
        private const string cacheRegion = "OSBLE.Areas.AssignmentWizard.Models.WizardComponentManager";
        private const string allComponentsKey = "_wcm_allComponents";
        private const string selectedComponentsKey = "_wcm_selectedComponents";
        private const string unselectedComponentsKey = "_wcm_unselectedComponents";

        private HttpCookie managerCookie;
        private ObservableCollection<WizardBaseController> _selectedComponents = new ObservableCollection<WizardBaseController>();
        private ObservableCollection<WizardBaseController> _unselectedComponents = new ObservableCollection<WizardBaseController>();

        #region constructor

        private WizardComponentManager()
        {
            AllComponents = new List<WizardBaseController>();
            SelectedComponents = new ObservableCollection<WizardBaseController>();
            UnselectedComponents = new ObservableCollection<WizardBaseController>();

            if (HttpContext.Current != null)
            {
                managerCookie = HttpContext.Current.Request.Cookies.Get(managerCookieString);
                if (managerCookie == null)
                {
                    managerCookie = new HttpCookie(managerCookieString);
                    managerCookie.Expires = DateTime.Now.AddHours(24.0);
                }
                HttpContext.Current.Response.SetCookie(managerCookie);
            }

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

        #endregion

        #region properties

        public List<WizardBaseController> AllComponents { get; private set; }

        /// <summary>
        /// A list of the currently selected wizard components.  DO NOT directly modify this list.
        /// </summary>
        public ObservableCollection<WizardBaseController> SelectedComponents
        {
            get
            {
                return _selectedComponents;
            }
            protected set
            {
                if (_selectedComponents != null)
                {
                    _selectedComponents.CollectionChanged -= new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ComponentsCollectionChanged);
                }
                _selectedComponents = value;
                _selectedComponents.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ComponentsCollectionChanged);
            }
        }

        /// <summary>
        /// A list of unselected wizard components.  DO NOT directly modify this list.
        /// </summary>
        public ObservableCollection<WizardBaseController> UnselectedComponents
        {
            get
            {
                return _unselectedComponents;
            }
            protected set
            {
                if (_unselectedComponents != null)
                {
                    _unselectedComponents.CollectionChanged -= new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ComponentsCollectionChanged);
                }
                _unselectedComponents = value;
                _unselectedComponents.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(ComponentsCollectionChanged);
            }
        }

        void ComponentsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (HttpContext.Current != null)
            {
                //update all collections
                string selectedComponentPieces = string.Join("|", _selectedComponents);
                string unselectedComponentsPieces = string.Join("|", _unselectedComponents);

                managerCookie.Values[selectedComponentsKey] = selectedComponentPieces;
                managerCookie.Values[unselectedComponentsKey] = unselectedComponentsPieces;

                HttpContext.Current.Response.SetCookie(managerCookie);
            }
        }

        public bool IsNewAssignment
        {
            get
            {
                if (managerCookie.Values[isNewAssignmentKey] != null)
                {
                    return Convert.ToBoolean(managerCookie.Values[isNewAssignmentKey]);
                }
                else
                {
                    managerCookie.Values[isNewAssignmentKey] = true.ToString();
                    return true;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    managerCookie.Values[isNewAssignmentKey] = value.ToString();
                    HttpContext.Current.Response.SetCookie(managerCookie);
                }
            }
        }

        public int ActiveAssignmentId
        {
            get
            {
                if (managerCookie.Values[assignmentKey] != null)
                {
                    return Convert.ToInt32(managerCookie.Values[assignmentKey]);
                }
                else
                {
                    managerCookie.Values[assignmentKey] = "0";
                    return 0;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    managerCookie.Values[assignmentKey] = value.ToString();
                    HttpContext.Current.Response.SetCookie(managerCookie);
                }
            }
        }

        public AssignmentTypes ActiveAssignmentType
        {
            get
            {
                if (managerCookie.Values[activeAssignmentTypeKey] != null)
                {
                    return (AssignmentTypes)Convert.ToInt32(managerCookie.Values[activeAssignmentTypeKey]);
                }
                else
                {
                    managerCookie.Values[activeAssignmentTypeKey] = ((int)AssignmentTypes.Basic).ToString();
                    return AssignmentTypes.Basic;
                }
            }
            private set
            {
                if (HttpContext.Current != null)
                {
                    managerCookie.Values[activeAssignmentTypeKey] = ((int)value).ToString();
                    HttpContext.Current.Response.SetCookie(managerCookie);
                }
            }
        }

        private int ActiveComponentIndex
        {
            get
            {
                if (managerCookie.Values[activeComponentIndexKey] != null)
                {
                    return Convert.ToInt32(managerCookie.Values[activeComponentIndexKey]);
                }
                else
                {
                    managerCookie.Values[activeComponentIndexKey] = "0";
                    return 0;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    managerCookie.Values[activeComponentIndexKey] = value.ToString();
                    HttpContext.Current.Response.SetCookie(managerCookie);
                }
            }
        }

        public WizardBaseController ActiveComponent
        {
            get
            {
                WizardBaseController component = SelectedComponents.ElementAtOrDefault(ActiveComponentIndex);

                //null component probably means that we lost our context
                if (component == null)
                {
                    //load in selected / unselected components
                    SelectedComponents = new ObservableCollection<WizardBaseController>(ComponentsFromString(managerCookie[selectedComponentsKey], "|"));
                    UnselectedComponents = new ObservableCollection<WizardBaseController>(ComponentsFromString(managerCookie[unselectedComponentsKey], "|"));
                }

                //retry
                return SelectedComponents.ElementAt(ActiveComponentIndex);
            }
        }

        #endregion

        #region public methods

        public void SortComponents()
        {
            if (AllComponents[0].SortOrder != 0)
            {
                List<WizardBaseController> selectedAsList = SelectedComponents.ToList();
                selectedAsList.Sort(new WizardSortOrderComparer());
                SelectedComponents = new ObservableCollection<WizardBaseController>(selectedAsList);

                List<WizardBaseController> unselectedAsList = UnselectedComponents.ToList();
                unselectedAsList.Sort(new WizardSortOrderComparer());
                UnselectedComponents = new ObservableCollection<WizardBaseController>(unselectedAsList);
            }
            else
            {
                List<WizardBaseController> selectedAsList = SortComponents(SelectedComponents.ToList());
                SelectedComponents = new ObservableCollection<WizardBaseController>(selectedAsList);

                List<WizardBaseController> unselectedAsList = SortComponents(UnselectedComponents.ToList());
                UnselectedComponents = new ObservableCollection<WizardBaseController>(unselectedAsList);
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

        #endregion

        #region private helpers

        /// <summary>
        /// Converts a delimited string into a list of <see cref="WizardBaseController "/> objects.
        /// </summary>
        /// <param name="itemString"></param>
        /// <param name="delimiter">The token used to delimit each entry</param>
        /// <returns></returns>
        private List<WizardBaseController> ComponentsFromString(string itemString, string delimiter)
        {
            List<WizardBaseController> components = new List<WizardBaseController>();
            string[] items = itemString.Split(delimiter.ToCharArray());
            foreach (string item in items)
            {
                WizardBaseController component = AllComponents.Find(c => c.ControllerName == item);
                components.Add(component);
            }
            return components;
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

            //pull from the cache if possible
            ObjectCache cache = new FileCache();
            bool loadedFromCache = false;
            string[] sorted = (string[])cache.Get(componentsCacheString, cacheRegion);
            if (sorted != null)
            {
                //set loaded to true for now
                loadedFromCache = true;

                //make sure that the sizes match up
                if (sorted.Length != AllComponents.Count)
                {
                    loadedFromCache = false;
                }

                //make sure all components are represented in the cache
                foreach (WizardBaseController component in AllComponents)
                {
                    if (!sorted.Contains(component.ControllerName))
                    {
                        loadedFromCache = false;
                        break;
                    }
                }

                //if we're still clear to load from the cache, then do so
                if (loadedFromCache)
                {
                    WizardBaseController[] tempComponentList = new WizardBaseController[sorted.Length];
                    for (int i = 0; i < sorted.Length; i++)
                    {
                        tempComponentList[i] = AllComponents.Find(c => c.ControllerName == sorted[i]);
                    }

                    //reassign the sorted list
                    AllComponents = tempComponentList.ToList();
                }
            }

            //if caching failed, do it the long way
            if (!loadedFromCache)
            {
                List<WizardBaseController> components = SortComponents(AllComponents);
                AllComponents = components;

                //save sorted component information to cache
                string[] sortedComponents = new string[AllComponents.Count];
                for (int i = 0; i < sortedComponents.Length; i++)
                {
                    sortedComponents[i] = AllComponents[i].ControllerName;
                }
                cache.Add(componentsCacheString, sortedComponents, DateTime.Now, cacheRegion);
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

        private List<WizardBaseController> SortComponents(List<WizardBaseController> unsorted)
        {
            List<WizardBaseController> components = new List<WizardBaseController>();
            Dictionary<string, NTree<WizardBaseController>> nodes = new Dictionary<string, NTree<WizardBaseController>>();
            NTree<WizardBaseController> rootNode = null;

            //build the tree
            foreach (WizardBaseController component in unsorted)
            {

                NTree<WizardBaseController> node = null;
                if (!nodes.ContainsKey(component.ControllerName))
                {
                    node = new NTree<WizardBaseController>(component);
                    nodes.Add(component.ControllerName, node);
                }
                else
                {
                    node = nodes[component.ControllerName];
                }

                //check for null prereq before we contine.  Null prereq denotes root node
                if (component.Prerequisite != null)
                {
                    NTree<WizardBaseController> parentNode = null;
                    if (nodes.ContainsKey(component.Prerequisite.ControllerName))
                    {
                        parentNode = nodes[component.Prerequisite.ControllerName];
                    }
                    else
                    {
                        parentNode = new NTree<WizardBaseController>(component.Prerequisite);
                        nodes.Add(component.Prerequisite.ControllerName, parentNode);
                    }
                    parentNode.addChild(node);
                }
                else
                {
                    //set root node for later traversal
                    rootNode = node;
                }
            }

            //traverse tree, add to the list of components
            rootNode.traverse(rootNode, (data) => { components.Add(data); });
            return components;
        }

        #endregion
    }
}