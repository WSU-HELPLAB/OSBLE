using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments
{
    public enum AssignmentTypes { Basic, CriticalReview, DiscussionAssignment };

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
	}
}