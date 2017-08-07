using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Report
{
    public class OSBLEInterventionReportItem
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int CourseId { get; set; }
        public int OSBLEInterventionId { get; set; }
        public DateTime InteractionDateTime { get; set; }
        public string InteractionDetails { get; set; }
        public string InterventionFeedback { get; set; }
        public string InterventionDetailBefore { get; set; }
        public string InterventionDetailAfter { get; set; }
        public string AdditionalActionDetails { get; set; }
        public string InterventionTrigger { get; set; }
        public string InterventionType { get; set; }
        public string InterventionTemplateText { get; set; }
        public string InterventionSuggestedCode { get; set; }
        public int IsDismissed { get; set; }
        public int RefreshThreshold { get; set; }
        public int ShowInIDESuggestions { get; set; }

        public OSBLEInterventionReportItem()
        {
            FirstName = "";
            LastName = "";
            CourseId = 0;
            OSBLEInterventionId = 0;
            InteractionDateTime = new DateTime();
            InteractionDetails = "";
            InterventionFeedback = "";
            InterventionDetailBefore = "";
            InterventionDetailAfter = "";
            AdditionalActionDetails = "";
            InterventionTrigger = "";
            InterventionType = "";
            InterventionTemplateText = "";
            InterventionSuggestedCode = "";
            IsDismissed = -1;
            RefreshThreshold = -1;
            ShowInIDESuggestions = -1;
        }

    }

    public enum OSBLEInteractionReportFilters : byte    
    {
        Index, 
        Availability, 
        DismissedInterventions, 
        PrivateMessages, 
        UnansweredQuestionsLayout, 
        Status, 
        AvailableDetails, 
        DismissedInterventionsLayout, 
        UpdateSuggestionsSettings
        //any new types...
    };

    public static class OSBLEInteractionReportFiltersExtensions
    {
        /// <summary>
        /// Returns the string name of the OSBLEInteractionReportFilters.
        /// EX: SomeEnum will get returned as "SomeEnum".
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string Explode(this OSBLEInteractionReportFilters item)
        {
            return item.ToString();
        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value, true);
        }

        public static IEnumerable<T> GetValues<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }

        public static string RemoveLineEndingsAndTabs(this string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return value;
            }
            string lineSeparator = ((char)0x2028).ToString();
            string paragraphSeparator = ((char)0x2029).ToString();

            return value.Replace("\r\n", " NEWLINE ").Replace("\n", " NEWLINE ").Replace("\r", " NEWLINE ").Replace(lineSeparator, " NEWLINE ").Replace(paragraphSeparator, " NEWLINE ").Replace("\t", " TAB ");
        }

        public static string EscapeText(this string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\""; 
        }
    }
}
