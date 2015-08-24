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

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class StudentRubricsReceivedDecorator : TableDecorator
    {
        public StudentRubricsReceivedDecorator(ITableBuilder builder)
            : base(builder)
        {
        }

        public override DynamicDictionary BuildTableForTeam(IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.studentRubricsReceived = new DynamicDictionary();
            List<bool> hasRubricList = new List<bool>();
            List<int> authorTeamIds = new List<int>();
            foreach (TeamMember tm in assignmentTeam.Team.TeamMembers)
            {
                AssignmentTeam previousTeam = OSBLEController.GetAssignmentTeam(assignmentTeam.Assignment.PreceedingAssignment,
                    tm.CourseUser);
                authorTeamIds.Add(previousTeam.TeamID);

                using (OSBLEContext db = new OSBLEContext())
                {
                    hasRubricList.Add( (from e in db.RubricEvaluations
                                                             where e.AssignmentID == assignmentTeam.AssignmentID &&
                                                             e.RecipientID == previousTeam.TeamID &&
                                                             e.Evaluator.AbstractRoleID == (int)CourseRole.CourseRoles.Student &&
                                                             e.IsPublished
                                                             select e.ID).Count() > 0 );

                }
            }

            data.studentRubricsReceived.assignmentID = assignmentTeam.AssignmentID;

            data.studentRubricsReceived.hasRubricList = hasRubricList;
            data.studentRubricsReceived.authorTeamIDs = authorTeamIds;

            return data;
        }

    }
}
