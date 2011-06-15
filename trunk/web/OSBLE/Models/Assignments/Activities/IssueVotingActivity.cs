using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    public abstract class IssueVotingActivity : StudioActivity
    {
        
        //Anonymity of issue voting
        // Need reference to assignment activity

        // Requires a PeerReview
        public virtual AssignmentActivity PreviousActivity { get; set; }
        public virtual PeerReviewActivity peerReviewActivity { get; set; }


        public enum SetGrade
        {
            [Display(Name = "Set Grade to percentage of issues voted on")]
            PercentOfIssues,
            [Display(Name = "Set grade to percent in agreement with moderator")]
            PercentAgreementWModerator,
            [Display(Name = "Manually enter grade")]
            Manually
        };

        public SetGrade setgrade { get; set; }


        [Display(Name = "Set Grade to percentage of issues voted on")]
        public bool SetGradePercentOfIssues { get; set; }

        [Display(Name = "Set grade to percent in agreement with moderator")]
        public bool SetGradePercentAgreementWModerator { get; set; }

        [Display(Name = "Manually enter grade")]
        public bool SetGradeManually { get; set; }

    }
}