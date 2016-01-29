using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using OSBIDE.Controls.ViewModels;
using OSBIDE.Controls.Views;
using OSBIDE.Library.ServiceClient.ServiceHelpers;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Logging;

namespace WashingtonStateUniversity.OSBIDE_Plugins_VS2013
{
    public sealed partial class OsbidePluginsVs2013Package
    {
        private void ShowGenericToolWindow(object sender, EventArgs e)
        {
            OpenGenericToolWindow();
        }

        private void OpenGenericToolWindow(string url = "")
        {
            _manager.OpenGenericToolWindow(null, url);
        }

        private void OpenOsbideWebLink(object sender, EventArgs e)
        {
            var authKey = _cache[StringConstants.AuthenticationCacheKey] as string;
            var url = string.Format("{0}/Account/TokenLogin?authToken={1}", StringConstants.WebClientRoot, authKey);

            Process.Start(new ProcessStartInfo(url));
        }

        private void OpenActivityFeedWindow(string url = "")
        {
            var cacheItem = _cache[StringConstants.AuthenticationCacheKey];
            if (cacheItem != null && string.IsNullOrEmpty(cacheItem.ToString()) == false)
            {
                try
                {
                    _manager.OpenActivityFeedWindow(null, StringConstants.WebClientRoot + "/feed/osbide/");
                }
                catch (Exception ex)
                {
                    ShowAwesomiumError(ex);
                }
            }
            else
            {
                MessageBox.Show("You must be logged into OSBIDE in order to access this window.");
            }
        }

        private void ShowActivityFeedTool(object sender, EventArgs e)
        {
            OpenActivityFeedWindow();
        }

        private void ShowActivityFeedDetailsTool(object sender, EventArgs e)
        {
            var cacheItem = _cache[StringConstants.AuthenticationCacheKey];
            if (cacheItem != null && string.IsNullOrEmpty(cacheItem.ToString()) == false)
            {
                try
                {
                    _manager.OpenActivityFeedDetailsWindow();
                }
                catch (Exception ex)
                {
                    ShowAwesomiumError(ex);
                }
            }
            else
            {
                MessageBox.Show("You must be logged into OSBIDE in order to access this window.");
            }
        }

        private void ShowChatTool(object sender, EventArgs e)
        {
            var cacheItem = _cache[StringConstants.AuthenticationCacheKey];
            if (cacheItem != null && string.IsNullOrEmpty(cacheItem.ToString()) == false)
            {
                try
                {
                    _manager.OpenChatWindow();
                }
                catch (Exception ex)
                {
                    ShowAwesomiumError(ex);
                }
            }
            else
            {
                MessageBox.Show("You must be logged into OSBIDE in order to access this window.");
            }
        }

        private void ShowProfileTool(object sender, EventArgs e)
        {
            var cacheItem = _cache[StringConstants.AuthenticationCacheKey];
            if (cacheItem != null && string.IsNullOrEmpty(cacheItem.ToString()) == false)
            {
                try
                {
                    _manager.OpenProfileWindow();
                    ToggleProfileImage(false);
                }
                catch (Exception ex)
                {
                    ShowAwesomiumError(ex);
                }
            }
            else
            {
                MessageBox.Show("You must be logged into OSBIDE in order to access this window.");
            }
        }

        private void ShowCreateAccountTool(object sender, EventArgs e)
        {
            try
            {
                _manager.OpenCreateAccountWindow();
            }
            catch (Exception ex)
            {
                ShowAwesomiumError(ex);
            }
        }

        public void ShowAskForHelp(object sender, EventArgs e)
        {
            var cacheItem = _cache[StringConstants.AuthenticationCacheKey];
            if (cacheItem != null && string.IsNullOrEmpty(cacheItem.ToString()))
            {
                MessageBox.Show("You must be logged into OSBIDE in order to access this window.");
                return;
            }

            var vm = new AskForHelpViewModel();

            //find current text selection
            var dte = (DTE2)GetService(typeof(SDTE));
            if (dte != null)
            {
                dynamic selection = dte.ActiveDocument.Selection;
                if (selection != null)
                {
                    try
                    {
                        vm.Code = selection.Text;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }

            //AC: Restrict "ask for help" to approx 20 lines
            if (vm.Code.Length > 750)
            {
                vm.Code = vm.Code.Substring(0, 750);
            }

            //show message dialog
            var result = AskForHelpForm.ShowModalDialog(vm);
            if (result == MessageBoxResult.OK)
            {
                var generator = EventGenerator.GetInstance();
                var evt = new AskForHelpEvent
                {
                    Code = vm.Code,
                    UserComment = vm.UserText
                };
                generator.SubmitEvent(evt);

                MessageBox.Show("Your question has been logged and will show up in the activity stream shortly.");
            }
        }

        private void AskForHelpCheckActive(object sender, EventArgs e)
        {
            var cmd = (OleMenuCommand)sender;
            var dte = (DTE2)GetService(typeof(SDTE));
            if (dte == null || dte.ActiveDocument == null) return;

            var selection = dte.ActiveDocument.Selection as TextSelection;
            if (selection == null) return;

            cmd.Enabled = !string.IsNullOrEmpty(selection.Text);
        }

        private void OpenLoginScreen(object sender, EventArgs e)
        {
            var vm = new OsbleLoginViewModel();
            vm.RequestCreateAccount += ShowCreateAccountTool;

            //attempt to store previously cached values if possible
            vm.Password = _userPassword;
            vm.Email = _userName;
            vm.IsLoggedIn = _client.IsSendingData;

            var result = OsbideLoginControl.ShowModalDialog(vm);

            //assume that data was changed and needs to be saved
            if (result == MessageBoxResult.OK)
            {
                try
                {
                    _cache[StringConstants.UserNameCacheKey] = vm.Email;
                    _userName = vm.Email;
                    _userPassword = vm.Password;
                    _cache[StringConstants.PasswordCacheKey] = AesEncryption.EncryptStringToBytes_Aes(vm.Password, _encoder.Key, _encoder.IV);
                    _cache[StringConstants.AuthenticationCacheKey] = vm.AuthenticationHash;
                }
                catch (Exception ex)
                {
                    //write to the log file
                    _errorLogger.WriteToLog(string.Format("SaveUser error: {0}", ex.Message), LogPriority.HighPriority);

                    //turn off client sending if we run into an error
                    if (_client != null)
                    {
                        _client.StopSending();
                    }
                }

                //If we got back a valid user, turn on log saving
                if (_userName != null && _userPassword != null)
                {
                    //turn on client sending
                    if (_client != null)
                    {
                        _client.IsCollectingData = true;
                        _client.StartSending();
                    }
                    MessageBox.Show("Welcome to OSBLE!");
                    _manager.OpenActivityFeedWindow();
                }
                else
                {
                    //turn off client sending if the user didn't log in.
                    if (_client != null)
                    {
                        _client.StopSending();
                    }
                }
            }
            else if (result == MessageBoxResult.No)
            {
                //In this case, I'm using MessageBoxResult.No to represent a log out request.  We can
                //fake that by just turning off client collection and sending.
                _client.IsCollectingData = false;
                _client.StopSending();
                _cache.Remove(StringConstants.AuthenticationCacheKey);
                //We need to clear the awesomium caches...?
                _manager.OpenActivityFeedWindow(null, StringConstants.WebClientRoot + "/Account/LogOff");
                _manager.CloseActivityFeedWindow();
                //redirect the feed url after we've logged out
                _manager.RedirectActivityFeedWindow(StringConstants.WebClientRoot + "/feed/osbide/");
                _manager.CloseProfileWindow();                

                MessageBox.Show("You have been logged out of OSBLE.");
            }
        }
    }
}
