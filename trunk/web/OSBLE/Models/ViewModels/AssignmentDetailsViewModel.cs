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

        public string PrintTeam()
        {
            int i = 1;
            string teamList = "";
            foreach (TeamMember tm in (team.TeamMembers).OrderBy(s => s.CourseUser.UserProfile.LastName).ThenBy(d => d.CourseUser.UserProfile.FirstName))
            {
                if (i == team.TeamMembers.Count && team.TeamMembers.Count != 1)
                {
                    teamList += " & " + tm.CourseUser.UserProfile.FirstName + " " + tm.CourseUser.UserProfile.LastName;
                }
                else if (i == 1)
                {
                    teamList += tm.CourseUser.UserProfile.FirstName + " " + tm.CourseUser.UserProfile.LastName;
                }
                else
                {
                    teamList += ", " + tm.CourseUser.UserProfile.FirstName + " " + tm.CourseUser.UserProfile.LastName;
                }
                i++;
            }
            return teamList;
        }
    }
}