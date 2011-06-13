using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments.Activities
{
    public class IssueVotingActivity : StudioActivity
    {
        //Anonymity of issue voting
        // Need reference to assignment activity

        // Requires a PeerReview
        public virtual AssignmentActivity PreviousActivity { get; set; }
     
        public bool SetGradePercentOfIssues { get; set; }
        public bool SetGradePercentAgreementWMocerator { get; set; }
        public bool SetGradeManually { get; set; }

    }
}