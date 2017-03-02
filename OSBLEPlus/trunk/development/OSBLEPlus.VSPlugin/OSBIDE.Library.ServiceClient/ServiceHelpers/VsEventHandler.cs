using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics.Eventing.Reader;

using EnvDTE;
using EnvDTE90a;
using StackFrame = EnvDTE.StackFrame;

using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility;
using System.Runtime.Caching;
using OSBLEPlus.Logic.Utility.Logging;
using System.Threading.Tasks;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public class VsEventHandler : EventHandlerBase
    {
        /// <summary>
        /// These events constantly fire and are of no use to us.
        /// </summary>
        private readonly List<string> _boringCommands =
            (
                new[] 
                {
                    "Build.SolutionConfigurations",
                    "Edit.GoToFindCombo",
                    string.Empty
                }
            ).ToList();

        private DateTime _lastEditorActivityEvent = DateTime.MinValue;

        public enum BreakpointIDs
        {
            ToggleBreakpoint = 255,
            BreakAtFunction = 311,
            EditorClick = 769
        };

        //used to open the intervention window based on activity results.
        dynamic _toolWindowManager;
        //method to open/refresh the window
        //_toolWindowManager.OpenInterventionWindow();

        //use this to make sure we process the intervention after the last event was logged.
        private const int intervention_processing_delay_ms = 2000;
        private readonly ObjectCache _cache = new FileCache(StringConstants.LocalCacheDirectory, new ObjectBinder());
        private readonly ILogger _logger;

        public VsEventHandler(IServiceProvider serviceProvider, IEventGenerator osbideEvents, dynamic staticToolWindowManager = null)
            : base(serviceProvider, osbideEvents)
        {
            if (staticToolWindowManager != null)
            {
                _toolWindowManager = staticToolWindowManager;
            }
        }

        private Command GetCommand(string guid, int id)
        {
            Command cmd = null;
            try
            {
                cmd = Dte.Commands.Item(guid, id);
            }
            catch (Exception)
            {
                //do nothing
            }
            return cmd;
        }

        #region EventHandlerBase Overrides

        public override void SubmitEventRequested(object sender, SubmitEventArgs e)
        {
            base.SubmitEventRequested(sender, e);

            var evt = e.Event;
            evt.SolutionName = Dte.Solution.FullName;

            //send off to the service client
            NotifyEventCreated(this, new EventCreatedArgs(evt));
        }

        //Moved into VsEventHnlder.cs from EventHanlderBase so it would have access to the intervention window triggering
        public override void SolutionOpened()
        {
            try
            {
                //Load exception handling on each project open.  Note that I'm only
                //loading C related groups as loading the entire collection takes
                //a very (10+ minute) long time to load.
                var debugger = (EnvDTE90.Debugger3)Dte.Debugger;
                string[] exceptionGroups = { "C++ Exceptions", "Win32 Exceptions", "Native Run-Time Checks" };

                if (debugger == null || debugger.ExceptionGroups == null) return;

                foreach (EnvDTE90.ExceptionSettings settings in debugger.ExceptionGroups)
                {
                    var settingsName = settings.Name;
                    if (!exceptionGroups.Contains(settingsName)) continue;

                    foreach (EnvDTE90.ExceptionSetting setting in settings)
                    {
                        settings.SetBreakWhenThrown(true, setting);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("SolutionOpened(): method: {0}: error: {1}", "crashed while loading exception handling portion", ex.Message), LogPriority.HighPriority);
            }           

            //try
            //{
            //    CheckInterventionStatus("SolutionOpened");
            //}
            //catch (Exception ex)
            //{
            //    _logger.WriteToLog(string.Format("CheckInterventionStatus(): method: {0}: error: {1}", "SolutionOpened", ex.Message), LogPriority.HighPriority);
            //}
        }

        public override void SolutionSubmitted(object sender, SubmitAssignmentArgs e)
        {
            base.SolutionSubmitted(sender, e);

            var submit = new SubmitEvent
            {
                AssignmentId = e.AssignmentId,
                SolutionName = string.Empty,
            };
            submit.CreateSolutionBinary(submit.GetSolutionBinary());

            //let others know that we have a new event
            NotifyEventCreated(this, new EventCreatedArgs(submit));

            try
            {
                CheckInterventionStatus("SolutionSubmitted");
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("CheckInterventionStatus(): method: {0}: error: {1}", "SolutionSubmitted", ex.Message), LogPriority.HighPriority);
            }
        }

        public override void DocumentSaved(Document document)
        {
            base.DocumentSaved(document);
            var save = new SaveEvent
            {
                SolutionName = Dte.Solution.FullName,
                Document = DocumentFactory.FromDteDocument(document)
            };

            //let others know that we have a new event
            NotifyEventCreated(this, new EventCreatedArgs(save));

            try
            {
                CheckInterventionStatus("DocumentSaved");
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("CheckInterventionStatus(): method: {0}: error: {1}", "DocumentSaved", ex.Message), LogPriority.HighPriority);
            }
        }

        public override void OnBuildDone(vsBuildScope scope, vsBuildAction action)
        {
            base.OnBuildDone(scope, action);

            var build = new BuildEvent
            {
                SolutionName = Dte.Solution.FullName,
            };

            var filesWithErrors = new List<string>();

            //start at 1 when iterating through Error List
            for (var i = 1; i <= Dte.ToolWindows.ErrorList.ErrorItems.Count; i++)
            {
                var item = Dte.ToolWindows.ErrorList.ErrorItems.Item(i);
                var beli = new BuildEventErrorListItem
                {
                    BuildEvent = build,
                    ErrorListItem = TypeConverters.ErrorItemToErrorListItem(item)
                };

                //only worry about critical errors
                if (!string.IsNullOrWhiteSpace(beli.ErrorListItem.CriticalErrorName))
                {
                    build.ErrorItems.Add(beli);

                    //add the file with the error to our list of items that have errors
                    if (filesWithErrors.Contains(beli.ErrorListItem.File.ToLower()) == false)
                    {
                        filesWithErrors.Add(beli.ErrorListItem.File.ToLower());
                    }
                }
            }

            //add in breakpoint information
            for (var i = 1; i <= Dte.Debugger.Breakpoints.Count; i++)
            {
                var bebp = new BuildEventBreakPoint
                {
                    BreakPoint =
                        TypeConverters.IdeBreakPointToBreakPoint(Dte.Debugger.Breakpoints.Item(i)),
                    BuildEvent = build
                };
                build.Breakpoints.Add(bebp);
            }

            //get all files in the solution
            var files = SolutionHelpers.GetSolutionFiles(Dte.Solution);

            //add in associated documents
            foreach (var bd in files.Select(file => new BuildDocument
            {
                Build = build,
                Document = file
            }))
            {
                build.Documents.Add(bd);
            }

            EventFactory.ToZippedBinary(build);

            //let others know that we have created a new event
            NotifyEventCreated(this, new EventCreatedArgs(build));

            try
            {
                CheckInterventionStatus("OnBuildDone");
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("CheckInterventionStatus(): method: {0}: error: {1}", "OnBuildDone", ex.Message), LogPriority.HighPriority);
            }
        }

        public override void GenericCommand_AfterCommandExecute(string guid, int id, object customIn, object customOut)
        {
            base.GenericCommand_AfterCommandExecute(guid, id, customIn, customOut);
            var cmd = GetCommand(guid, id);
            if (cmd == null) return;

            var commandName = cmd.Name;

            //Speed up the process by always ignoring boring commands
            if (_boringCommands.Contains(commandName) == false)
            {
                var oEvent = EventFactory.FromCommand(commandName, Dte);

                //protect against the off-chance that we'll get a null return value
                if (oEvent != null)
                {
                    //let others know that we have created a new event
                    NotifyEventCreated(this, new EventCreatedArgs(oEvent));

                    // if the user started without debugging, we need to turn on the event log listener
                    if (commandName == "Debug.StartWithoutDebugging")
                    {
                        eventLogWatcher.Enabled = true;
                    }

                    try
                    {
                        CheckInterventionStatus(oEvent.EventType.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteToLog(string.Format("CheckInterventionStatus(): method: {0}: error: {1}", "GenericCommand_AfterCommandExecute", ex.Message), LogPriority.HighPriority);
                    }
                }
            }
        }

        /// <summary>
        /// Called whenever the current line gets modified (text added / deleted).  Only raised at a maximum of
        /// once per minute in order to undercut the potential flood of event notifications.
        /// </summary>
        /// <param name="startPoint"></param>
        /// <param name="endPoint"></param>
        /// <param name="hint"></param>
        public override void EditorLineChanged(TextPoint startPoint, TextPoint endPoint, int hint)
        {
            base.EditorLineChanged(startPoint, endPoint, hint);
            if (_lastEditorActivityEvent < DateTime.UtcNow.Subtract(new TimeSpan(0, 1, 0)))
            {
                _lastEditorActivityEvent = DateTime.UtcNow;
                var activity = new EditorActivityEvent
                {
                    SolutionName = Path.GetFileName(Dte.Solution.FullName),
                    LineChanged = startPoint.Line
                };
                NotifyEventCreated(this, new EventCreatedArgs(activity));

                try
                {
                    CheckInterventionStatus("EditorLineChanged");
                }
                catch (Exception ex)
                {
                    _logger.WriteToLog(string.Format("CheckInterventionStatus(): method: {0}: error: {1}", "EditorLineChanged", ex.Message), LogPriority.HighPriority);
                }
            }
        }

        public override void OnExceptionThrown(string exceptionType, string name, int code, string description, ref dbgExceptionAction exceptionAction)
        {
            base.OnExceptionThrown(exceptionType, name, code, description, ref exceptionAction);
            HandleException(exceptionType, name, code, description, ref exceptionAction);
        }

        public override void OnExceptionNotHandled(string exceptionType, string name, int code, string description, ref dbgExceptionAction exceptionAction)
        {
            base.OnExceptionNotHandled(exceptionType, name, code, description, ref exceptionAction);
            HandleException(exceptionType, name, code, description, ref exceptionAction);
        }

        private void HandleException(string exceptionType, string name, int code, string description, ref dbgExceptionAction exceptionAction)
        {
            var ex = new ExceptionEvent();

            var debugger = Dte.Debugger as Debugger4;

            if (debugger != null)
            {
                //not sure when the current thread could be NULL, but you never know with
                //the DTE.
                if (debugger.CurrentThread != null)
                {
                    foreach (StackFrame dteFrame in debugger.CurrentThread.StackFrames)
                    {
                        ex.StackFrames.Add(TypeConverters.VsStackFrameToStackFrame(dteFrame));
                    }
                }
            }

            //the stuff inside this try will be null if there isn't an open document
            //window (rare, but possible)
            try
            {
                TextSelection debugSelection = Dte.ActiveDocument.Selection;
                debugSelection.SelectLine();
                ex.LineContent = debugSelection.Text;
                ex.LineNumber = debugSelection.CurrentLine;
                ex.DocumentName = Dte.ActiveDocument.Name;
            }
            catch (Exception)
            {
                ex.LineContent = string.Empty;
                ex.LineNumber = 0;
                ex.DocumentName = Dte.Solution.FullName;
            }

            ex.ExceptionAction = (int)exceptionAction;
            ex.ExceptionCode = code;
            ex.ExceptionDescription = description;
            ex.ExceptionName = name;
            ex.ExceptionType = exceptionType;
            ex.SolutionName = Dte.Solution.FullName;
            NotifyEventCreated(this, new EventCreatedArgs(ex));

            try
            {
                CheckInterventionStatus("HandleException");
            }
            catch (Exception e)
            {
                _logger.WriteToLog(string.Format("CheckInterventionStatus(): method: {0}: error: {1}", "HandleException", e.Message), LogPriority.HighPriority);
            }
        }

        public override void NETErrorEventRecordWritten(object sender, System.Diagnostics.Eventing.Reader.EventRecordWrittenEventArgs e)
        {
            EventRecord r = e.EventRecord;

            // parse the data from the log's properties
            List<string> data = new List<string>();
            foreach (EventProperty prop in r.Properties)
            {
                data.AddRange(prop.Value.ToString().Split('\n'));
            }

            // verify that it is related to the users app
            string appName = Path.GetFileNameWithoutExtension(Dte.Solution.FullName);
            if (data.Contains(appName + ".exe") && r.ProviderName == "Application Error")
            {
                // the user's app crashed while they ran it outside of debug mode.

                int code = int.Parse(data[6], System.Globalization.NumberStyles.HexNumber);

                // TODO: find name/type of exception from code    


                EnvDTE.dbgExceptionAction action = dbgExceptionAction.dbgExceptionActionBreak;
                HandleException("Unknown Exception Type", "Unhandled exception", code, "The program encountered an unhandled run-time exception.", ref action);

            }
        }

        #endregion

        #region OSBLE+ Suggestions specific code

        //delay check to make sure other events have finished being logged
        async System.Threading.Tasks.Task DelayCheck()
        {
            await System.Threading.Tasks.Task.Delay(intervention_processing_delay_ms);
        }

        private async void OpenInterventionWindow(string caption = "", string customUrl = "")
        {
            try
            {
                await DelayCheck();
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("OpenInterventionWindow() DelayCheck(): method: {0}: error: {1}", caption, ex.Message), LogPriority.HighPriority);                
            }
            try
            {
                _toolWindowManager.OpenInterventionWindow(null, caption, customUrl);
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("OpenInterventionWindow() _toolWindowManager.OpenInterventionWindow(): method: {0}: error: {1}", caption, ex.Message), LogPriority.HighPriority);                
            }            
        }

        private async void CheckInterventionStatus(string caption = "")
        {
            bool processIntervention = false;
            try
            {
                processIntervention = await CheckInterventionRefreshStatus(caption);
            }
            catch (Exception ex)
            {
                processIntervention = false;
                _logger.WriteToLog(string.Format("CheckInterventionStatus() CheckInterventionRefreshStatus(): method: {0}: error: {1}", caption, ex.Message), LogPriority.HighPriority);
            }

            if (processIntervention)
            {
                try
                {
                    OpenInterventionWindow("New: " + DateTime.Now.ToShortTimeString()); //we are using .Now because the time will be relative to their system
                }
                catch (Exception ex)
                {
                    _logger.WriteToLog(string.Format("CheckInterventionStatus(): method: {0}: error: {1}", caption, ex.Message), LogPriority.HighPriority);
                }
            }
        }

        /// <summary>
        /// Checks intervention status, if needed, re-opens/refreshes the intervention window
        /// </summary>
        /// <param name="caption"></param>        
        private async Task<bool> CheckInterventionRefreshStatus(string interventionTrigger = "")
        {
            try
            {
                //first check if interventions are enabled
                bool interventionsEnabled = false;
                if (!_cache.Contains("InterventionsEnabled"))
                {
                    interventionsEnabled = await InterventionsEnabled();
                }
                else
                {
                    try
                    {
                        DateTime CheckThreshold = DateTime.Now.AddHours(-1);
                        DateTime LastRefreshTime = DateTime.Parse(_cache["InterventionsEnabledRefreshedTime"].ToString());
                        if (_cache["InterventionsEnabled"].ToString() == "false" && LastRefreshTime < CheckThreshold) //check again if the last refresh was over an hour ago
                        {
                            interventionsEnabled = await InterventionsEnabled();
                        }
                        else
                        {
                            if (_cache["InterventionsEnabled"].ToString() == "true")
                            {
                                interventionsEnabled = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteToLog(string.Format("LastRefreshTime check: {0}: error: {1}", "CheckInterventionRefreshStatus", ex.Message), LogPriority.HighPriority);
                    }
                }

                //process if interventions are enabled
                if (interventionsEnabled)
                {
                    bool overrideRefresh = OverrideRefreshStatus(interventionTrigger);

                    try
                    {
                        //now setup refresh threshold
                        if (!_cache.Contains("InterventionRefreshThresholdInMinutes"))
                        {
                            await SetupInterventionRefreshThreshold();
                        }
                        else
                        {
                            int interventionRefreshThresholdInMinutes = int.Parse(_cache["InterventionRefreshThresholdInMinutes"].ToString());
                            DateTime CheckThreshold = DateTime.Now.AddHours(-1);
                            DateTime LastRefreshTime = DateTime.Parse(_cache["LastInterventionRefreshTimeThreshold"].ToString());

                            if (interventionRefreshThresholdInMinutes == 5 || LastRefreshTime < CheckThreshold) //default value or check again if the last refresh was over an hour ago
                            {
                                await SetupInterventionRefreshThreshold();
                            }
                        }

                        //now see if we need to refresh
                        if (_cache.Contains("InterventionRefresh") && _cache.Contains(StringConstants.AuthenticationCacheKey) && _cache.Contains("InterventionRefreshThresholdInMinutes"))
                        {
                            int refreshThreshold = int.Parse(_cache["InterventionRefreshThresholdInMinutes"].ToString());

                            string lastRefresh = _cache["InterventionRefresh"] as string;
                            DateTime lastRefreshDT = DateTime.Parse(lastRefresh);
                            DateTime timeNow = DateTime.Now;

                            TimeSpan difference = (timeNow - lastRefreshDT);

                            if (difference.TotalMinutes >= refreshThreshold || overrideRefresh) //TODO: check threshold for refreshing
                            {
                                string authKey = _cache[StringConstants.AuthenticationCacheKey] as string;
                                var task = AsyncServiceClient.ProcessIntervention(authKey);
                                var result = await task;
                                if (result == "true" || overrideRefresh)
                                {
                                    _cache["InterventionRefresh"] = DateTime.Now.ToString();
                                    return true;
                                }
                            }
                            //else do nothing
                        }
                        else //create the cache entry so it can be found for the next check, set it to now minus a day so we will be sure to check intervention status the first time the cache is built
                        {
                            _cache["InterventionRefresh"] = DateTime.Now.AddDays(-1).ToString();

                            //set up the refresh threshold if we, somehow, don't have it already set by this point...
                            if (!_cache.Contains("InterventionRefreshThresholdInMinutes"))
                                await SetupInterventionRefreshThreshold();

                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteToLog(string.Format("CheckInterventionRefreshStatus() error: {0}", ex.Message), LogPriority.HighPriority);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("CheckInterventionRefreshStatus() catch-all error: {0}", ex.Message), LogPriority.HighPriority);
            }
            
            return false;
        }

        private async Task<bool> SetupInterventionRefreshThreshold()
        {
            try
            {
                var result = AsyncServiceClient.GetInterventionRefreshThresholdValue();
                _cache["InterventionRefreshThresholdInMinutes"] = await result;
                _cache["LastInterventionRefreshTimeThreshold"] = DateTime.Now.ToString();
                return true;
            }
            catch (Exception ex)
            {
                _cache["InterventionRefreshThresholdInMinutes"] = 5;
                _logger.WriteToLog(string.Format("SetupInterventionRefreshThreshold() error: {0}", ex.Message), LogPriority.HighPriority);
                return false;
            }
        }

        private async Task<bool> InterventionsEnabled()
        {
            try
            {
                var task = AsyncServiceClient.InterventionsEnabled();
                _cache["InterventionsEnabled"] = await task;
                _cache["InterventionsEnabledRefreshedTime"] = DateTime.Now.ToString();
                if (task.Result == "true")
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("InterventionsEnabled() error: {0}", ex.Message), LogPriority.HighPriority);
                return false;
            }
        }

        private bool OverrideRefreshStatus(string interventionTrigger)
        {
            try
            {
                switch (interventionTrigger)
                {
                    case "SolutionOpened":
                        return true;
                        break;
                    default:
                        bool openWindow = false;
                        try
                        {
                            //if they just submitted we want to go ahead and refresh
                            if (_cache.Contains("LastSubmitTime"))
                            {
                                //process
                                string lastSubmit = _cache["LastSubmitTime"] as string;
                                DateTime lastSubmitDateTime = DateTime.Parse(lastSubmit);

                                TimeSpan difference = (DateTime.Now - lastSubmitDateTime);

                                if (Math.Abs(difference.TotalMinutes) <= 10) //If they submitted within the last 10 minutes go ahead and refresh
                                {
                                    openWindow = true;
                                    _cache["LastSubmitTime"] = DateTime.Now.AddDays(-1).ToString(); //change the last submit so we don't do this again on the next event
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.WriteToLog(string.Format("OverrideRefreshStatus() error: {0}", ex.Message), LogPriority.HighPriority);
                        }

                        return openWindow;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.WriteToLog(string.Format("OverrideRefreshStatus() error: {0}", ex.Message), LogPriority.HighPriority);
                return false;
            }            
        }
        #endregion
    }
}
