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
        TeamEvaluation = 4,
        CriticalReviewDiscussion = 5,
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
                    description = "Students may submit one or more deliverables, which the instructor may evaluate with a rubric.";
                    break;
                case AssignmentTypes.CriticalReview:
                    description = "The instructor creates teams to review student work submitted in a previous assignments. Students perform reviews by directly annotating student work and/or completing evaluation rubrics. Instructor may optionally (a) evaluate students' reviews using a rubric. and (b) release students' reviews to authors. Critical review assignments require that a basic assignment already exists.";
                    break;
                case AssignmentTypes.DiscussionAssignment:
                    description = "Students are given a discussion prompt, and engage in an online discussion, either as an entire class, or in smaller discussion groups. The instructor can require students to make initial post prior to seeing the posts of others. Instructor can optionally grade students' discussion performance using a rubric.";
                    break;
                case AssignmentTypes.TeamEvaluation:
                    description = "Students evaluate their and their teammates performance in a previous team assignment. Students are given n x 100 points to allocate across the n members of the team. Student evaluations are averaged to compute a multiplier for each team member. Instructor can require students to justify their evaluations.";
                    break;
                case AssignmentTypes.CriticalReviewDiscussion:
                    description = "Student reviews from a previous critical review assignment are merged into a single document and/or rubric, which then become the focus for an online group discussion that considers their similarities and differences. Critical review discussion teams consist of the original set of reviewers, plus the submission author(s). Critical review discussion assignments require that a critical review assignment already exists.";
                    break;
                default:
                    description = "This assignment does not yet have a description. You should not see this message!.";
                    break;
            }
            return description;
        }
	}
}