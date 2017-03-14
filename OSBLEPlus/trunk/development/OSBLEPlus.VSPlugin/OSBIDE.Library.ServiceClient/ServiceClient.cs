using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;

using OSBIDE.Library.ServiceClient.ServiceHelpers;

using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Helpers;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Logging;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBIDE.Library.ServiceClient
{
    public class ServiceClient : INotifyPropertyChanged
    {
        private static ServiceClient _instance;

        private const string CacheRegion = "ServiceClient";
        private const string CacheKey = "logs";

        private const int BatchSize = 20;

        #region instance variables

        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public event EventHandler ReceivedNewSocialActivity = delegate { };
        public event EventHandler InterventionUpdate = delegate { };

        private readonly ILogger _logger;

        private readonly ObjectCache _cache = new FileCache(StringConstants.LocalCacheDirectory, new ObjectBinder());
        private readonly TransmissionStatus _sendStatus = new TransmissionStatus();

        private bool _isSendingData;
        private bool _isCollectingDate = true;

        #endregion

        //used to open the intervention window based on activity results.
        dynamic _toolWindowManager;
        //method to open/refresh the window
        //_toolWindowManager.OpenInterventionWindow();

        #region properties

        public bool IsCollectingData
        {
            get
            {
                lock (this)
                {
                    return _isCollectingDate;
                }
            }
            set
            {
                lock (this)
                {
                    _isCollectingDate = value;
                }
                OnPropertyChanged("IsCollectingData");
            }
        }

        public bool IsSendingData
        {
            get
            {
                lock (this)
                {
                    return _isSendingData;
                }
            }
            private set
            {
                lock (this)
                {
                    _isSendingData = value;
                }
                OnPropertyChanged("IsSendingData");
            }
        }

        public TransmissionStatus SendStatus
        {
            get
            {
                lock (this)
                {
                    return _sendStatus;
                }
            }
        }

        #endregion

        #region constructor

        private ServiceClient(EventHandlerBase dteEventHandler, ILogger logger, dynamic staticToolManager = null)
        {
            if (staticToolManager != null)
            {
                _toolWindowManager = staticToolManager;
            }

            var events = dteEventHandler;
            _logger = logger;

            //AC: "events" ends up being null during unit testing.  Otherwise, it should never happen.
            if (events != null)
            {
                events.EventCreated += OsbideEventCreated;
            }

            //if we don't have a cache record of pending logs when we start, create a dummy list
            if (!_cache.Contains(CacheKey, CacheRegion))
            {
                SaveLogsToCache(new List<IActivityEvent>());
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Returns a singleton instance of <see cref="ServiceClient"/>.  Parameters are only 
        /// used during the first instantiation of the <see cref="ServiceClient"/>.
        /// </summary>
        /// <param name="dteEventHandler"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static ServiceClient GetInstance(EventHandlerBase dteEventHandler, ILogger logger, dynamic staticToolManager = null)
        {
            return _instance ?? (_instance = new ServiceClient(dteEventHandler, logger, staticToolManager));
        }

        /// <summary>
        /// Will stop data from being sent to the server
        /// </summary>
        public void StopSending()
        {
            IsSendingData = false;
        }

        /// <summary>
        /// Begins sending data to the server
        /// </summary>
        public void StartSending()
        {
            var wasNotSending = !IsSendingData;
            IsSendingData = true;

            //only start the tasks if we were not sending in the first place.
            if (wasNotSending)
            {
                //send off saved local errors
                Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            SendLocalErrorLogs();
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteToLog("Error sending local logs to server: " + ex.Message, LogPriority.MediumPriority);
                        }
                    }
                    );

                //register a thread to keep our service key from going stale
                Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            CheckStatus();
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteToLog("Error in CheckKey: " + ex.Message, LogPriority.MediumPriority);
                        }
                    }
                    );
            }
        }

        #endregion

        #region private send methods

        /// <summary>
        /// This function is responsible for continually asking for status updates from OSBIDE
        /// </summary>
        private void CheckStatus()
        {
            while (IsSendingData)
            {
                //this block checks to make sure that our authentication key is up to date
                string webServiceKey;
                lock (_cache)
                {
                    webServiceKey = _cache[StringConstants.AuthenticationCacheKey] as string;

                    var result = false;
                    try
                    {
                        var task = AsyncServiceClient.IsValidKey(webServiceKey);
                        result = task.Result;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    //if result is false, our key has gone stale.  Try to login again
                    if (result == false)
                    {
                        var userName = _cache[StringConstants.UserNameCacheKey] as string;
                        var passwordBytes = _cache[StringConstants.PasswordCacheKey] as byte[];
                        var encoderKey = _cache[StringConstants.AesKeyCacheKey] as byte[];
                        var encoderVector = _cache[StringConstants.AesVectorCacheKey] as byte[];
                        var password = string.Empty;
                        try
                        {
                            password = AesEncryption.DecryptStringFromBytes_Aes(passwordBytes, encoderKey, encoderVector);
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                        if (userName != null && password != null)
                        {
                            var task = AsyncServiceClient.Login(userName, password);
                            webServiceKey = task.Result;
                            _cache[StringConstants.AuthenticationCacheKey] = webServiceKey;
                        }
                        else
                        {
                            IsSendingData = false;
                        }
                    }
                }

                //this block checks for recent user profile activity
                DateTime lastLocalProfileUpdate;
                const string lastProfileActivityKey = "LastProfileActivity";

                //get last cached value
                lock (_cache)
                {
                    if (_cache.Contains(lastProfileActivityKey) == false)
                    {
                        _cache[lastProfileActivityKey] = DateTime.MinValue;
                    }
                    try
                    {
                        lastLocalProfileUpdate = (DateTime)_cache[lastProfileActivityKey];
                    }
                    catch (Exception)
                    {
                        lastLocalProfileUpdate = DateTime.MinValue;
                        _cache.Remove(lastProfileActivityKey);
                    }
                }

                //get last server value
                if (IsSendingData)
                {
                    DateTime lastServerProfileUpdate;
                    try
                    {
                        var task = AsyncServiceClient.GetMostRecentSocialActivity(webServiceKey);
                        lastServerProfileUpdate = task.Result;
                    }
                    catch (Exception)
                    {
                        lastServerProfileUpdate = DateTime.MinValue;
                    }

                    if (lastLocalProfileUpdate < lastServerProfileUpdate)
                    {
                        //notify client of new social activity
                        if (ReceivedNewSocialActivity != null)
                        {
                            ReceivedNewSocialActivity(this, EventArgs.Empty);
                        }

                        lock (_cache)
                        {
                            _cache[lastProfileActivityKey] = lastServerProfileUpdate;
                        }
                    }
                }

                Thread.Sleep(new TimeSpan(0, 0, 3, 0, 0));
            }
        }

        private void SendLocalErrorLogs()
        {
            var dataRoot = StringConstants.DataRoot;
            var logExtension = StringConstants.LocalErrorLogExtension;
            var today = StringConstants.LocalErrorLogFileName;

            //find all log files
            var files = Directory.GetFiles(dataRoot);
            foreach (var file in files)
            {
                if (Path.GetExtension(file) != logExtension
                    || Path.GetFileNameWithoutExtension(file) == today)
                    continue;

                var log = LocalErrorLog.FromFile(file);
                bool result;
                lock (_cache)
                {
                    var webServiceKey = _cache[StringConstants.AuthenticationCacheKey] as string;

                    var task = AsyncServiceClient.SubmitLocalErrorLog(new LocalErrorLogRequest { Log = log, AuthToken = webServiceKey });
                    result = task.Result;
                }

                //remove if file successfully sent
                if (!result) continue;
                try
                {
                    File.Delete(file);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void SendError(Exception ex)
        {
            _logger.WriteToLog(string.Format("Push error: {0}", ex.Message), LogPriority.HighPriority);
            IsSendingData = false;
        }

        private async void SendLogToServer(IActivityEvent data)
        {
            SendStatus.IsActive = true;

            List<IActivityEvent> logsToBeSaved;
            string webServiceKey;

            //request exclusive access to our cache of existing logs
            lock (_cache)
            {
                //get pending records
                logsToBeSaved = GetLogsFromCache();

                //add new log to list
                logsToBeSaved.Add(data);

                //clear out cache
                SaveLogsToCache(new List<IActivityEvent>());

                webServiceKey = (string)_cache[StringConstants.AuthenticationCacheKey];
            }

            //send logs to the server
            for (var idx = 0; idx < logsToBeSaved.Count; idx++)
            {
                var log = logsToBeSaved[idx];

                //reset the log's sending user just to be safe
                _logger.WriteToLog(string.Format("Sending {0}th of {1} log(s) to the server", idx + 1, logsToBeSaved.Count), LogPriority.LowPriority);
                SendStatus.CurrentTransmission = log;

                //update send status with the number of logs that need to submit
                SendStatus.NumberOfTransmissions = logsToBeSaved.Count;

                try
                {
                    //compose web request and send to the server
                    var eventPostRequest = GetEventPostWebRequest(log, webServiceKey);
                    var task = AsyncServiceClient.SubmitLog(eventPostRequest);
                    var result = await task;

                    //log status
                    _logger.WriteToLog(string.Format("The return code of the batch is {0}", result), LogPriority.LowPriority);
                    log.EventLogId = result;

                    //update batch status
                    SendStatus.LastTransmissionTime = DateTime.UtcNow;
                    SendStatus.LastTransmission = log;
                    SendStatus.CompletedTransmissions++;
                }
                catch (Exception ex)
                {
                    SendError(ex);
                    break;
                }
            }

            lock (_cache)
            {
                SaveLogsToCache(logsToBeSaved.Where(x => !(x.EventLogId > 0)).ToList());
            }
            SendStatus.IsActive = false;
        }

        /// <summary>
        /// Called whenever OSBIDE detects an event change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>        
        private void OsbideEventCreated(object sender, EventCreatedArgs e)
        {
            //TODO: move this to the proper location and check status first (refactor)
            //notify client of new social activity
            //if (InterventionUpdate != null)
            //{
            //    InterventionUpdate(this, EventArgs.Empty);
            //}

            //create a new event log...
            SendStatus.IsActive = false;

            //if the system is allowing web pushes, send it off.  Otherwise,
            //save to cache and try again later
            if (IsSendingData)
            {
                Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            SendLogToServer(e.OsbideEvent);                            
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteToLog(string.Format("SendToServer Error: {0}", ex.Message), LogPriority.HighPriority);                            
                        }
                    }
                    );                

            }
            else
            {
                //only continue if we're okay to collect data
                if (IsCollectingData == false)
                {
                    return;
                }

                SendStatus.IsActive = false;
                lock (_cache)
                {
                    var cachedLogs = GetLogsFromCache();
                    cachedLogs.Add(e.OsbideEvent);
                    SaveLogsToCache(cachedLogs);
                }
            }
        }

        #endregion

        #region private helpers

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SaveLogsToCache(List<IActivityEvent> logs)
        {
            _cache.Set(CacheKey, logs.ToArray(), new CacheItemPolicy(), CacheRegion);
        }

        private List<IActivityEvent> GetLogsFromCache()
        {
            var logs = new List<IActivityEvent>();

            //get pending records
            try
            {
                logs = ((IActivityEvent[])_cache.Get(CacheKey, CacheRegion)).ToList();
            }
            catch (Exception ex)
            {
                //saved logs corrupted, start over
                SaveLogsToCache(logs);
                _logger.WriteToLog(string.Format("GetLogsFromCache() error: {0}", ex.Message), LogPriority.HighPriority);
            }

            return logs;
        }

        private static EventPostRequest GetEventPostWebRequest(IActivityEvent log, string authToken)
        {
            var request = new EventPostRequest
            {
                AuthToken = authToken,
            };

            switch (log.EventType)
            {
                case EventType.AskForHelpEvent:
                    request.AskHelpEvent = (AskForHelpEvent)log;
                    break;
                case EventType.BuildEvent:
                    request.BuildEvent = (BuildEvent)log;
                    break;
                case EventType.CutCopyPasteEvent:
                    request.CutCopyPasteEvent = (CutCopyPasteEvent)log;
                    break;
                case EventType.DebugEvent:
                    request.DebugEvent = (DebugEvent)log;
                    break;
                case EventType.EditorActivityEvent:
                    request.EditorActivityEvent = (EditorActivityEvent)log;
                    break;
                case EventType.ExceptionEvent:
                    request.ExceptionEvent = (ExceptionEvent)log;
                    break;
                case EventType.FeedPostEvent:
                    request.FeedPostEvent = (FeedPostEvent)log;
                    break;
                case EventType.LogCommentEvent:
                    request.LogCommentEvent = (LogCommentEvent)log;
                    break;
                case EventType.HelpfulMarkGivenEvent:
                    request.HelpfulMarkEvent = (HelpfulMarkGivenEvent)log;
                    break;
                case EventType.SaveEvent:
                    request.SaveEvent = (SaveEvent)log;
                    break;
                case EventType.SubmitEvent:
                    request.SubmitEvent = (SubmitEvent)log;
                    break;
            }

            return request;
        }

        #endregion        
    }
}
