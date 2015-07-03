using System.Collections.Generic;
using OSBLEPlus.Logic.Utility;

namespace OSBIDE.Data.SQLDatabase
{
    public class TimelineStateDictionaries
    {
        public class UIProperty
        {
            public string Label { get; set; }
            public string Css { get; set; }
            public string CssGray { get; set; }
        }

        public static Dictionary<ProgrammingState, UIProperty> UIProperties = new Dictionary<ProgrammingState, UIProperty>
        {
            // {current state, next state}
            {ProgrammingState.idle, new UIProperty{Label = "--", CssGray = "idle color_grayscale", Css = "idle color_default"}},
            {ProgrammingState.edit_syn_u_sem_u, new UIProperty{Label = "??", CssGray = "edit grayscale", Css = "edit"}},
            {ProgrammingState.edit_syn_n_sem_n, new UIProperty{Label = "NN", CssGray = "edit grayscale", Css = "edit"}},
            {ProgrammingState.edit_syn_n_sem_u, new UIProperty{Label = "N?", CssGray = "edit grayscale", Css = "edit"}},
            {ProgrammingState.edit_syn_y_sem_n, new UIProperty{Label = "YN", CssGray = "edit grayscale", Css = "edit"}},
            {ProgrammingState.edit_syn_y_sem_u, new UIProperty{Label = "Y?", CssGray = "edit grayscale", Css = "edit"}},
            {ProgrammingState.debug_sem_n, new UIProperty{Label = "DN", CssGray = "debug grayscale", Css = "debug"}},
            {ProgrammingState.debug_sem_u, new UIProperty{Label = "D?", CssGray = "debug grayscale", Css = "debug"}},
            {ProgrammingState.run_sem_u, new UIProperty{Label = "R?", CssGray = "run grayscale", Css = "run"}},
            {ProgrammingState.run_sem_n, new UIProperty{Label = "RN", CssGray = "run grayscale", Css = "run"}},
            {ProgrammingState.run_last_success, new UIProperty{Label = "R/", CssGray = "run grayscale", Css = "run"}},
       };

        public static Dictionary<ProgrammingState, ProgrammingState> NextStateForBuildWithError = new Dictionary<ProgrammingState, ProgrammingState>
        {
            // {current state, next state}
            {ProgrammingState.edit_syn_u_sem_u, ProgrammingState.edit_syn_n_sem_u},
            {ProgrammingState.edit_syn_n_sem_n, ProgrammingState.edit_syn_n_sem_n},
            {ProgrammingState.edit_syn_n_sem_u, ProgrammingState.edit_syn_n_sem_u},
            {ProgrammingState.edit_syn_y_sem_n, ProgrammingState.edit_syn_n_sem_n},
            {ProgrammingState.edit_syn_y_sem_u, ProgrammingState.edit_syn_n_sem_u},
            {ProgrammingState.debug_sem_n, ProgrammingState.edit_syn_n_sem_n},
            {ProgrammingState.debug_sem_u, ProgrammingState.edit_syn_n_sem_u},
            {ProgrammingState.run_sem_u, ProgrammingState.edit_syn_n_sem_u},
            {ProgrammingState.run_sem_n, ProgrammingState.edit_syn_n_sem_n},
            {ProgrammingState.run_last_success, ProgrammingState.edit_syn_n_sem_u},
            {ProgrammingState.idle, ProgrammingState.edit_syn_n_sem_u},
        };

        public static Dictionary<ProgrammingState, ProgrammingState> NextStateForBuildWithoutError = new Dictionary<ProgrammingState, ProgrammingState>
        {
            // {current state, next state}
            {ProgrammingState.edit_syn_u_sem_u, ProgrammingState.edit_syn_y_sem_u},
            {ProgrammingState.edit_syn_n_sem_n, ProgrammingState.edit_syn_y_sem_n},
            {ProgrammingState.edit_syn_n_sem_u, ProgrammingState.edit_syn_y_sem_u},
            {ProgrammingState.edit_syn_y_sem_n, ProgrammingState.edit_syn_y_sem_n},
            {ProgrammingState.edit_syn_y_sem_u, ProgrammingState.edit_syn_y_sem_u},
            {ProgrammingState.debug_sem_n, ProgrammingState.edit_syn_y_sem_n},
            {ProgrammingState.debug_sem_u, ProgrammingState.edit_syn_y_sem_u},
            {ProgrammingState.run_sem_u, ProgrammingState.run_sem_u},
            {ProgrammingState.run_sem_n, ProgrammingState.run_sem_n},
            {ProgrammingState.run_last_success, ProgrammingState.run_last_success},
            {ProgrammingState.idle, ProgrammingState.edit_syn_y_sem_u},
        };

        public static Dictionary<ProgrammingState, ProgrammingState> NextStateForStartWithoutDebugging = new Dictionary<ProgrammingState, ProgrammingState>
        {
            // {current state, next state}
            {ProgrammingState.edit_syn_u_sem_u, ProgrammingState.run_sem_n},
            {ProgrammingState.edit_syn_n_sem_n, ProgrammingState.run_sem_n},
            {ProgrammingState.edit_syn_n_sem_u, ProgrammingState.run_sem_u},
            {ProgrammingState.edit_syn_y_sem_n, ProgrammingState.run_sem_n},
            {ProgrammingState.edit_syn_y_sem_u, ProgrammingState.run_sem_u},
            {ProgrammingState.debug_sem_n, ProgrammingState.run_sem_n},
            {ProgrammingState.debug_sem_u, ProgrammingState.run_sem_u},
            {ProgrammingState.run_sem_u, ProgrammingState.run_sem_u},
            {ProgrammingState.run_sem_n, ProgrammingState.run_sem_n},
            {ProgrammingState.run_last_success, ProgrammingState.run_last_success},
            {ProgrammingState.idle, ProgrammingState.edit_syn_u_sem_u},
        };

        public static Dictionary<ProgrammingState, ProgrammingState> NextStateForDebug = new Dictionary<ProgrammingState, ProgrammingState>
        {
            // {current state, next state}
            {ProgrammingState.edit_syn_u_sem_u, ProgrammingState.debug_sem_u},
            {ProgrammingState.edit_syn_n_sem_n, ProgrammingState.debug_sem_n}, // should not happen, the syntax has to be correct in debugging mode
            {ProgrammingState.edit_syn_n_sem_u, ProgrammingState.debug_sem_u}, // should not happen
            {ProgrammingState.edit_syn_y_sem_n, ProgrammingState.debug_sem_n},
            {ProgrammingState.edit_syn_y_sem_u, ProgrammingState.debug_sem_u},
            {ProgrammingState.debug_sem_n, ProgrammingState.debug_sem_u}, // no semantic error if the debugging state can continue 
            {ProgrammingState.debug_sem_u, ProgrammingState.debug_sem_u},
            {ProgrammingState.run_sem_u, ProgrammingState.debug_sem_u},
            {ProgrammingState.run_sem_n, ProgrammingState.debug_sem_n},
            {ProgrammingState.run_last_success, ProgrammingState.debug_sem_u},
            {ProgrammingState.idle, ProgrammingState.edit_syn_y_sem_u},
        };

        public static Dictionary<ProgrammingState, ProgrammingState> NextStateForEditorEvent = new Dictionary<ProgrammingState, ProgrammingState>
        {
            // {current state, next state}
            {ProgrammingState.edit_syn_u_sem_u, ProgrammingState.edit_syn_u_sem_u},
            {ProgrammingState.edit_syn_n_sem_n, ProgrammingState.edit_syn_n_sem_n},
            {ProgrammingState.edit_syn_n_sem_u, ProgrammingState.edit_syn_n_sem_u},
            {ProgrammingState.edit_syn_y_sem_n, ProgrammingState.edit_syn_y_sem_n},
            {ProgrammingState.edit_syn_y_sem_u, ProgrammingState.edit_syn_y_sem_u},
            {ProgrammingState.debug_sem_n, ProgrammingState.edit_syn_y_sem_n},
            {ProgrammingState.debug_sem_u, ProgrammingState.edit_syn_y_sem_u},
            {ProgrammingState.run_sem_u, ProgrammingState.edit_syn_y_sem_u},
            {ProgrammingState.run_sem_n, ProgrammingState.edit_syn_y_sem_n},
            {ProgrammingState.run_last_success, ProgrammingState.edit_syn_y_sem_u},
            {ProgrammingState.idle, ProgrammingState.edit_syn_u_sem_u},
        };

        public static Dictionary<ProgrammingState, ProgrammingState> NextStateForExceptionEvent = new Dictionary<ProgrammingState, ProgrammingState>
        {
            // {current state, next state}
            {ProgrammingState.debug_sem_n, ProgrammingState.debug_sem_n},
            {ProgrammingState.debug_sem_u, ProgrammingState.debug_sem_n},
            {ProgrammingState.run_sem_u, ProgrammingState.run_sem_n},
            {ProgrammingState.run_sem_n, ProgrammingState.run_sem_n},
            {ProgrammingState.run_last_success, ProgrammingState.run_sem_n},
            {ProgrammingState.idle, ProgrammingState.debug_sem_n},
        };

        public static List<string> EditorEvents = new List<string>
        {
            "CutCopyPasteEvent","EditorActivityEvent","SaveEvent","SubmitEvent"
        };
    }
}
