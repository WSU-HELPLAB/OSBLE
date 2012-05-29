using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Resources;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class CriticalReviewsPerformedDecorator : TableDecorator
    {
        public CriticalReviewsPerformedDecorator(ITableBuilder builder)
            :base(builder)
        {
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.TeacherCritical = new DynamicDictionary();


                AssignmentTeam assignTeam = assignmentTeam as AssignmentTeam;
                Assignment assignment = assignTeam.Assignment;

                //get information to download all reviews that the team did
                List<ReviewTeam> authorTeams = new List<ReviewTeam>();
                authorTeams = (from rt in assignment.ReviewTeams
                               where rt.ReviewTeamID == assignTeam.TeamID
                               select rt).ToList();

                List<DateTime?> submissionTimes = new List<DateTime?>();
                foreach (ReviewTeam reviewTeam in authorTeams)
                {
                    submissionTimes.Add(FileSystem.GetSubmissionTime(assignTeam, reviewTeam.AuthorTeam));
                }

            //Get the most recent submission time
                //DateTime lastSubmission = submissionTimes.Max(DateTime => );

            //MK TODO: fix this to only pass in latest submission time
            data.TeacherCritical.submissionTimes = submissionTimes;
          
            
            data.TeacherCritical.AssignmentTeam = assignmentTeam;

            return data;
        }
    }
}
