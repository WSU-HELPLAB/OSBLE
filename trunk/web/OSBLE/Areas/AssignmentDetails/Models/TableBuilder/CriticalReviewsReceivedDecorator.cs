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

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class CriticalReviewsReceivedDecorator : TableDecorator
    {
        public CriticalReviewsReceivedDecorator(ITableBuilder builder)
            :base(builder)
        {
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.TeacherReceivedCritical = new DynamicDictionary();


            AssignmentTeam assignTeam = assignmentTeam as AssignmentTeam;
            Assignment assignment = assignTeam.Assignment;

            List<CourseUser> CourseUsersInReviewTeam = (from tm in assignTeam.Team.TeamMembers
                                            orderby tm.CourseUser.UserProfile.LastName, tm.CourseUser.UserProfile.FirstName
                                            select tm.CourseUser).ToList();

            
            string submissionFolder;
            List<DateTime?> timeStamp = new List<DateTime?>();

            foreach(CourseUser cu in CourseUsersInReviewTeam)
            {
                bool addedTimeStamp = false;
                AssignmentTeam previousAssignmentTeam = OSBLEController.GetAssignmentTeam(assignment.PreceedingAssignment, cu);
            
                foreach (AssignmentTeam at in assignment.AssignmentTeams)
                {
                    submissionFolder = FileSystem.GetTeamUserSubmissionFolderForAuthorID(false, assignment.Category.Course, assignment.ID, at, previousAssignmentTeam.Team);
                    DirectoryInfo DI = new DirectoryInfo(submissionFolder);
                    if (DI.Exists)
                    {
                        timeStamp.Add(DI.LastAccessTime);
                        addedTimeStamp = true;
                        break;
                    }
                }
                if (addedTimeStamp == false)
                {
                    timeStamp.Add(null);
                }
            }

            //build string for mousover event
            data.TeacherReceivedCritical.TimeStampList = timeStamp;
            data.TeacherReceivedCritical.CourseUsers = CourseUsersInReviewTeam;
            data.TeacherReceivedCritical.Assignment = assignment;
            return data;
        }

    }
}
