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
using System.Runtime.Serialization;
using System.IO;
using OSBLE.Models.Users;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    //TODO: Properly evaluate the change to FileCache (see note below)
    //AC: We were having issues with the selected assignment type randomly defaulting to "Basic."  Previously,
    //    I had been using JS cookies to track component settings.  I think that this might have been part of
    //    the problem so I converted the WCM over fo the FileCache.  However, the conversion was mostly a
    //    replacement of Cookie[value] to Cache[value].  This might need to be revisited in a future date.
    [Serializable]
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
        private const string cacheIdKey = "_wcm_cacheIdString";

        [NonSerialized]
        private FileCache managerCache;

        [NonSerialized]
        private ObservableCollection<WizardBaseController> _selectedComponents = new ObservableCollection<WizardBaseController>();

        [NonSerialized]
        private ObservableCollection<WizardBaseController> _unselectedComponents = new ObservableCollection<WizardBaseController>();

        #region constructor

        public WizardComponentManager(UserProfile profile)
        {
            managerCache = FileCacheHelper.GetCacheInstance(profile);
            managerCache.DefaultRegion = Path.Combine(managerCache.DefaultRegion, "assignmentWizard");
            AllComponents = new List<WizardBaseController>();
            SelectedComponents = new ObservableCollection<WizardBaseController>();
            UnselectedComponents = new ObservableCollection<WizardBaseController>();
            RegisterComponents();
            if (managerCache[selectedComponentsKey] != null)
            {
                SelectedComponents = new ObservableCollection<WizardBaseController>(ComponentsFromString(managerCache[selectedComponentsKey].ToString(), "|"));
            }
            if (managerCache[unselectedComponentsKey] != null)
            {
                UnselectedComponents = new ObservableCollection<WizardBaseController>(ComponentsFromString(managerCache[unselectedComponentsKey].ToString(), "|"));
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

                managerCache[selectedComponentsKey] = selectedComponentPieces;
                managerCache[unselectedComponentsKey] = unselectedComponentsPieces;
            }
        }

        public bool IsNewAssignment
        {
            get
            {
                if (managerCache[isNewAssignmentKey] != null)
                {
                    return Convert.ToBoolean(managerCache[isNewAssignmentKey]);
                }
                else
                {
                    managerCache[isNewAssignmentKey] = true.ToString();
                    return true;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    managerCache[isNewAssignmentKey] = value.ToString();
                }
            }
        }

        public int ActiveAssignmentId
        {
            get
            {
                if (managerCache[assignmentKey] != null)
                {
                    return Convert.ToInt32(managerCache[assignmentKey]);
                }
                else
                {
                    managerCache[assignmentKey] = "0";
                    return 0;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    managerCache[assignmentKey] = value.ToString();
                }
            }
        }

        public AssignmentTypes ActiveAssignmentType
        {
            get
            {
                if (managerCache[activeAssignmentTypeKey] != null)
                {
                    return (AssignmentTypes)Convert.ToInt32(managerCache[activeAssignmentTypeKey]);
                }
                else
                {
                    managerCache[activeAssignmentTypeKey] = ((int)AssignmentTypes.Basic).ToString();
                    return AssignmentTypes.Basic;
                }
            }
            private set
            {
                if (HttpContext.Current != null)
                {
                    managerCache[activeAssignmentTypeKey] = ((int)value).ToString();
                }
            }
        }

        private int ActiveComponentIndex
        {
            get
            {
                if (managerCache[activeComponentIndexKey] != null)
                {
                    return Convert.ToInt32(managerCache[activeComponentIndexKey]);
                }
                else
                {
                    managerCache[activeComponentIndexKey] = "0";
                    return 0;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    managerCache[activeComponentIndexKey] = value.ToString();
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

            //The collectionChanged event won't get called to save our sort, so we have to do it manually
            ComponentsCollectionChanged(this, new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
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
                if (assignmentType == type.ToString())
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
                if (component != null)
                {
                    components.Add(component);
                }
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
                                           !type.IsAbstract
                                           &&
                                           type.Namespace.CompareTo(componentNamespace) == 0
                                           select type).ToList();

            foreach (Type component in componentObjects)
            {
                WizardBaseController controller = Activator.CreateInstance(component) as WizardBaseController;
                AllComponents.Add(controller);
            }

            //pull from the cache if possible
            FileCache cache = FileCacheHelper.GetGlobalCacheInstance();
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
                        WizardBaseController component = AllComponents.Find(c => c.ControllerName == sorted[i]);
                        if (component != null)
                        {
                            tempComponentList[i] = component;
                        }
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
                cache.Add(componentsCacheString, sortedComponents, cache.DefaultPolicy, cacheRegion);
            }

            //attach event listeners to selection change
            //and apply a sort order for faster sorting in the future
            int counter = 1;
            foreach (WizardBaseController component in AllComponents)
            {
                component.SortOrder = counter;
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
                        parentNode = new NTree<WizardBaseController>(component.Prerequisite as WizardBaseController);
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