using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using EnvDTE;
using EnvDTE90a;
using StackFrame = EnvDTE.StackFrame;

using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

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

        public VsEventHandler(IServiceProvider serviceProvider, IEventGenerator osbideEvents)
            : base(serviceProvider, osbideEvents)
        {
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
                    SolutionName = Path.GetFileName(Dte.Solution.FullName)
                };
                NotifyEventCreated(this, new EventCreatedArgs(activity));
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
        }

        #endregion
    }
}
