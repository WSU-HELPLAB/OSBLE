using System;
using System.Runtime.Caching;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using OSBIDE.Controls;
using OSBIDE.Controls.ViewModels;
using OSBIDE.Controls.Views;
using OSBLEPlus.Logic.Utility;

using EnvDTE;
using OSBLEPlus.Logic.Utility.Logging;

namespace OSBIDE.Plugins.Base
{
    public class OsbideToolWindowManager
    {
        private readonly OsbideResourceInterceptor _interceptor = OsbideResourceInterceptor.Instance;
        private readonly FileCache _cache;
        private readonly Package _vsPackage;
        private readonly BrowserViewModel _chatVm = new BrowserViewModel();
        private readonly BrowserViewModel _profileVm = new BrowserViewModel();
        private readonly BrowserViewModel _communityVm = new BrowserViewModel();
        private readonly BrowserViewModel _activityFeedVm = new BrowserViewModel();
        private readonly BrowserViewModel _activityFeedDetailsVm = new BrowserViewModel();
        private readonly BrowserViewModel _createAccountVm = new BrowserViewModel();
        private readonly BrowserViewModel _askTheProfessorVm = new BrowserViewModel();
        private readonly BrowserViewModel _genericWindowVm = new BrowserViewModel();
        private readonly BrowserViewModel _interventionVm = new BrowserViewModel();
        private static int _detailsToolWindowId;
        private readonly ILogger _logger;

        public OsbideToolWindowManager(FileCache cache, Package vsPackage)
        {
            _cache = cache;
            _vsPackage = vsPackage;
            _interceptor.NavigationRequested += NavigationRequested;
            _chatVm.Url = StringConstants.ChatUrl;
            _profileVm.Url = StringConstants.ProfileUrl;
            _communityVm.Url = StringConstants.CommunityUrl;
            _activityFeedVm.Url = StringConstants.ActivityFeedUrl;
            _activityFeedDetailsVm.Url = StringConstants.ActivityFeedUrl;
            _createAccountVm.Url = StringConstants.CreateAccountUrl;
            _askTheProfessorVm.Url = StringConstants.AskTheProfessorUrl;
            _genericWindowVm.Url = StringConstants.ProfileUrl;
            _interventionVm.Url = StringConstants.InterventionUrl;
        }

        public void OpenChatWindow(Package vsPackage = null)
        {
            _chatVm.AuthKey = _cache[StringConstants.AuthenticationCacheKey] as string;
            OpenToolWindow(new ChatToolWindow(), _chatVm, vsPackage);
        }

        public void OpenProfileWindow(Package vsPackage = null)
        {
            _profileVm.AuthKey = _cache[StringConstants.AuthenticationCacheKey] as string;
            OpenToolWindow(new UserProfileToolWindow(), _profileVm, vsPackage);
        }

        public void OpenCommunityWindow(Package vsPackage = null)
        {
            _communityVm.AuthKey = _cache[StringConstants.AuthenticationCacheKey] as string;
            OpenToolWindow(new CommunityToolWindow(), _communityVm, vsPackage);
        }

        public void OpenInterventionWindow(Package vsPackage = null, string caption = "", string customUrl = "")
        {
            //need to get the current document the user is focusing on.
            DTE dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            var currentDocument = dte.ActiveDocument;
            var currentWindow = dte.ActiveWindow;

            //open the intervention window
            _interventionVm.AuthKey = _cache[StringConstants.AuthenticationCacheKey] as string;

            if (customUrl != "")
                _interventionVm.Url = customUrl;
            else
                _interventionVm.Url = string.Format("{0}/Intervention", StringConstants.WebClientRoot);
            try
            {
                OpenToolWindow(new InterventionWindow(customUrl), _interventionVm, vsPackage);
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("OpenToolWindow(): method: {0}: error: {1}", "OpenInterventionWindow", ex.Message), LogPriority.HighPriority);
            }            

            //make sure the window pops up as expected, make sure it's the osble intervention window
            if (dte.ActiveWindow != null && dte.ActiveWindow.Caption.Contains("OSBLE+ Suggestions"))
            {
                try
                {
                    dte.ActiveWindow.AutoHides = false; //make sure the window will appear
                }
                catch (Exception ex)
                {
                    _logger.WriteToLog(string.Format("dte.ActiveWindow.AutoHides: method: {0}: error: {1}", "OpenInterventionWindow", ex.Message), LogPriority.HighPriority);
                }

                try
                {
                    //check for updates and change the title bar caption                
                    if (caption != "")
                    {
                        dte.ActiveWindow.Caption = "OSBLE+ Suggestions (" + caption + ")";
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteToLog(string.Format("dte.ActiveWindow.Caption: method: {0}: error: {1}", "OpenInterventionWindow", ex.Message), LogPriority.HighPriority);
                }
            }
            try
            {
                //make sure the user focus is not stripped away from them... put focus back on the document they were on.
                if (currentDocument != null) // only activate if there is a current document.
                    currentDocument.Activate();
                else
                    currentWindow.Activate();
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("dcurrentDocument/Window.Activate();: method: {0}: error: {1}", "OpenInterventionWindow", ex.Message), LogPriority.HighPriority);
            }            
        }

        public void CloseInterventionWindow(Package vsPackage = null)
        {
            CloseToolWindow(new InterventionWindow(), vsPackage);
        }

        public void CloseProfileWindow(Package vsPackage = null)
        {
            CloseToolWindow(new UserProfileToolWindow(), vsPackage);
        }

        public void CloseCommunityWindow(Package vsPackage = null)
        {
            CloseToolWindow(new CommunityToolWindow(), vsPackage);
        }

        public void OpenGenericToolWindow(Package vsPackage = null, string url = "")
        {
            _genericWindowVm.AuthKey = _cache[StringConstants.AuthenticationCacheKey] as string;
            if (string.IsNullOrEmpty(url) == false)
            {
                _genericWindowVm.Url = url;
            }
            OpenToolWindow(new GenericOsbideToolWindow(), _genericWindowVm, vsPackage);
        }

        public void CloseGenericToolWindow(Package vsPackage = null)
        {
            CloseToolWindow(new GenericOsbideToolWindow(), vsPackage);
        }

        public void OpenActivityFeedDetailsWindow(Package vsPackage = null)
        {
            _activityFeedDetailsVm.AuthKey = _cache[StringConstants.AuthenticationCacheKey] as string;
            OpenToolWindow(new ActivityFeedDetailsToolWindow(), _activityFeedDetailsVm, vsPackage, _detailsToolWindowId);

            //ensures that we get a new tool window with each click
            _detailsToolWindowId++;
        }

        public void CloseActivityFeedDetailsWindow(Package vsPackage = null)
        {
            CloseToolWindow(new ActivityFeedDetailsToolWindow(), vsPackage);
        }

        public void OpenActivityFeedWindow(Package vsPackage = null, string url = "")
        {
            _activityFeedVm.AuthKey = _cache[StringConstants.AuthenticationCacheKey] as string;
            if (string.IsNullOrEmpty(url) == false)
            {
                _activityFeedVm.Url = url;
            }
            OpenToolWindow(new ActivityFeedToolWindow(), _activityFeedVm, vsPackage);
        }

        public void RedirectActivityFeedWindow(string url = "")
        {
            if (string.IsNullOrEmpty(url) == false)
            {
                _activityFeedVm.Url = url;
            }
        }

        public void CloseActivityFeedWindow(Package vsPackage = null)
        {
            CloseToolWindow(new ActivityFeedToolWindow(), vsPackage);
        }

        public void OpenCreateAccountWindow(Package vsPackage = null)
        {
            _createAccountVm.AuthKey = string.Empty;
            OpenToolWindow(new CreateAccountToolWindow(), _createAccountVm, vsPackage);
        }

        public void CloseCreateAccountWindow(Package vsPackage = null)
        {
            CloseToolWindow(new CreateAccountToolWindow(), vsPackage);
        }

        public void OpenAskTheProfessorWindow(Package vsPackage = null)
        {
            _askTheProfessorVm.AuthKey = _cache[StringConstants.AuthenticationCacheKey] as string;
            OpenToolWindow(new AskTheProfessorToolWindow(), _askTheProfessorVm, vsPackage);
        }

        public void CloseAskTheProfessorWindow(Package vsPackage = null)
        {
            CloseToolWindow(new AskTheProfessorToolWindow(), vsPackage);
        }

        private void OpenToolWindow(ToolWindowPane pane, BrowserViewModel vm, Package vsPackage, int toolId = 0)
        {
            if (vsPackage == null)
            {
                vsPackage = _vsPackage;
            }
            var window = vsPackage.FindToolWindow(pane.GetType(), toolId, true);
            
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("oops!");
            }

            ((BrowserView)window.Content).Dispatcher.BeginInvoke(
                (Action)delegate
                {
                    ((BrowserView)window.Content).ViewModel = vm;
                }
                );
            
            //for some reason the above code does not properly update URL if it's been updated in the vm we pass in...
            ((BrowserView)window.Content).ViewModel.Url = vm.Url;

            var windowFrame = (IVsWindowFrame)window.Frame;
            
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private void CloseToolWindow(ToolWindowPane pane, Package vsPackage, int toolID = 0)
        {
            if (vsPackage == null)
            {
                vsPackage = _vsPackage;
            }
            var window = vsPackage.FindToolWindow(pane.GetType(), toolID, true);
            if ((null == window) || (null == window.Frame))
            {
                // window already closed
                return;
            }

            var windowFrame = (IVsWindowFrame) window.Frame;
            windowFrame.CloseFrame((uint)__FRAMECLOSE.FRAMECLOSE_NoSave);
        }

        private void NavigationRequested(object sender, OsbideResourceInterceptor.ResourceInterceptorEventArgs e)
        {
            var provider = (_vsPackage as IServiceProvider);
            var uiShell = provider.GetService(typeof(SVsUIShell)) as IVsUIShell;

            var commandSet = CommonGuidList.guidOSBIDE_VSPackageCmdSet;
            object inputParameters = null;
            if (uiShell == null)
            {
                return;
            }
            _cache[e.Component.ToString()] = e.Url;
            switch (e.Component)
            {
                case VsComponent.AskTheProfessor:
                    _askTheProfessorVm.Url = e.Url;
                    uiShell.PostExecCommand(ref commandSet, CommonPkgCmdIDList.cmdidOsbideAskTheProfessor, 0, ref inputParameters);
                    break;
                case VsComponent.CreateAccount:
                    _createAccountVm.Url = e.Url;
                    uiShell.PostExecCommand(ref commandSet, CommonPkgCmdIDList.cmdidOsbideCreateAccountTool, 0, ref inputParameters);
                    break;
                case VsComponent.Chat:
                    _chatVm.Url = e.Url;
                    uiShell.PostExecCommand(ref commandSet, CommonPkgCmdIDList.cmdidOsbideChatTool, 0, ref inputParameters);
                    break;
                case VsComponent.FeedDetails:
                    _activityFeedDetailsVm.Url = e.Url;
                    uiShell.PostExecCommand(ref commandSet, CommonPkgCmdIDList.cmdidOsbideActivityFeedDetailsTool, 0, ref inputParameters);
                    break;
                case VsComponent.FeedOverview:
                    _activityFeedVm.Url = e.Url;
                    uiShell.PostExecCommand(ref commandSet, CommonPkgCmdIDList.cmdidOsbideActivityFeedTool, 0, ref inputParameters);
                    break;
                case VsComponent.UserProfile:
                    _profileVm.Url = e.Url;
                    uiShell.PostExecCommand(ref commandSet, CommonPkgCmdIDList.cmdidOsbideUserProfileTool, 0, ref inputParameters);
                    break;
                case VsComponent.InterventionDetails:
                    _interventionVm.Url = e.Url;
                    uiShell.PostExecCommand(ref commandSet, CommonPkgCmdIDList.cmdidOsbideInterventionTool, 0, ref inputParameters);
                    break;
                case VsComponent.GenericComponent:
                    _genericWindowVm.Url = e.Url;
                    uiShell.PostExecCommand(ref commandSet, CommonPkgCmdIDList.cmdidOsbideGenericToolWindow, 0, ref inputParameters);
                    break;
            }
        }

        public void CloseAllWindows(Package vsPackage = null)
        {
            CloseToolWindow(new ActivityFeedDetailsToolWindow(), vsPackage);
            CloseToolWindow(new ActivityFeedToolWindow(), vsPackage);
            CloseToolWindow(new AskTheProfessorToolWindow(), vsPackage);
            CloseToolWindow(new CreateAccountToolWindow(), vsPackage);
            CloseToolWindow(new GenericOsbideToolWindow(), vsPackage);
            CloseToolWindow(new UserProfileToolWindow(), vsPackage);

            if (_cache.Contains("community") && Boolean.Equals(true, _cache["community"]))
                CloseToolWindow(new CommunityToolWindow(), vsPackage);
        }
    }
}
