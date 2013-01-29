using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Controllers;
using System.IO;
using System.Text;
using OSBLE.Models;
using OSBLE.Areas.AssignmentDetails.ViewModels;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class CriticalReviewsReceivedDecorator : TableDecorator
    {
        public CriticalReviewsReceivedDecorator(ITableBuilder builder)
            : base(builder)
        {
            ReviewTeams = new List<CriticalReviewsReceivedTeam>();
        }

        public List<CriticalReviewsReceivedTeam> ReviewTeams { get; set; }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.TeacherReceivedCritical = new DynamicDictionary();
            data.TeacherReceivedCritical.ReviewTeams = ReviewTeams;
            
            AssignmentTeam assignTeam = assignmentTeam as AssignmentTeam;
            Assignment assignment = assignTeam.Assignment;
            Assignment previousAssignment = assignment.PreceedingAssignment;

            List<CourseUser> CourseUsersInReviewTeam = (from tm in assignTeam.Team.TeamMembers
                                                        orderby tm.CourseUser.UserProfile.LastName, tm.CourseUser.UserProfile.FirstName
                                                        select tm.CourseUser).ToList();
            List<CriticalReviewsReceivedTeam> reviewers = new List<CriticalReviewsReceivedTeam>();

            string submissionFolder;
            List<DateTime?> timeStamp = new List<DateTime?>();

            foreach (CourseUser cu in CourseUsersInReviewTeam)
            {
                bool addedTimeStamp = false;
                AssignmentTeam previousAssignmentTeam = OSBLEController.GetAssignmentTeam(assignment.PreceedingAssignment, cu);
                foreach (AssignmentTeam at in assignment.AssignmentTeams)
                {
                    submissionFolder = FileSystem.GetTeamUserSubmissionFolderForAuthorID(false, assignment.Course, assignment.ID, at, previousAssignmentTeam.Team);
                    DirectoryInfo DI = new DirectoryInfo(submissionFolder);
                    if (DI.Exists)
                    {
                        timeStamp.Add(DI.LastAccessTime);
                        addedTimeStamp = true;
                        data.TeacherReceivedCritical.IsPdfReviewAssignment = false;
                        break;
                    }
                    else if (previousAssignment.HasDeliverables && previousAssignment.Deliverables[0].DeliverableType == DeliverableType.PDF)
                    {
                        //AC Note: I have no clue what is going on here.  I'm inserting this code for Annodate-based PDF assignments.
                        //I'm not sure if it makes the most sense for it to go here, but it works, so...
                        data.TeacherReceivedCritical.IsPdfReviewAssignment = true;
                        reviewers = reviewers.Union(ReviewTeams.Where(rt => rt.CourseUser.ID == cu.ID)).ToList();

                        //AC: stuff that was here before
                        timeStamp.Add(DateTime.Now);
                        addedTimeStamp = true;
                        break;
                    }
                }
                if (addedTimeStamp == false)
                {
                    timeStamp.Add(null);
                }
            }

            data.TeacherReceivedCritical.Reviewers = reviewers;
            data.TeacherReceivedCritical.TimeStampList = timeStamp;
            data.TeacherReceivedCritical.CourseUsers = CourseUsersInReviewTeam;
            data.TeacherReceivedCritical.Assignment = assignment;
            return data;
        }

    }
}
