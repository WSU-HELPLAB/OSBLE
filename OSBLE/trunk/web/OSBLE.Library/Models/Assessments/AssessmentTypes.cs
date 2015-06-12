using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Assessments
{
    public enum AssessmentType : byte
    {
        CommitteeDiscussion = 1,
        ReviewOfStudentWork = 2,
        CommitteeReview = 3,
        AggregateAssessment = 4
    };

    public static class AssessmentTypesExtensions
    {
        /// <summary>
        /// Breaks apart the string name of the AssignmentType based on upper camel casing.
        /// EX: SomeEnum will get returned as "Some Enum".
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string Explode(this AssessmentType item)
        {
            string rawEnumValue = item.ToString();

            char[] characters = rawEnumValue.ToArray();

            string formattedValue = characters[0].ToString();
            for (int i = 1; i < characters.Length; i++)
            {
                if (char.IsUpper(characters[i]))
                {
                    formattedValue += " ";
                }
                formattedValue += characters[i].ToString();
            }
            return formattedValue;
        }

        /// <summary>
        /// Returns a long-form description of the enumeration.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string Description(this AssessmentType item)
        {
            string description = "";
            switch (item)
            {
                case AssessmentType.CommitteeDiscussion:
                    description = "Committee chair uploads a document or set of documents (usually a PDF, e.g., an exit survey summary) to be discussed by committee asynchronously.";
                    break;
                case AssessmentType.ReviewOfStudentWork:
                    description = "A course instructor or committee chair uploads (if work does not exist in OSBLE), or flags in OSBLE (if work exists in OSBLE) student work for assessment. Either course instructor or the assessment committee then review the work against an assessment rubric specifying one or more target outcomes";
                    break;
                case AssessmentType.CommitteeReview:
                    description = "Committee engages in online discussion of their previous (independent) reviews of student work. (This assessment must be linked to a “Committee Review of Student Work” assessement).";
                    break;
                case AssessmentType.AggregateAssessment:
                    description = "Merges all reviews of student work for a given year, and reports average assessment ratings by outcome and student achievement level (low, medium, high).";
                    break;
                default:
                    description = "This assessment does not yet have a description. You should not see this message!.";
                    break;
            }
            return description;
        }
    }
}
