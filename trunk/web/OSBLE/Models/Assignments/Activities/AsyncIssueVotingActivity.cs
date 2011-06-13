using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments.Activities
{
    public class AsyncIssueVotingActivity : IssueVotingActivity
    {
        public bool UseIssueVotingAsync { get; set; }
        /// <summary>
        /// if true student can particapate in Async issue voting
        /// if False student cannot participate in Async issue voting
        /// </summary>
        public bool UseOnlyStudentsWhoCompletedPeerReview { get; set; }
        public bool EnableIssueVotingDiscussion { get; set; }
        /// <summary>
        /// If true reviewer must complete voting before he can discuss any issues
        /// If False reviewer could discuss issues before voting, closely approximating FTF review.
        /// </summary>
        public bool ReviewerMustCompleteIssueVoting { get; set; }
    }
}