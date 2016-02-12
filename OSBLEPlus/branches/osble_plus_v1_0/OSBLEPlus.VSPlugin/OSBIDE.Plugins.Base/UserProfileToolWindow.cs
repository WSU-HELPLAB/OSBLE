﻿using System;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell;

using OSBIDE.Controls.ViewModels;
using OSBIDE.Controls.Views;
using OSBLEPlus.Logic.Utility;

namespace OSBIDE.Plugins.Base
{
    [Guid("eee1c7ba-00ea-4b22-88d7-6cb17837c3d7")]
    public sealed class UserProfileToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public UserProfileToolWindow() :
            base(null)
        {
            // Set the window title reading it from the resources.
            Caption = "User Profile";
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
            var cache = Cache.CacheInstance;
            string url;
            try
            {
                url = cache[VsComponent.UserProfile.ToString()].ToString();
            }
            catch (Exception)
            {
                url = StringConstants.ProfileUrl;
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
            Content = view;
        }
    }
}
