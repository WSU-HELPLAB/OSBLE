using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Resources;
using System.Text;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class CriticalReviewsPerformedDecorator : TableDecorator
    {
        public List<ReviewTeam> ReviewTeams { get; set; }

        public CriticalReviewsPerformedDecorator(ITableBuilder builder)
            :base(builder)
        {
            ReviewTeams = new List<ReviewTeam>();
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.TeacherCritical = new DynamicDictionary();
            data.TeacherCritical.ReviewTeams = ReviewTeams;

                AssignmentTeam assignTeam = assignmentTeam as AssignmentTeam;
                Assignment assignment = assignTeam.Assignment;

                //get information to download all reviews that the team did
                List<ReviewTeam> authorTeams = new List<ReviewTeam>();
                authorTeams = (from rt in assignment.ReviewTeams
                               where rt.ReviewTeamID == assignTeam.TeamID
                               select rt).ToList();

                StringBuilder reviewedTeams = new StringBuilder();
                DateTime lastSubmission = DateTime.MinValue;
                bool hasSubmission = false;
                int submittedCount = 0;
                foreach (ReviewTeam reviewTeam in authorTeams)
                {
                    DateTime? thisSub = FileSystem.GetSubmissionTime(assignTeam, reviewTeam.AuthorTeam);
                    if (thisSub != null)
                    {
                        reviewedTeams.Append(reviewTeam.AuthorTeam.Name).Append(", submitted on ").Append(thisSub).Append("\n");
                        hasSubmission = true;
                        submittedCount++;
                    }
                    else
                    {
                        reviewedTeams.Append(reviewTeam.AuthorTeam.Name).Append("; no review submitted\n");
                    }
                }
            string altText = string.Format("Download {0}'s reviews of:\n{1}", assignmentTeam.Team.Name, reviewedTeams);

            if (assignment.PreceedingAssignment.HasDeliverables && assignment.PreceedingAssignment.Deliverables[0].DeliverableType == DeliverableType.PDF)
            {
                data.TeacherCritical.IsPdfReviewAssignment = true;
            }
            else
            {
                data.TeacherCritical.IsPdfReviewAssignment = false;
            }

            data.TeacherCritical.fractionReviewed = string.Format("{0}/{1} submitted", submittedCount.ToString(), authorTeams.Count.ToString());
            data.TeacherCritical.altText = altText;
            data.TeacherCritical.hasSubmission = hasSubmission;
            data.TeacherCritical.AssignmentTeam = assignmentTeam;

            return data;
        }
    }
}
