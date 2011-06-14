using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    public class PeerReviewActivity : StudioActivity
    {
        // Need (Link) to previous Activity
        // Requires a PeerReview
        public virtual AssignmentActivity PreviousActivity { get; set; }

        [Display(Name = "Only students who submitted assignment may participate")]
        public bool UseOnlySubmittedStudents { get; set; }
        [Display(Name = "Use Moderators")]
        public bool UseModerators { get; set; }

        //Anonymity of review
        [Display(Name = "Author is anonymous")]
        public bool IsAuthorAnonymous { get; set; }
        [Display(Name = "Reviewer is Anonymous")]
        public bool IsReviewersAnonymous { get; set; }
        [Display(Name = "Reviewer roles are anonymous")]
        public bool IsReviewersRoleAnonymous { get; set; }

        //basis of review
        [Display(Name = "Use inline comments")]
        public bool UseInlineComments { get; set; }
            // if inline comments checked
            
            //Need reference to inline Comments
        [Display(Name = "Use rubric")]
        public bool UseRubric { get; set; }
            // if Rubric checked

            //Need reference to rubric

        /// <summary>
        /// If true then student can view Peer Reviews done by others of their work.
        /// </summary>
        [Display(Name = "Students can access the reviews of their subissions after the peer review deadline")]
        public bool CanStudentAccessReviews { get; set; }

            /// <summary>
            /// If true then students will have access to the Peer Reviews (if allowed) after the Peer Review has ended.
            /// If false the students will have access to the Peer Reviews (if allowed) after they have done their own Peer Reviews
            /// even after the deadline passes.  If the student does not do their peer reviews they will never be able to look at others
            /// reviews of their work.
            /// </summary>
            [Display(Name = "Require the student to complete their assigned reviews before they can access the reviews of their own submission")]
            public bool HasStudentCompletedAssignedReviews { get; set; }

        /// <summary>
        /// If true then the reviewer can see what other reviewers have already said during their reviews.
        /// </summary>
        [Display(Name = "Reviewer can view others reviewers reviews of this submission")]
        public bool CanReviewerViewOthersReviews { get; set; }

            /// <summary>
            /// If true then reviewer will have access to the Peer Reviews (if allowed) after the Peer Review has ended.
            /// If false the reviewer will have access to the Peer Reviews (if allowed) after they have done their own Peer Reviews
            /// even after the deadline passes.  
            /// </summary>
            [Display(Name = "Require the reviewer to complete their assigned reviews before they can access the reviews of the current submission")]
            public bool HasReviewerCompletedAssignedReviews { get; set; }

        
        // if use rubric is selected
        [Display(Name = "Instructor completes rubric for randomly selected review")]
        public bool InstructorCompletesRubricRandomReview { get; set; }
        [Display(Name = "Instructor completes rubric for all reviews")]
        public bool InstructorCompletesRubricAllReviews { get; set; }
        
        
    }
}