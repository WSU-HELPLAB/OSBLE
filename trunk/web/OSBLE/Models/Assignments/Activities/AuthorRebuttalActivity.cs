using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    public class AuthorRebuttalActivity : StudioActivity
    {
        //Need Link to Previous activity
        // Requires a PeerReview
        public virtual AssignmentActivity PreviousActivity { get; set; }

        [Display(Name = "Present all issues logged by team, members, irrespective of voting results")]
        public bool PresentAllIssuesLogged { get; set; }

        [Display(Name = "Present only issues that received at least X number of votes")]
        public bool PresentIssuesXLogged { get; set; }
        public int xlogged { get; set; }

            [Display(Name = "Present only issues for which X percent of team members voted")]
            public bool PresentIssuesXPercentLogged { get; set; }
            public int xpercent { get; set; }

        [Display(Name = "Present only issues for which team moderator voted")]
        public bool PresentOnlyIssuesModeratorVoted { get; set; }



        [Display(Name = "Author must accept or refute each issue")]
        public bool AuthorMustAcceptorRefuteEachIssue { get; set; }

            [Display(Name = "Author must provide written rationale for issues refuted")]
            public bool AuthorMustProvideRationale { get; set; }

        [Display(Name = "Author must specify whether each issue was addressed in the resubmission")]
        public bool AuthorMustSayIfIssueWasAddressed { get; set; }

            [Display(Name = "Author must describe how each issue was addressed in the resubmission")]
            public bool AuthorMustDescribeHowAddressed { get; set; }

    }
}