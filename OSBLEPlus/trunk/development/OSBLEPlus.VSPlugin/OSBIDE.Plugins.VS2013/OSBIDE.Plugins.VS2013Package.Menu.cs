using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;
using Process = System.Diagnostics.Process;

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.CommandBars;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using OSBIDE.Controls.ViewModels;
using OSBIDE.Controls.Views;
using OSBIDE.Library.ServiceClient.ServiceHelpers;
using OSBIDE.Plugins.Base;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility;

namespace WashingtonStateUniversity.OSBIDE_Plugins_VS2013
{
    public sealed partial class OsbidePluginsVs2013Package
    {
        private void InitializeMenuCommand()
        {
            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                //login toolbar item.
                var menuCommandId = new CommandID(CommonGuidList.guidOSBIDE_VSPackageCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideCommand);
                var menuItem = new MenuCommand(OpenLoginScreen, menuCommandId);
                mcs.AddCommand(menuItem);

                //login toolbar menu option.
                var loginMenuOption = new CommandID(CommonGuidList.guidOSBIDE_OsbideToolsMenuCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideLoginToolWin);
                var menuLoginMenuOption = new MenuCommand(OpenLoginScreen, loginMenuOption);
                mcs.AddCommand(menuLoginMenuOption);

                //activity feed
                var activityFeedId = new CommandID(CommonGuidList.guidOSBIDE_VSPackageCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideActivityFeedTool);
                var menuActivityWin = new MenuCommand(ShowActivityFeedTool, activityFeedId);
                mcs.AddCommand(menuActivityWin);

                //activity feed details
                var activityFeedDetailsId = new CommandID(CommonGuidList.guidOSBIDE_VSPackageCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideActivityFeedDetailsTool);
                var menuActivityDetailsWin = new MenuCommand(ShowActivityFeedDetailsTool, activityFeedDetailsId);
                mcs.AddCommand(menuActivityDetailsWin);

                //chat window
                var chatWindowId = new CommandID(CommonGuidList.guidOSBIDE_VSPackageCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideChatTool);
                var menuChatWin = new MenuCommand(ShowChatTool, chatWindowId);
                mcs.AddCommand(menuChatWin);

                //profile page
                var profileWindowId = new CommandID(CommonGuidList.guidOSBIDE_VSPackageCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideUserProfileTool);
                var menuProfileWin = new MenuCommand(ShowProfileTool, profileWindowId);
                mcs.AddCommand(menuProfileWin);
                
                //community page
                var communityWindowId = new CommandID(CommonGuidList.guidOSBIDE_VSPackageCmdSet, (int)CommonPkgCmdIDList.cmdidCommunityTool);
                var menuCommunityWin = new MenuCommand(ShowCommunityTool, communityWindowId);
                mcs.AddCommand(menuCommunityWin);

                //"ask for help context" menu
                var askForHelpId = new CommandID(CommonGuidList.guidOSBIDE_ContextMenuCmdSet, (int)CommonPkgCmdIDList.cmdOsbideAskForHelp);
                var askForHelpWin = new OleMenuCommand(ShowAskForHelp, askForHelpId);
                askForHelpWin.BeforeQueryStatus += AskForHelpCheckActive;
                mcs.AddCommand(askForHelpWin);

                //create account window
                var createAccountWindowId = new CommandID(CommonGuidList.guidOSBIDE_VSPackageCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideCreateAccountTool);
                var menuAccountWin = new MenuCommand(ShowCreateAccountTool, createAccountWindowId);
                mcs.AddCommand(menuAccountWin);

                //OSBIDE documentation link
                var documentationId = new CommandID(CommonGuidList.guidOSBIDE_VSPackageCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideDocumentationTool);
                var documentationWin = new MenuCommand(OpenDocumentation, documentationId);
                mcs.AddCommand(documentationWin);

                //OSBIDE web link
                var webLinkId = new CommandID(CommonGuidList.guidOSBIDE_VSPackageCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideWebLinkTool);
                var webLinkWin = new MenuCommand(OpenOsbideWebLink, webLinkId);
                mcs.AddCommand(webLinkWin);

                //generic OSBIDE window
                var genericId = new CommandID(CommonGuidList.guidOSBIDE_VSPackageCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideGenericToolWindow);
                var genericWindow = new MenuCommand(ShowGenericToolWindow, genericId);
                mcs.AddCommand(genericWindow);

                //submit assignment command
                var submitCommand = new CommandID(CommonGuidList.guidOSBIDE_OsbideToolsMenuCmdSet, (int)CommonPkgCmdIDList.cmdidOsbideSubmitAssignmentCommand);
                var submitMenuItem = new MenuCommand(SubmitAssignmentCallback, submitCommand);
                mcs.AddCommand(submitMenuItem);

                // -- Set an event listener for shell property changes
                var shellService = GetService(typeof(SVsShell)) as IVsShell;
                if (shellService != null)
                {
                    ErrorHandler.ThrowOnFailure(shellService.
                      AdviseShellPropertyChanges(this, out _eventSinkCookie));
                }
            }
        }

        private void InitOsbideErrorContextMenu()
        {
            var dte = (DTE2)GetService(typeof(SDTE));
            if (dte != null)
            {
                var cmdBars = (CommandBars)dte.CommandBars;
                var errorListBar = cmdBars[10];

                var osbideControl = errorListBar.Controls.Add(MsoControlType.msoControlButton,
                    Missing.Value,
                    Missing.Value, 1, true);
                // Set the caption of the submenuitem
                osbideControl.Caption = "View Error in OSBLE+";
                _osbideErrorListEvent = (CommandBarEvents)dte.Events.get_CommandBarEvents(osbideControl);
                _osbideErrorListEvent.Click += osbideCommandBarEvent_Click;
            }
        }

        private void osbideCommandBarEvent_Click(object commandBarControl, ref bool handled, ref bool cancelDefault)
        {
            var listItem = new ErrorListItem();
            var dte = (DTE2)GetService(typeof(SDTE));
            if (dte != null)
            {
                var selectedItems = (Array)dte.ToolWindows.ErrorList.SelectedItems;
                if (selectedItems != null)
                {
                    foreach (ErrorItem item in selectedItems)
                    {
                        listItem = TypeConverters.ErrorItemToErrorListItem(item);
                    }
                }
            }

            if (string.IsNullOrEmpty(listItem.CriticalErrorName) == false)
            {
                var url = string.Format("{0}?errorTypeStr={1}&component={2}", StringConstants.ActivityFeedUrl, listItem.CriticalErrorName, VsComponent.FeedOverview);
                OpenActivityFeedWindow(url);
            }
            else
            {
                MessageBox.Show(@"OSBLE+ only supports search for errors");
            }
        }

        private void SubmitAssignmentCallback(object sender, EventArgs e)
        {
            var evt = new SubmitEvent();
            var dte = (DTE2)GetService(typeof(SDTE));

            if (dte.Solution.FullName.Length == 0)
            {
                MessageBox.Show(@"No solution is currently open.");
                return;
            }
            var cacheItem = _cache[StringConstants.AuthenticationCacheKey];
            if (cacheItem != null && string.IsNullOrEmpty(cacheItem.ToString()) == false)
            {
                evt.SolutionName = dte.Solution.FullName;

                var vm = new SubmitAssignmentViewModel(
                    _cache[StringConstants.UserNameCacheKey] as string,
                    _cache[StringConstants.AuthenticationCacheKey] as string,
                    evt
                    );
                SubmitAssignmentWindow.ShowModalDialog(vm);
            }
            else
            {
                MessageBox.Show(@"You must be logged into OSBLE+ in order to submit an assignment.");
            }
        }

        private void OpenDocumentation(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo(StringConstants.DocumentationUrl));
        }
    }
}
