using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments.Activities
{
    public class AuthorRebuttalActivity : StudioActivity
    {
        //Need Link to Previous activity
        // Requires a PeerReview
        public virtual AssignmentActivity PreviousActivity { get; set; }

        public bool AuthorMustAcceptorRefuteEachIssue;
            public bool AuthorMustProvideRationale;
        public bool AuthorMustSayIfIssueWasAddressed;
            public bool AuthorMustDescribeHowAddressed;

    }
}