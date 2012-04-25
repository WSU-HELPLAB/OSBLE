using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Models.ViewModels
{
    public class AssignmentDetailsViewModel
    {
        public Score score;
        public DateTime? submissionTime;
        public int postCount;
        public int replyCount;
        public Team team;
        public int assignmentID;
        public int teamEvalsCompleted;
        public int teamEvalsTotal;

        public AssignmentDetailsViewModel(int assignmentID, Score score, DateTime? submissionTime, Team team, int postCount, int replyCount)
        {
            this.assignmentID = assignmentID;
            if (score != null)
            {
                this.score = score;
            }
            this.submissionTime = submissionTime;
            this.postCount = postCount;
            this.replyCount = replyCount;
            this.team = team;
        }

        public string PrintTeam(AbstractRole role)
        {
            int i = 1;
            string teamList = "";
            if (role.Anonymized) // observer
            {
                foreach (TeamMember tm in (team.TeamMembers).OrderBy(s => s.CourseUserID))
                {
                    if (i == team.TeamMembers.Count && team.TeamMembers.Count != 1)
                    {
                        teamList += " & " + tm.CourseUser.DisplayName(role);
                    }
                    else if (i == 1)
                    {
                        teamList += tm.CourseUser.DisplayName(role);
                    }
                    else
                    {
                        teamList += ", " + tm.CourseUser.DisplayName(role);
                    }
                    i++;
                }
            }
            else
            {
                foreach (TeamMember tm in (team.TeamMembers).OrderBy(s => s.CourseUser.UserProfile.LastName).ThenBy(d => d.CourseUser.UserProfile.FirstName))
                {
                    if (i == team.TeamMembers.Count && team.TeamMembers.Count != 1)
                    {
                        teamList += " & " + tm.CourseUser.DisplayName(role);
                    }
                    else if (i == 1)
                    {
                        teamList += tm.CourseUser.DisplayName(role);
                    }
                    else
                    {
                        teamList += ", " + tm.CourseUser.DisplayName(role);
                    }
                    i++;
                }
            }
            return teamList;
        }

        //This function returns a double that has the largest difference in evaluations.
        public double largestDifferenceInEvaluation()
        {
            double returnVal = 0.0;
            Dictionary<int, double> studentDict = new Dictionary<int,double>();

            OSBLEContext db = new OSBLEContext();

            List<TeamMemberEvaluation> teamMemberEvaluations = (from t in db.TeamMemberEvaluations
                                                                where t.TeamEvaluation.TeamID == this.team.ID &&
                                                                t.TeamEvaluation.AssignmentID == this.assignmentID
                                                                select t).ToList();
            //Gathering all evals for all the team members and putting their average into studentDict
            foreach (TeamMember tm in team.TeamMembers)
            {
                int denom =  (from t in teamMemberEvaluations
                                                        where t.RecipientID == tm.CourseUserID
                                                        select t).Count();
                if(denom > 0)
                {
                    double avg = (from t in teamMemberEvaluations
                              where t.RecipientID == tm.CourseUserID
                              select t.Points).Sum() / denom;
                    studentDict.Add(tm.CourseUserID, avg); 
                }
            }

            //going through all the averaged values and extracting the one with the largest difference from 100.
            foreach (KeyValuePair<int, double> pair in studentDict)
            {
                if (Math.Abs(pair.Value - 100) > returnVal)
                    returnVal = Math.Abs(pair.Value - 100);
            }
            return returnVal;
        }
    }
}