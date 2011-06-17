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
        public virtual PeerReviewActivity peerReviewActivity { get; set; }


        public enum SetGrade
        {
            PercentOfIssues,
            PercentAgreementWModerator,
            Manually
        };

        public SetGrade Setgrade { get; set; }

    }
}