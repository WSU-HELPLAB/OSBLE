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

                DateTime lastSubmission = DateTime.MinValue;
                foreach (ReviewTeam reviewTeam in authorTeams)
                {
                    DateTime? thisSub = FileSystem.GetSubmissionTime(assignTeam, reviewTeam.AuthorTeam);
                    if(thisSub != null)
                    {
                        if (lastSubmission < thisSub)
                        {
                            lastSubmission = (DateTime)thisSub;
                        }
                    }                    
                }
            data.TeacherCritical.LastSubmissionTime = lastSubmission;

            data.TeacherCritical.AssignmentTeam = assignmentTeam;

            return data;
        }
    }
}
