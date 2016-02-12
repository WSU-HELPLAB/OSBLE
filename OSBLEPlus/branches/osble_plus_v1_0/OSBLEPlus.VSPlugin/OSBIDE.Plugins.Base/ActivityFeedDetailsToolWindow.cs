using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

using OSBIDE.Controls.ViewModels;
using OSBIDE.Controls.Views;
using OSBLEPlus.Logic.Utility;

namespace OSBIDE.Plugins.Base
{
    [Guid("eee1c7ba-00ea-4b22-88d7-6cb17837c3d5")]
    public sealed class ActivityFeedDetailsToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public ActivityFeedDetailsToolWindow() :
            base(null)
        {
            // Set the window title reading it from the resources.
            Caption = "Feed Details";
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            BitmapResourceID = 301;
            BitmapIndex = 1;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            var view = new BrowserView();
            view.BrowserViewModelChanged += view_BrowserViewModelChanged;
            var cache = Cache.CacheInstance;
            string url;
            try
            {
                url = cache[VsComponent.FeedDetails.ToString()].ToString();
            }
            catch (Exception)
            {
                url = string.Empty;
            }
            string authKey;
            try
            {
                authKey = cache[StringConstants.AuthenticationCacheKey].ToString();
            }
            catch (Exception)
            {
                authKey = string.Empty;
            }
            view.ViewModel = new BrowserViewModel()
            {
                Url = url,
                AuthKey = authKey
            };
            view.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            Content = view;
        }

        private static void view_BrowserViewModelChanged(object sender, BrowserViewModelChangedEventArgs e)
        {
            if (e.OldModel != null)
            {
                e.OldModel.PropertyChanged -= ViewModel_PropertyChanged;
            }
            if (e.NewModel != null)
            {
                e.NewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private static void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var vm = sender as BrowserViewModel;
            if (vm != null)
            {
                //vm.Url
            }
        }
    }
}
