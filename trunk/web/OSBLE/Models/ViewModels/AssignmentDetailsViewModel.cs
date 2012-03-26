using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;

namespace OSBLE.Models.ViewModels
{
    public class AssignmentDetailsViewModel
    {
        public Score score;
        public DateTime? submissionTime;
        public int postCount;
        public int replyCount;
        public Team team;

        public AssignmentDetailsViewModel(Score score, DateTime? submissionTime, Team team, int postCount, int replyCount)
        {
            if (score != null)
            {
                this.score = score;
            }
            this.submissionTime = submissionTime;
            this.postCount = postCount;
            this.replyCount = replyCount;
            this.team = team;
        }
    }
}