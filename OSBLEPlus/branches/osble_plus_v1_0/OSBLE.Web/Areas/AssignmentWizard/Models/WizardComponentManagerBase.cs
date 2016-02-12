using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Caching;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.IO;
using OSBLE.Models.Users;
using OSBLE.Utility;
using FileCacheHelper = OSBLEPlus.Logic.Utility.FileCacheHelper;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    [Serializable]
    public abstract class WizardComponentManagerBase
    {
        protected const string activeComponentIndexKey = "ComponentManagerActiveComponentIndex";
        protected const string instance = "_wcm_instance";
        protected const string componentsCacheString = "sortedComponents";
        protected const string managerCookieString = "_wcm_managerCookie";
        protected const string allComponentsKey = "_wcm_allComponents";
        protected const string selectedComponentsKey = "_wcm_selectedComponents";
        protected const string unselectedComponentsKey = "_wcm_unselectedComponents";
        protected const string cacheIdKey = "_wcm_cacheIdString";

        protected abstract string CacheRegion { get; }
        protected abstract string WizardComponentNamespace { get; }

        [NonSerialized]
        protected FileCache ManagerCache;

        [NonSerialized]
        private ObservableCollection<IWizardBaseController> _selectedComponents = new ObservableCollection<IWizardBaseController>();

        [NonSerialized]
        private ObservableCollection<IWizardBaseController> _unselectedComponents = new ObservableCollection<IWizardBaseController>();

        #region constructor

        public WizardComponentManagerBase(UserProfile profile)
        {
            ManagerCache = FileCacheHelper.GetCacheInstance(profile);
            ManagerCache.DefaultRegion = Path.Combine(ManagerCache.DefaultRegion, CacheRegion);
            AllComponents = new List<IWizardBaseController>();
            SelectedComponents = new ObservableCollection<IWizardBaseController>();
            UnselectedComponents = new ObservableCollection<IWizardBaseController>();
            RegisterComponents();
            if (ManagerCache[selectedComponentsKey] != null)
            {
                SelectedComponents = new ObservableCollection<IWizardBaseController>(ComponentsFromString(ManagerCache[selectedComponentsKey].ToString(), "|"));
            }
            if (ManagerCache[unselectedComponentsKey] != null)
            {
                UnselectedComponents = new ObservableCollection<IWizardBaseController>(ComponentsFromString(ManagerCache[unselectedComponentsKey].ToString(), "|"));
            }


        }

        #endregion

        #region properties

        public List<IWizardBaseController> AllComponents { get; private set; }

        /// <summary>
        /// A list of the currently selected wizard components.  DO NOT directly modify this list.
        /// </summary>
        public ObservableCollection<IWizardBaseController> SelectedComponents
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
        public ObservableCollection<IWizardBaseController> UnselectedComponents
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

                ManagerCache[selectedComponentsKey] = selectedComponentPieces;
                ManagerCache[unselectedComponentsKey] = unselectedComponentsPieces;
            }
        }

        
        private int ActiveComponentIndex
        {
            get
            {
                if (ManagerCache[activeComponentIndexKey] != null)
                {
                    return Convert.ToInt32(ManagerCache[activeComponentIndexKey]);
                }
                else
                {
                    ManagerCache[activeComponentIndexKey] = "0";
                    return 0;
                }
            }
            set
            {
                if (HttpContext.Current != null)
                {
                    ManagerCache[activeComponentIndexKey] = value.ToString();
                }
            }
        }

        public IWizardBaseController ActiveComponent
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
                List<IWizardBaseController> selectedAsList = SelectedComponents.ToList();
                selectedAsList.Sort(new WizardSortOrderComparer());
                SelectedComponents = new ObservableCollection<IWizardBaseController>(selectedAsList);

                List<IWizardBaseController> unselectedAsList = UnselectedComponents.ToList();
                unselectedAsList.Sort(new WizardSortOrderComparer());
                UnselectedComponents = new ObservableCollection<IWizardBaseController>(unselectedAsList);
            }
            else
            {
                List<IWizardBaseController> selectedAsList = SortComponents(SelectedComponents.ToList());
                SelectedComponents = new ObservableCollection<IWizardBaseController>(selectedAsList);

                List<IWizardBaseController> unselectedAsList = SortComponents(UnselectedComponents.ToList());
                UnselectedComponents = new ObservableCollection<IWizardBaseController>(unselectedAsList);
            }

            //The collectionChanged event won't get called to save our sort, so we have to do it manually
            ComponentsCollectionChanged(this, new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }

        public IWizardBaseController GetNextComponent()
        {
            if (ActiveComponentIndex < SelectedComponents.Count - 1)
            {
                ActiveComponentIndex++;
                return ActiveComponent;
            }
            return null;
        }

        public IWizardBaseController GetPreviousComponent()
        {
            if (ActiveComponentIndex > 0)
            {
                ActiveComponentIndex--;
                return ActiveComponent;
            }
            return null;
        }


        public IWizardBaseController GetComponentByName(string name)
        {
            IWizardBaseController component = (from c in AllComponents
                                              where c.ControllerName.CompareTo(name) == 0
                                              select c).FirstOrDefault();
            return component;
        }

        public IWizardBaseController GetComponentByType(Type type)
        {
            IWizardBaseController component = (from c in AllComponents
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
            foreach (IWizardBaseController component in AllComponents)
            {
                component.IsSelected = false;
            }
            ActiveComponentIndex = 0;
            SelectedComponents.Clear();
        }


        #endregion

        #region private helpers

        /// <summary>
        /// Converts a delimited string into a list of <see cref="IWizardBaseController"/> objects.
        /// </summary>
        /// <param name="itemString"></param>
        /// <param name="delimiter">The token used to delimit each entry</param>
        /// <returns></returns>
        private List<IWizardBaseController> ComponentsFromString(string itemString, string delimiter)
        {
            List<IWizardBaseController> components = new List<IWizardBaseController>();
            string[] items = itemString.Split(delimiter.ToCharArray());
            foreach (string item in items)
            {
                IWizardBaseController component = AllComponents.Find(c => c.ControllerName == item);
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
                IWizardBaseController component = sender as IWizardBaseController;
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
            string componentNamespace = WizardComponentNamespace;
            List<Type> componentObjects = (from type in Assembly.GetExecutingAssembly().GetTypes()
                                           where
                                           type.GetInterfaces().Contains(typeof(IWizardBaseController))
                                           &&
                                           !type.IsAbstract
                                           &&
                                           type.Namespace.CompareTo(componentNamespace) == 0
                                           select type).ToList();

            foreach (Type component in componentObjects)
            {
                IWizardBaseController controller = Activator.CreateInstance(component) as IWizardBaseController;
                AllComponents.Add(controller);
            }

            //pull from the cache if possible
            FileCache cache = FileCacheHelper.GetGlobalCacheInstance();
            bool loadedFromCache = false;
            string[] sorted = (string[])cache.Get(componentsCacheString, CacheRegion);
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
                foreach (IWizardBaseController component in AllComponents)
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
                    IWizardBaseController[] tempComponentList = new IWizardBaseController[sorted.Length];
                    for (int i = 0; i < sorted.Length; i++)
                    {
                        IWizardBaseController component = AllComponents.Find(c => c.ControllerName == sorted[i]);
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
                List<IWizardBaseController> components = SortComponents(AllComponents);
                AllComponents = components;

                //save sorted component information to cache
                string[] sortedComponents = new string[AllComponents.Count];
                for (int i = 0; i < sortedComponents.Length; i++)
                {
                    sortedComponents[i] = AllComponents[i].ControllerName;
                }
                cache.Add(componentsCacheString, sortedComponents, cache.DefaultPolicy, CacheRegion);
            }

            //attach event listeners to selection change
            //and apply a sort order for faster sorting in the future
            int counter = 1;
            foreach (IWizardBaseController component in AllComponents)
            {
                component.SortOrder = counter;
                component.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(ComponentPropertyChanged);
                counter++;
            }
        }

        private List<IWizardBaseController> SortComponents(List<IWizardBaseController> unsorted)
        {
            List<IWizardBaseController> components = new List<IWizardBaseController>();
            Dictionary<string, NTree<IWizardBaseController>> nodes = new Dictionary<string, NTree<IWizardBaseController>>();
            NTree<IWizardBaseController> rootNode = null;

            //build the tree
            foreach (IWizardBaseController component in unsorted)
            {

                NTree<IWizardBaseController> node = null;
                if (!nodes.ContainsKey(component.ControllerName))
                {
                    node = new NTree<IWizardBaseController>(component);
                    nodes.Add(component.ControllerName, node);
                }
                else
                {
                    node = nodes[component.ControllerName];
                }

                //check for null prereq before we contine.  Null prereq denotes root node
                if (component.Prerequisite != null)
                {
                    NTree<IWizardBaseController> parentNode = null;
                    if (nodes.ContainsKey(component.Prerequisite.ControllerName))
                    {
                        parentNode = nodes[component.Prerequisite.ControllerName];
                    }
                    else
                    {
                        parentNode = new NTree<IWizardBaseController>(component.Prerequisite as IWizardBaseController);
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