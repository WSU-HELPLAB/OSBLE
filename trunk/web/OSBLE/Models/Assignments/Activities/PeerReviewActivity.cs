using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments.Activities
{
    public class PeerReviewActivity : StudioActivity
    {
        // Need (Link) to previous Activity
        // Requires a PeerReview
        public virtual AssignmentActivity PreviousActivity { get; set; }

        public bool UseOnlySubmittedStudents { get; set; }
        public bool UseModerators { get; set; }

        //Anonymity of review
        public bool IsAuthorAnonymous { get; set; }
        public bool IsReviewersAnonymous { get; set; }
        public bool IsReviewersRoleAnonymous { get; set; }

        //basis of review
        public bool UseInlineComments { get; set; }
            // if inline comments checked
            
            //Need reference to inline Comments

        public bool UseRubric { get; set; }
            // if Rubric checked

            //Need reference to rubric

        /// <summary>
        /// If true then student can view Peer Reviews done by others of their work.
        /// </summary>
        public bool CanStudentAccessReviews { get; set; }

            /// <summary>
            /// If true then students will have access to the Peer Reviews (if allowed) after the Peer Review has ended.
            /// If false the students will have access to the Peer Reviews (if allowed) after they have done their own Peer Reviews
            /// even after the deadline passes.  If the student does not do their peer reviews they will never be able to look at others
            /// reviews of their work.
            /// </summary>
            public bool HasStudentCompletedAssignedReviews { get; set; }

        /// <summary>
        /// If true then the reviewer can see what other reviewers have already said during their reviews.
        /// </summary>
        public bool CanReviewerViewOthersReviews { get; set; }

        
        
            // if use rubric is selected
            public bool InstructorCompletesRubricRandomReview { get; set; }
            public bool InstructorCompletesRubricAllReviews { get; set; }
        
        
    }
}