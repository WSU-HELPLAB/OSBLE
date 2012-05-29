using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

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

            List<CourseUser> CourseUsers = (from tm in assignTeam.Team.TeamMembers
                                            orderby tm.CourseUser.UserProfile.LastName, tm.CourseUser.UserProfile.FirstName
                                            select tm.CourseUser).ToList();

            data.TeacherReceivedCritical.CourseUsers = CourseUsers;
            data.TeacherReceivedCritical.Assignment = assignment;
            return data;
        }

    }
}
