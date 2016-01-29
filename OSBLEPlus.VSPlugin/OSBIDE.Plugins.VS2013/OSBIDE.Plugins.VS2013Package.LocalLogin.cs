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
            var task = AsyncServiceClient.Login(userName, password);
            var result = await task;
            Init_BrowserLogin(result);
            InitStepTwo_LoginCompleted(result);
        }

        private void Init_BrowserLogin(string authKey) //initialize browser authentication/activecourseuser
        {
            _manager.OpenActivityFeedWindow(null, 
                                            StringConstants.WebClientRoot + "/Account/TokenLogin?authToken=" + authKey +
                                            "&destinationUrl=" + StringConstants.WebClientRoot + "/feed/osbide/");
        }

        private void InitStepTwo_LoginCompleted(string authKey)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                var result = MessageBox.Show("It appears as though your OSBIDE user name or password has changed since the last time you opened Visual Studio.  Would you like to log back into OSBIDE?", "Log Into OSBIDE", MessageBoxButton.YesNo);
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
            MessageBox.Show("Your version of OSBIDE is out of date.  Installation of the latest version will now begin.");
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
