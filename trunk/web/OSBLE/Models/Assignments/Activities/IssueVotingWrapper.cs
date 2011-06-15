using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.Models.Assignments.Activities
{
    // Strongly typed wrapper model
    public class IssueVotingWrapper 
    {
        public IssueVotingActivity issueVoting { get; set; }
        public AsyncIssueVotingActivity async_issueVoting { get; set; }

        public IssueVotingWrapper()
        {
            issueVoting = new IssueVotingActivity();
            async_issueVoting = new AsyncIssueVotingActivity();
        }
    }
}