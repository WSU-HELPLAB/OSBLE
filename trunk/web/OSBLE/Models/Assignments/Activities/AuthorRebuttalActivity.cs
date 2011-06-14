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

        [Display(Name = "Author must accept or refute each issue")]
        public bool AuthorMustAcceptorRefuteEachIssue;

            [Display(Name = "Author must provide written rationale for issues refuted")]
            public bool AuthorMustProvideRationale;

        [Display(Name = "Author must specify whether each issue was addressed in the resubmission")]
        public bool AuthorMustSayIfIssueWasAddressed;

            [Display(Name = "Author must describe how each issue was addressed in the resubmission")]
            public bool AuthorMustDescribeHowAddressed;

    }
}