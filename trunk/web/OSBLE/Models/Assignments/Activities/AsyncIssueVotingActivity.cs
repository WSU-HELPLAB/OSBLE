using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    public class AsyncIssueVotingActivity : IssueVotingActivity
    {
        [Display(Name = "Perform issue voting asynchronously online")]
        public bool UseIssueVotingAsync { get; set; }
        /// <summary>
        /// if true student can particapate in Async issue voting
        /// if False student cannot participate in Async issue voting
        /// </summary>
        [Display(Name = "Use only students who completed a peer review of this submission")]
        public bool UseOnlyStudentsWhoCompletedPeerReview { get; set; }

        [Display(Name = "Enable issue voting discussion")]
        public bool EnableIssueVotingDiscussion { get; set; }
        /// <summary>
        /// If true reviewer must complete voting before he can discuss any issues
        /// If False reviewer could discuss issues before voting, closely approximating FTF review.
        /// </summary>
        [Display(Name = "Require the reviewer to have completed issue voting prior to discussion")]
        public bool ReviewerMustCompleteIssueVoting { get; set; }
    }
}