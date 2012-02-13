using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments
{
    public enum AssignmentTypes : byte 
    { 
        Basic = 1, 
        CriticalReview = 2, 
        DiscussionAssignment = 3,
        TeamEvaluation = 4
    };

	public static class AssignmentTypeExtensions
	{
        /// <summary>
        /// Breaks apart the string name of the AssignmentType based on upper camel casing.
        /// EX: SomeEnum will get returned as "Some Enum".
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string Explode(this AssignmentTypes item)
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
        public static string Description(this AssignmentTypes item)
        {
            string description = "";
            switch (item)
            {
                case AssignmentTypes.Basic:
                    description = "A basic, non-studio assignment.";
                    break;
                case AssignmentTypes.CriticalReview:
                    description = "Peer review assignments have students review the work of others on a previous assignment.";
                    break;
                case AssignmentTypes.DiscussionAssignment:
                    description = "Discussion assignments require students to comment on a particular discussion.";
                    break;
                default:
                    description = "Your run of the mill assignment";
                    break;
            }
            return description;
        }
	}
}