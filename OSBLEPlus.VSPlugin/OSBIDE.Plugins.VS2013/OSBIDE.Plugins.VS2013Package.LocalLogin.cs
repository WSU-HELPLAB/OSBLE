using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;

using OSBIDE.Library.ServiceClient.ServiceHelpers;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Logging;

namespace WashingtonStateUniversity.OSBIDE_Plugins_VS2013
{
    public sealed partial class OsbidePluginsVs2013Package
    {
        private void InitAndEncryptLocalLogin()
        {
            if (_cache.Contains(StringConstants.AesKeyCacheKey) == false)
            {
                _encoder.GenerateKey();
                _encoder.GenerateIV();
                _cache[StringConstants.AesKeyCacheKey] = _encoder.Key;
                _cache[StringConstants.AesVectorCacheKey] = _encoder.IV;
            }
            else
            {
                _encoder.Key = (byte[])_cache[StringConstants.AesKeyCacheKey];
                _encoder.IV = (byte[])_cache[StringConstants.AesVectorCacheKey];
            }

            //set up user name and password
            _userName = _cache[StringConstants.UserNameCacheKey] as string;
            var passwordBytes = _cache[StringConstants.PasswordCacheKey] as byte[];
            try
            {
                _userPassword = AesEncryption.DecryptStringFromBytes_Aes(passwordBytes, _encoder.Key, _encoder.IV);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private async void Login(string userName, string password)
        {
            try
            {
                var task = AsyncServiceClient.Login(userName, password);
                var result = await task;

                //setup intervention window cache
                bool showInterventionWindow = false;
                try //we don't want this to cause any issue if it fails...
                {
                    var settingsTask = AsyncServiceClient.GetUserInterventionSettings(result); //result is the authkey
                    var settingsResult = await settingsTask;
                    showInterventionWindow = settingsResult.ShowInIDE;
                    //setup cache for intervention settings
                    _cache["ShowSuggestionsWindow"] = showInterventionWindow.ToString();
                    //refresh threshold                    
                    _cache["InterventionRefreshThresholdInMinutes"] = settingsResult.RefreshThreshold;
                    //set last refresh time as now since we will open the window now if they are seeing interventions
                    _cache["LastRefreshTime"] = DateTime.Now.ToString();
                }
                catch (Exception e)
                {
                    _errorLogger.WriteToLog(string.Format("Open Suggestions window error: {0}", e.Message), LogPriority.HighPriority);
                }

                Init_BrowserLogin(result, showInterventionWindow);
                InitStepTwo_LoginCompleted(result);
            }
            catch (Exception e)
            {
                try
                {
                    //this may crash visual studio, we'll catch it just in case...
                    _manager.CloseAllWindows();
                    var result = MessageBox.Show("The OSBLE+ Visual Studio Plugin is unable to connect to the OSBLE+ server.  If this issue persists, please contact support@osble.org with the error message.\n\nError: " + e.InnerException.ToString(), "Log Into OSBLE+", MessageBoxButton.OK);
                }
                catch (Exception ex)
                {
                    ShowAwesomiumError(ex); // if this crashes it must be missing awesomium
                }
            }
        }

        private void Init_BrowserLogin(string authKey, bool showInterventionWindow = false) //initialize browser authentication/activecourseuser
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                var result = MessageBox.Show("It appears as though your OSBLE+ user name or password has changed since the last time you opened Visual Studio.  Would you like to log back into OSBLE+?", "Log Into OSBLE+", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    OpenLoginScreen(this, EventArgs.Empty);
                }
            }
            else
            {
                try
                {
                    _manager.OpenActivityFeedWindow(null,
                                            StringConstants.WebClientRoot + "/Account/TokenLogin?authToken=" + authKey +
                                            "&destinationUrl=" + StringConstants.WebClientRoot + "/feed/osbide/");
                }
                catch (Exception ex)
                {
                    ShowAwesomiumError(ex);
                }

                //try to open the suggestions window
                try
                {
                    if (showInterventionWindow)
                    {
                        _manager.OpenInterventionWindow();
                    }
                }
                catch (Exception ex)
                {
                    //write to the log file
                    _errorLogger.WriteToLog(string.Format("Open Suggestions window error: {0}", ex.Message), LogPriority.HighPriority);
                }

            }
        }

        private void InitStepTwo_LoginCompleted(string authKey)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                var result = MessageBox.Show("It appears as though your OSBLE+ user name or password has changed since the last time you opened Visual Studio.  Would you like to log back into OSBLE+?", "Log Into OSBLE+", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    OpenLoginScreen(this, EventArgs.Empty);
                }
            }
            else
            {
                _cache[StringConstants.AuthenticationCacheKey] = authKey;
            }

            //having logged in, we can now check to make sure we're up to date
            try
            {
                GetLibraryVersion();
            }
            catch (Exception ex)
            {
                //write to the log file
                _errorLogger.WriteToLog(string.Format("LibraryVersionNumberAsync error: {0}", ex.Message), LogPriority.HighPriority);
                _hasStartupErrors = true;
            }
        }

        private async void GetLibraryVersion()
        {
            var task = AsyncServiceClient.LibraryVersionNumber();
            var result = await task;

            InitStepThree_CheckServiceVersionComplete(result);
        }

        private async void InitStepThree_CheckServiceVersionComplete(string version)
        {
            var isOsbideUpToDate = true;

            if (string.IsNullOrWhiteSpace(version))
            {
                _errorLogger.WriteToLog("Web service error", LogPriority.HighPriority);
                _hasStartupErrors = true;
                return;
            }

            //if we have a version mismatch, stop sending data to the server & delete localDb
            if (string.Compare(StringConstants.LibraryVersion, version, StringComparison.OrdinalIgnoreCase) != 0)
            {
                isOsbideUpToDate = false;

                //download updated library version
                var web = new WebClient();
                web.DownloadFileCompleted += web_DownloadFileCompleted;
                web.Headers.Add(HttpRequestHeader.UserAgent, "OSBIDE");
                if (File.Exists(StringConstants.LocalUpdatePath))
                {
                    try
                    {
                        File.Delete(StringConstants.LocalUpdatePath);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                try
                {
                    web.DownloadFileAsync(new Uri(StringConstants.UpdateUrl), StringConstants.LocalUpdatePath);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            //if we're all up to date and had no startup errors, then we can start sending logs to the server
            if (isOsbideUpToDate && _hasStartupErrors == false)
            {
                _client.StartSending();
                ShowActivityFeedTool(this, EventArgs.Empty);

                var task = AsyncServiceClient.GetMostRecentWhatsNewItem();
                var recentNews = await task;
                GetRecentNewsItemDateComplete(recentNews);
            }
        }

        private void web_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            MessageBox.Show("Your version of the OSBLE+ Visual Studio plugin is out of date.  Installation of the latest version will now begin.");
            try
            {
                Process.Start(StringConstants.LocalUpdatePath);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void GetRecentNewsItemDateComplete(DateTime? webDate)
        {
            const string newsKey = "MostRecentNewsDate";
            DateTime cachedDate;

            //pull local cache value (note I'm being very careful here)
            if (_cache.Contains(newsKey) == false)
            {
                _cache[newsKey] = DateTime.MinValue;
            }
            try
            {
                cachedDate = (DateTime)_cache[newsKey];
            }
            catch (Exception)
            {
                cachedDate = DateTime.MinValue;
                _cache.Remove(newsKey);
            }

            //pull latest date from web
            if (webDate.HasValue && webDate.Value > cachedDate)
            {
                //store more recent value
                _cache[newsKey] = webDate.Value;

                //open news page
                OpenGenericToolWindow(StringConstants.WhatsNewUrl);
            }
        }
    }
}
