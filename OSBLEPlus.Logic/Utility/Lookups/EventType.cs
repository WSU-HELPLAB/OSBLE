using System.ComponentModel;

namespace OSBLEPlus.Logic.Utility.Lookups
{
    public enum EventType
    {
        [Description("Ask For Help Event")]
        AskForHelpEvent = 1,
        [Description("Build Event")]
        BuildEvent = 2,
        [Description("Cut Copy Paste Event")]
        CutCopyPasteEvent = 3,
        [Description("Debug Event")]
        DebugEvent = 4,
        [Description("Editor Activity Event")]
        EditorActivityEvent = 5,
        [Description("Exception Event")]
        ExceptionEvent = 6,
        [Description("Feed Post Event")]
        FeedPostEvent = 7,
        [Description("Helpful Mark Given Event")]
        HelpfulMarkGivenEvent = 8,
        [Description("Log Comment Event")]
        LogCommentEvent = 9,
        [Description("Save Event")]
        SaveEvent = 10,
        [Description("Submit Event")]
        SubmitEvent = 11,
        [Description("Null")]
        Null = 12
    }
}