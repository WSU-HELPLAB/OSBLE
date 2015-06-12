using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Resources;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.FileSystem;

namespace OSBLE.Areas.AssignmentDetails.Models.TableBuilder
{
    public class ABETProficiencyDecorator : TableDecorator
    {
        public ABETProficiencyDecorator(ITableBuilder builder)
            : base(builder)
        {
        }
        
        public override Resources.DynamicDictionary BuildTableForTeam(OSBLE.Models.Assignments.IAssignmentTeam assignmentTeam)
        {
            dynamic data = Builder.BuildTableForTeam(assignmentTeam);
            data.AssignmentTeam = assignmentTeam;

            // We also need to pass the current proficiency level to the view. 
            // Such values are stored in attributes for the actual submissions 
            // for an assignment.
            if (assignmentTeam.Assignment.GetSubmissionCount() > 0)
            {
                OSBLEDirectory fp = 
                    OSBLE.Models.FileSystem.Directories.GetAssignment(
                        assignmentTeam.Assignment.Course.ID, assignmentTeam.AssignmentID)
                    .Submission(assignmentTeam.TeamID) as OSBLEDirectory;
                OSBLEFile of = fp.FirstFile;
                if (null == of)
                {
                    // Can't tag a submission that doesn't exist
                    data.ABETProficiency = string.Empty;
                }
                else
                {
                    // Check to see if the attribute exists and default to "None" if 
                    // it does not.
                    if (null == of.ABETProficiencyLevel)
                    {
                        data.ABETProficiency = "None";
                    }
                    else
                    {
                        data.ABETProficiency = of.ABETProficiencyLevel;
                    }
                }
            }
            else
            {
                // If no submissions then set to empty string
                data.ABETProficiency = string.Empty;
            }

            return data;
        }
    }
}