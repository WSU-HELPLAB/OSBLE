using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OSBLE.Models.Assignments
{
    public class TeamConverter
    {
        /// <summary>
        /// Will convert a discussion team to a review team
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public static IReviewTeam DiscussionToReviewTeam(DiscussionTeam team)
        {
            IReviewTeam rt = new ReviewTeam();
            rt.Assignment = team.Assignment;
            rt.AssignmentID = team.AssignmentID;
            rt.AuthorTeam = team.AuthorTeam;
            rt.AuthorTeamID = (int)team.AuthorTeamID;
            rt.ReviewingTeam = team.Team;
            rt.ReviewTeamID = team.TeamID;
            return rt;
        }

        public static IList<IReviewTeam> DiscussionToReviewTeam(IEnumerable<DiscussionTeam> teams)
        {
            List<IReviewTeam> reviewTeams = new List<IReviewTeam>(teams.Count());
            foreach (DiscussionTeam dt in teams)
            {
                reviewTeams.Add(DiscussionToReviewTeam(dt));
            }
            return reviewTeams;
        }
    }
}
