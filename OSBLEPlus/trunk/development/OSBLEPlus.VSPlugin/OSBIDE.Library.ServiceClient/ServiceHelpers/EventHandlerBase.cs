using System;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    /// <summary>
    /// The EventHandlerBase class consolidates all of the various event handler types into a single class for
    /// easy inheritance.  By default, each event handler does nothing.  
    /// </summary>
    public abstract class EventHandlerBase
    {
        /// <summary>
        /// This event is raised whenever a new event log has been created and is ready for consumption
        /// </summary>
        public event EventHandler<EventCreatedArgs> EventCreated = delegate { };

        /// <summary>
        /// The GUID that contains menu event actions
        /// </summary>
        public static string MenuEventGuid = "{5EFC7975-14BC-11CF-9B2B-00AA00573819}";

        /// <summary>
        /// GUID for physical files and folders
        /// </summary>
        public static string PhysicalFileGuid = "{6BB5F8EE-4483-11D3-8BCF-00C04F8EC28C}";

        protected DTE2 Dte
        {
            get
            {
                DTE2 dteRef = null;
                if (ServiceProvider != null)
                {
                    dteRef = (DTE2)ServiceProvider.GetService(typeof(SDTE));
                }
                return dteRef;
            }
        }
        public IServiceProvider ServiceProvider { get; set; }

        private IEventGenerator _osbideEvents;

        private BuildEvents buildEvents = null;
        private CommandEvents genericCommandEvents = null;
        private CommandEvents menuCommandEvents = null;
        private DebuggerEvents debuggerEvents = null;
        private DocumentEvents documentEvents = null;
        private FindEvents findEvents = null;
        private ProjectItemsEvents miscFileEvents = null;
        private OutputWindowEvents outputWindowEvents = null;
        private SelectionEvents selectionEvents = null;
        private SolutionEvents solutionEvents = null;
        private ProjectItemsEvents solutionItemsEvents = null;
        private TextEditorEvents textEditorEvents = null;

        public EventHandlerBase(IServiceProvider serviceProvider, IEventGenerator osbideEvents)
        {
            if (serviceProvider == null)
            {
                throw new Exception("Service provider is null");
            }

            ServiceProvider = serviceProvider;

            //attach osbide requests
            _osbideEvents = osbideEvents;
            _osbideEvents.SolutionSubmitRequest += SolutionSubmitted;
            _osbideEvents.SubmitEventRequested += SubmitEventRequested;

            //save references to dte events
            buildEvents = Dte.Events.BuildEvents;
            genericCommandEvents = Dte.Events.CommandEvents;
            menuCommandEvents = Dte.Events.get_CommandEvents(MenuEventGuid);
            debuggerEvents = Dte.Events.DebuggerEvents;
            documentEvents = Dte.Events.DocumentEvents;
            findEvents = Dte.Events.FindEvents;
            miscFileEvents = Dte.Events.MiscFilesEvents;
            outputWindowEvents = Dte.Events.OutputWindowEvents;
            selectionEvents = Dte.Events.SelectionEvents;
            solutionEvents = Dte.Events.SolutionEvents;
            solutionItemsEvents = Dte.Events.SolutionItemsEvents;
            textEditorEvents = Dte.Events.TextEditorEvents;

            //attach osbide requests
            var osbideEventGenerator = osbideEvents;
            osbideEventGenerator.SolutionSubmitRequest += SolutionSubmitted;
            osbideEventGenerator.SubmitEventRequested += SubmitEventRequested;

            //attach listeners for dte events
            //build events
            buildEvents.OnBuildBegin += OnBuildBegin;
            buildEvents.OnBuildDone += OnBuildDone;

            //generic command events
            genericCommandEvents.AfterExecute += GenericCommand_AfterCommandExecute;
            genericCommandEvents.BeforeExecute += GenericCommand_BeforeCommandExecute;

            //menu-related command command
            menuCommandEvents.AfterExecute += MenuCommand_AfterExecute;
            menuCommandEvents.BeforeExecute += MenuCommand_BeforeExecute;

            //debugger events
            debuggerEvents.OnContextChanged += OnContextChanged;
            debuggerEvents.OnEnterBreakMode += OnEnterBreakMode;
            debuggerEvents.OnEnterDesignMode += OnEnterDesignMode;
            debuggerEvents.OnEnterRunMode += OnEnterRunMode;
            debuggerEvents.OnExceptionNotHandled += OnExceptionNotHandled;
            debuggerEvents.OnExceptionThrown += OnExceptionThrown;

            //document events
            documentEvents.DocumentClosing += DocumentClosing;
            documentEvents.DocumentOpened += DocumentOpened;
            documentEvents.DocumentSaved += DocumentSaved;

            //find events
            findEvents.FindDone += FindDone;

            //misc file events
            miscFileEvents.ItemAdded += ProjectItemAdded;
            miscFileEvents.ItemRemoved += ProjectItemRemoved;
            miscFileEvents.ItemRenamed += ProjectItemRenamed;

            //output window events
            outputWindowEvents.PaneUpdated += OutputPaneUpdated;

            //selection events
            selectionEvents.OnChange += SelectionChange;

            //solution events
            solutionEvents.BeforeClosing += SolutionBeforeClosing;
            solutionEvents.Opened += SolutionOpened;
            solutionEvents.ProjectAdded += ProjectAdded;
            solutionEvents.Renamed += SolutionRenamed;

            //solution item events
            solutionItemsEvents.ItemAdded += SolutionItemAdded;
            solutionItemsEvents.ItemRemoved += SolutionItemRemoved;
            solutionItemsEvents.ItemRenamed += SolutionItemRenamed;

            //text editor events
            textEditorEvents.LineChanged += EditorLineChanged;
        }

        protected void NotifyEventCreated(object sender, EventCreatedArgs eventArgs)
        {
            EventCreated(sender, eventArgs);
        }

        //OSBIDE-specific event handlers 
        public virtual void SolutionSubmitted(object sender, SubmitAssignmentArgs e) { }
        public virtual void SubmitEventRequested(object sender, SubmitEventArgs e) { }

        //build event handlers
        public virtual void OnBuildBegin(vsBuildScope scope, vsBuildAction action) { }
        public virtual void OnBuildDone(vsBuildScope scope, vsBuildAction action) { }

        //command event handlers
        public virtual void GenericCommand_AfterCommandExecute(string guid, int id, object customIn, object customOut) { }
        public virtual void GenericCommand_BeforeCommandExecute(string guid, int id, object customIn, object customOut, ref bool cancelDefault) { }

        //generic command event handlers
        public virtual void MenuCommand_BeforeExecute(string guid, int id, object customIn, object customOut, ref bool cancelDefault) { }
        public virtual void MenuCommand_AfterExecute(string guid, int id, object customIn, object customOut) { }

        //debugger event handlers
        public virtual void OnContextChanged(Process newProcess, Program newProgram, Thread newThread, StackFrame newStackFrame) { }
        public virtual void OnEnterBreakMode(dbgEventReason reason, ref dbgExecutionAction executionAction) { }
        public virtual void OnEnterDesignMode(dbgEventReason reason) { }
        public virtual void OnEnterRunMode(dbgEventReason reason) { }
        public virtual void OnExceptionNotHandled(string exceptionType, string name, int code, string description, ref dbgExceptionAction exceptionAction) { }
        public virtual void OnExceptionThrown(string exceptionType, string name, int code, string description, ref dbgExceptionAction exceptionAction) { }

        //document event handlers
        public virtual void DocumentClosing(Document document) { }
        public virtual void DocumentOpened(Document document) { }
        public virtual void DocumentSaved(Document document) { }

        //find event handlers
        public virtual void FindDone(vsFindResult result, bool cancelled) { }

        //misc file event handlers
        public virtual void ProjectItemAdded(ProjectItem projectItem) { }
        public virtual void ProjectItemRemoved(ProjectItem projectItem) { }
        public virtual void ProjectItemRenamed(ProjectItem projectItem, string oldName) { }

        //output window event handlers
        public virtual void OutputPaneUpdated(OutputWindowPane pPane) { }

        //selection event handlers
        public virtual void SelectionChange() { }

        //solution event handlers
        public virtual void SolutionBeforeClosing() { }
        public virtual void SolutionOpened()
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
        public virtual void ProjectAdded(Project project) { }
        public virtual void SolutionRenamed(string oldName) { }

        //solution item event handlers
        public virtual void SolutionItemAdded(ProjectItem projectItem) { }
        public virtual void SolutionItemRemoved(ProjectItem projectItem) { }
        public virtual void SolutionItemRenamed(ProjectItem projectItem, string oldName) { }

        //text editor event handlers
        public virtual void EditorLineChanged(TextPoint startPoint, TextPoint endPoint, int hint) { }
    }
}
