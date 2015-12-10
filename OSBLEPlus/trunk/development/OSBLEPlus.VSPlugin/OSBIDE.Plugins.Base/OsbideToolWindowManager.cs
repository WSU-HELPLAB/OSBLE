using System;
using System.Runtime.Caching;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using OSBIDE.Controls;
using OSBIDE.Controls.ViewModels;
using OSBIDE.Controls.Views;
using OSBLEPlus.Logic.Utility;

namespace OSBIDE.Plugins.Base
{
    public class OsbideToolWindowManager
    {
        private readonly OsbideResourceInterceptor _interceptor = OsbideResourceInterceptor.Instance;
        private readonly FileCache _cache;
        private readonly Package _vsPackage;
        private readonly BrowserViewModel _chatVm = new BrowserViewModel();
        private readonly BrowserViewModel _profileVm = new BrowserViewModel();
        private readonly BrowserViewModel _activityFeedVm = new BrowserViewModel();
        private readonly BrowserViewModel _activityFeedDetailsVm = new BrowserViewModel();
        private readonly BrowserViewModel _createAccountVm = new BrowserViewModel();
        private readonly BrowserViewModel _askTheProfessorVm = new BrowserViewModel();
        private readonly BrowserViewModel _genericWindowVm = new BrowserViewModel();
        private static int _detailsToolWindowId;

        public OsbideToolWindowManager(FileCache cache, Package vsPackage)
        {
            _cache = cache;
            _vsPackage = vsPackage;
            _interceptor.NavigationRequested += NavigationRequested;
            _chatVm.Url = StringConstants.ChatUrl;
            _profileVm.Url = StringConstants.ProfileUrl;
            _activityFeedVm.Url = StringConstants.ActivityFeedUrl;
            _activityFeedDetailsVm.Url = StringConstants.ActivityFeedUrl;
            _createAccountVm.Url = StringConstants.CreateAccountUrl;
            _askTheProfessorVm.Url = StringConstants.AskTheProfessorUrl;
            _genericWindowVm.Url = StringConstants.ProfileUrl;
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

        public void CloseProfileWindow(Package vsPackage = null)
        {
            CloseToolWindow(new UserProfileToolWindow(), vsPackage);
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
            ((BrowserView) window.Content).Dispatcher.BeginInvoke(
                (Action)delegate
                {
                    ((BrowserView) window.Content).ViewModel = vm;
                }
                );
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
                case VsComponent.GenericComponent:
                    _genericWindowVm.Url = e.Url;
                    uiShell.PostExecCommand(ref commandSet, CommonPkgCmdIDList.cmdidOsbideGenericToolWindow, 0, ref inputParameters);
                    break;
            }
        }
    }
}
