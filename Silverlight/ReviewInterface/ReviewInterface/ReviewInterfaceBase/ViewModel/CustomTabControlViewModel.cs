using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ReviewInterfaceBase.HelperClasses;
using ReviewInterfaceBase.View;

namespace ReviewInterfaceBase.ViewModel
{
    /// <summary>
    /// This is a CustomTabControl because the Silverlight TabControl unloads its children when they are not in view
    /// This is not ideal as users are expected to switch between views rapidly
    /// </summary>
    public class CustomTabControlViewModel
    {
        #region Delegates

        /// <summary>
        /// This fires whenever the tabs have switched i.e. the user click on a tab that is not currently visible
        /// </summary>
        public event SwitchedTabEventHandler SwitchedTabs = delegate { };

        #endregion Delegates

        #region Fields

        private List<ContentPresenter> contentList = new List<ContentPresenter>();
        private List<TabItem> tabItemList = new List<TabItem>();
        private CustomTabControlView thisView = new CustomTabControlView();
        private TabItem selectedTab;

        #endregion Fields

        #region Properties

        /// <summary>
        /// This gets or sets the SeletedTab i.e. the visible one
        /// </summary>
        public TabItem SelectedTab
        {
            get { return selectedTab; }
            set
            {
                if (selectedTab != null)
                {
                    selectedTab.IsSelected = false;
                    contentList[tabItemList.IndexOf(selectedTab)].Visibility = Visibility.Collapsed;
                }
                if (value != null)
                {
                    value.IsSelected = true;
                    contentList[tabItemList.IndexOf(value)].Visibility = Visibility.Visible;
                }

                TabItem oldTab = selectedTab;
                TabItem newTab = value;
                selectedTab = value;

                SwitchedTabs(this, new SwitchedTabEventArgs(oldTab, newTab));
            }
        }

        /// <summary>
        /// This gets or sets the SelectedTabIndex
        /// </summary>
        public int SelectedTabIndex
        {
            get
            {
                return tabItemList.IndexOf(SelectedTab);
            }
            set
            {
                SelectedTab = tabItemList[value];
            }
        }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// This function should only be used to get a reference to the view so it can be added a UIElement the
        /// view should never be manipulated directly.
        /// </summary>
        /// <returns>A Reference to the view that is currently being manipulated by this view model.</returns>
        public CustomTabControlView GetView()
        {
            return thisView;
        }

        /// <summary>
        /// This add the new tabItem to the TabControl
        /// </summary>
        /// <param name="tabItem">TabItem</param>
        public void AddTabItem(TabItem tabItem)
        {
            tabItemList.Add(tabItem);
            tabItem.MouseLeftButtonDown += new MouseButtonEventHandler(tabItem_MouseLeftButtonDown);
            ContentPresenter cp = new ContentPresenter() { Content = tabItem.Content };
            cp.SetValue(Grid.RowProperty, 1);
            cp.Visibility = Visibility.Collapsed;
            contentList.Add(cp);
            thisView.LayoutRoot.Children.Add(cp);
            thisView.TabPanel.Children.Add(tabItem);
        }

        /// <summary>
        /// This updates the content of the TabItem
        /// The TabItem must have already been added to the TabControl
        /// </summary>
        /// <param name="tabItem">A reference to the TabItem that will have its content updated</param>
        /// <param name="content">A reference to the content that the tabItem will have</param>
        public void UpdateTabItemContent(TabItem tabItem, UIElement content)
        {
            int index = (tabItemList.IndexOf(tabItem));
            thisView.LayoutRoot.Children.Remove(contentList[index]);
            ContentPresenter cp = new ContentPresenter() { Content = content };
            cp.SetValue(Grid.RowProperty, 1);
            cp.Visibility = contentList[index].Visibility;
            contentList[index] = cp;
            thisView.LayoutRoot.Children.Add(cp);
        }

        /// <summary>
        /// This removes a tab from the TabControl
        /// </summary>
        /// <param name="tabItem">The item to be removed</param>
        public void RemoveTabItem(TabItem tabItem)
        {
            if (tabItem == selectedTab)
            {
                selectedTab = null;
            }
            int index = (tabItemList.IndexOf(tabItem));
            thisView.LayoutRoot.Children.Remove(contentList[index]);
            thisView.TabPanel.Children.Remove(tabItem);

            tabItemList.RemoveAt(index);
            contentList.RemoveAt(index);
        }

        #endregion Public Methods

        #region Private EventHandlers

        /// <summary>
        /// This gets a mouseClick on a tabHeader
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private void tabItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            SelectedTab = sender as TabItem;
        }

        #endregion Private EventHandlers
    }
}