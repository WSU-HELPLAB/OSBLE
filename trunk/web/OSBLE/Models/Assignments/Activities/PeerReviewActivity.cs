using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments.Activities
{
    public class PeerReviewActivity : StudioActivity
    {
        // (Link) Associated submission name [assignment name] uneditable

        public bool studentSubmitted;
        public bool useModerators;

        //Anonymity of review
        public bool authorAnonymous;
        public bool reviewersAnonymous;
        public bool reviewerRolesAnonymous;

        //basis of review
        public bool inlineComments;
            // if inline comments checked
            public bool instructorDefinedGradingCommentCategories;
            public bool newCommentCategories;
        public bool Rubric;
            // if Rubric checked
            public bool instructorDefinedGradingRubric;
            public bool newRubric;

        //Author access to completed reviews
        public bool canStudentAccessReviews;
            // if student can access reviews is checked
            // and or if reviewerCanViewOthersReviews is true
            public bool hasStudentsCompletedAssignedReviews;

        // Reviewer access to completed reviews
        public bool canReviewerViewOthersReviews;

        //Add to gradebook (checked by default)
        public bool addGradebook = true;
            // if use rubric is selected
            public bool instructorCompletesRubricRandomReview = true; // default
            public bool instructorCompletesRubricAllReviews;
        
    }
}