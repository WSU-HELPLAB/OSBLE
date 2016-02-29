using System.ComponentModel;

namespace OSBLEPlus.Logic.Utility.Lookups
{
    public enum EventType
    {
        [Description("Help Requests")] // Finalized
        AskForHelpEvent = 1,
        [Description("Build Event")]
        BuildEvent = 2,
        [Description("Cut Copy Paste Event")]
        CutCopyPasteEvent = 3,
        [Description("Debug Event")]
        DebugEvent = 4,
        [Description("Editor Activity Event")] 
        EditorActivityEvent = 5,
        [Description("Runtime Errors")] // Finalized
        ExceptionEvent = 6,
        [Description("Posts")] // Finalized
        FeedPostEvent = 7,
        [Description("Helpful Responses")] // Finalized
        HelpfulMarkGivenEvent = 8,
        [Description("Log Comment Event")]
        LogCommentEvent = 9,
        [Description("Save Event")]
        SaveEvent = 10,
        [Description("Assignment Submissions")] // Finalized
        SubmitEvent = 11,
        [Description("Null")]
        Null = 12
    }
}