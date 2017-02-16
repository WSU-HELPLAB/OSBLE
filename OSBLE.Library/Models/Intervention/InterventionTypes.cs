using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Intervention
{
    public enum InterventionTypes : byte
    {
        AskForHelp = 1,
        AvailableDetails = 2,
        ClassmatesAvailable = 3,        
        MakeAPost = 4,
        MakeAPostAssignmentSubmit = 5,
        OfferHelp = 6,
        UnansweredQuestions = 7,
        BuildFailure = 8,
        UnansweredQuestionsAlternate = 9,
        //any new types...
    };
    public static class InterventionTypesExtensions
    {

        /// <summary>
        /// Returns the string name of the InterventionType.
        /// EX: SomeEnum will get returned as "SomeEnum".
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string Explode(this InterventionTypes item)
        {
            return item.ToString();
        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        /// <summary>
        /// Returns a long-form description of the enumeration.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string TemplateText(this InterventionTypes item)
        {
            string template = "";
            switch (item)
            {
                case InterventionTypes.AskForHelp:
                    template = "Hey all, I recently got a run-time error: |||[... describe run-time error here ...]||| Does anyone have any tips on how I could resolve this? \n\nI have tried [... describe what you've tried so far...].\n";
                    break;
                case InterventionTypes.AvailableDetails:
                    template = "Find out what people are talking about. Ask these classmates or the entire class a question!\n";
                    break;
                case InterventionTypes.ClassmatesAvailable:
                    template = "Hey all, I having difficulty with my program. I am running into [... describe the {error message / runtime-error / build error} here ...]. Does anyone have any tips on how I could resolve this? \n\nI have tried [... describe what you've tried so far...]\n";
                    break;                
                case InterventionTypes.MakeAPost:
                    template = "Type your post here! Use '#' to create a new topic!\n";
                    break;
                case InterventionTypes.MakeAPostAssignmentSubmit:
                    template = "Hey all, I'm done with |||[assignment # here]||| and am available if anyone wants to talk about it! \n\nI found [... aspect of assignment ...] especially [interesting/fun/tricky/etc.]. \n\n I overcame these issues by [... describe how you resolved these issues ...]\n\n One thing I learned that that might help you is: [... describe what you learned while completing this assignment ...].\n";
                    break;
                case InterventionTypes.OfferHelp:
                    template = "Hey all, I'm done with |||[assignment # here]||| and am available if anyone wants to talk about it! \n\nI found [... aspect of assignment ...] especially [interesting/fun/tricky/etc.].\n";
                    break;
                case InterventionTypes.UnansweredQuestions:
                    template = "";
                    break;
                case InterventionTypes.BuildFailure:
                    template = "Hey all, I recently got build errors: ||| [... describe build errors here ...] ||| Does anyone have any tips on how I could resolve this? \n\nI have tried [... describe what you've tried so far...].\n";
                    break;
                case InterventionTypes.UnansweredQuestionsAlternate:
                    template = "";
                    break;
                default:
                    template = "This message does not yet have a template. You should not see this message!.\n";
                    break;
            }
            return template;
        }

        /// <summary>
        /// Returns a bool corresponding to whether the content of the suggestion comes before or after the link text of the enumeration.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool ContentFirst(this InterventionTypes item)
        {
            bool contentFirst = false;
            switch (item)
            {
                case InterventionTypes.AskForHelp:
                    contentFirst = false;
                    break;
                case InterventionTypes.AvailableDetails:
                    contentFirst = true;
                    break;
                case InterventionTypes.ClassmatesAvailable:
                    contentFirst = true;
                    break;                
                case InterventionTypes.MakeAPost:
                    contentFirst = true;
                    break;
                case InterventionTypes.MakeAPostAssignmentSubmit:
                    contentFirst = true;
                    break;
                case InterventionTypes.OfferHelp:
                    contentFirst = true;
                    break;
                case InterventionTypes.UnansweredQuestions:
                    contentFirst = true;
                    break;
                case InterventionTypes.BuildFailure:
                    contentFirst = false;
                    break;
                case InterventionTypes.UnansweredQuestionsAlternate:
                    contentFirst = true;
                    break;
                default:
                    contentFirst = false;
                    break;
            }
            return contentFirst;
        }

        /// <summary>
        /// Returns an icon dictionary of the enumeration.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Dictionary<int, string> Icons(this InterventionTypes item)
        {
            Dictionary<int, string> icons = new Dictionary<int, string>();
            switch (item)
            {
                case InterventionTypes.AskForHelp:
                    icons.Add(1, "alert");
                    icons.Add(2, "comment");
                    break;
                case InterventionTypes.AvailableDetails:
                    icons.Add(1, "user");
                    icons.Add(2, "flag");
                    break;
                case InterventionTypes.ClassmatesAvailable:
                    icons.Add(1, "user");
                    icons.Add(2, "flag");
                    break;
                case InterventionTypes.MakeAPost:
                    icons.Add(1, "comment");
                    icons.Add(2, "thumbs-up");
                    break;
                case InterventionTypes.MakeAPostAssignmentSubmit:
                    icons.Add(1, "comment");
                    icons.Add(2, "thumbs-up");
                    break;
                case InterventionTypes.OfferHelp:
                    icons.Add(1, "education");
                    icons.Add(2, "thumbs-up");
                    break;
                case InterventionTypes.UnansweredQuestions:
                    icons.Add(1, "sunglasses");
                    icons.Add(2, "thumbs-up");
                    break;
                case InterventionTypes.UnansweredQuestionsAlternate:
                    icons.Add(1, "sunglasses");
                    icons.Add(2, "thumbs-up");
                    break;
                case InterventionTypes.BuildFailure:
                    icons.Add(1, "alert");
                    icons.Add(2, "comment");
                    break;
                default:
                    icons.Add(1, "alert");
                    icons.Add(2, "comment");
                    break;
            }
            return icons;
        }

        /// <summary>
        /// Returns a string title of the enumeration.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string Title(this InterventionTypes item)
        {
            string title = "";
            switch (item)
            {
                case InterventionTypes.AskForHelp:
                    title = "Runtime Errors: Get Help!";
                    break;
                case InterventionTypes.AvailableDetails:
                    title = "You Are Available!";
                    break;
                case InterventionTypes.ClassmatesAvailable:
                    title = "Others Available to Help!";
                    break;
                case InterventionTypes.MakeAPost:
                    title = "Make A Post!";
                    break;
                case InterventionTypes.MakeAPostAssignmentSubmit:
                    title = "Make a Post!";
                    break;
                case InterventionTypes.OfferHelp:
                    title = "Help Your Classmates!";
                    break;
                case InterventionTypes.UnansweredQuestions:
                    title = "Help other Students!";
                    break;
                case InterventionTypes.UnansweredQuestionsAlternate:
                    title = "Help Another Student!";
                    break;
                case InterventionTypes.BuildFailure:
                    title = "Build Errors: Get Help!";
                    break;
                default:
                    title = "";
                    break;
            }
            return title;
        }

        /// <summary>
        /// Returns a string link text of the enumeration.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string LinkText(this InterventionTypes item)
        {
            string linkText = "";
            switch (item)
            {
                case InterventionTypes.AskForHelp:
                    linkText = "Ask a question ";
                    break;
                case InterventionTypes.AvailableDetails:
                    linkText = "View/Change your status. ";
                    break;
                case InterventionTypes.ClassmatesAvailable:
                    linkText = "Ask for assistance!";
                    break;
                case InterventionTypes.MakeAPost:
                    linkText = "Tell others what you're doing!";
                    break;
                case InterventionTypes.MakeAPostAssignmentSubmit:
                    linkText = "Make a post about it!";
                    break;
                case InterventionTypes.OfferHelp:
                    linkText = "Let your classmates know you can help them!";
                    break;
                case InterventionTypes.UnansweredQuestions:
                    linkText = "Help your classmates out!";
                    break;
                case InterventionTypes.UnansweredQuestionsAlternate:
                    linkText = "Take a look at their questions!";
                    break;
                case InterventionTypes.BuildFailure:
                    linkText = "Ask a question ";
                    break;
                default:
                    linkText = "";
                    break;
            }
            return linkText;
        }

        /// <summary>
        /// Returns a string listItemContent of the enumeration.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string ListItemContent(this InterventionTypes item)
        {
            string listItemContent = "";
            switch (item)
            {
                case InterventionTypes.AskForHelp:
                    listItemContent = "about these run-time errors.";
                    break;
                case InterventionTypes.AvailableDetails:
                    listItemContent = "You have said you're available to help your classmates. ";
                    break;
                case InterventionTypes.ClassmatesAvailable:
                    listItemContent = "Some of your classmates are offering help. ";
                    break;
                case InterventionTypes.MakeAPost:
                    listItemContent = "What's going on right now?";
                    break;
                case InterventionTypes.MakeAPostAssignmentSubmit:
                    listItemContent = "You've submitted your assignment.";
                    break;
                case InterventionTypes.OfferHelp:
                    listItemContent = "You've submitted your assignment early! ";
                    break;
                case InterventionTypes.UnansweredQuestions:
                    listItemContent = "Other students have encountered issues you may have recently resolved. ";
                    break;
                case InterventionTypes.UnansweredQuestionsAlternate:
                    listItemContent = "Other students have unanswered questions. ";
                    break;
                case InterventionTypes.BuildFailure:
                    listItemContent = "about these build errors.";
                    break;
                default:
                    listItemContent = "";
                    break;
            }
            return listItemContent;
        }
    }
}
