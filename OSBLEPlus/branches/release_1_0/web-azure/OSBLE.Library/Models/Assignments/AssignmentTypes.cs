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
        CommitteeDiscussion = 6,
        ReviewOfStudentWork = 7,
        CommitteeReview = 8,
        AggregateAssessment = 9,
        AnchoredDiscussion = 10
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
                    description = "The instructor creates teams to review student work submitted in a previous assignment. Students perform reviews by directly annotating student work and/or completing evaluation rubrics. Instructor may optionally (a) evaluate students' reviews using a rubric. and (b) release students' reviews to authors. Critical review assignments require that a basic assignment already exists.";
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
                case AssignmentTypes.CommitteeDiscussion:
                    description = "Committee chair uploads a document or set of documents (usually a PDF, e.g., an exit survey summary) to be discussed by committee asynchronously.";
                    break;
                case AssignmentTypes.ReviewOfStudentWork:
                    description = "A course instructor or committee chair uploads (if work does not exist in OSBLE), or flags in OSBLE (if work exists in OSBLE) student work for assessment. Either course instructor or the assessment committee then review the work against an assessment rubric specifying one or more target outcomes";
                    break;
                case AssignmentTypes.CommitteeReview:
                    description = "Committee engages in online discussion of their previous (independent) reviews of student work. (This assessment must be linked to a “Committee Review of Student Work” assessement).";
                    break;
                case AssignmentTypes.AggregateAssessment:
                    description = "Merges all reviews of student work for a given year, and reports average assessment ratings by outcome and student achievement level (low, medium, high).";
                    break;
                case AssignmentTypes.AnchoredDiscussion:
                    description = "A special kind of discussion assignment in which students discuss one or more PDF documents by annotating the documents with sticky notes. Sticky notes can be responded to by other students, creating a threaded discussion anchored in the documents.";
                    break;
                default:
                    description = "This assignment does not yet have a description. You should not see this message!.";
                    break;
            }
            return description;
        }
	}
}