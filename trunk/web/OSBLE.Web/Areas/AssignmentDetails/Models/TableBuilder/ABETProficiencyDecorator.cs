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
                OSBLE.Models.FileSystem.FileSystem fs =
                new OSBLE.Models.FileSystem.FileSystem();
                AttributableFilesFilePath fp = fs.
                    Course(assignmentTeam.Assignment.Course).
                    Assignment(assignmentTeam.AssignmentID).
                    Submission(assignmentTeam.TeamID) as AttributableFilesFilePath;
                AttributableFile af = fp.FirstFile;
                if (null == af)
                {
                    // Can't tag a submission that doesn't exist
                    data.ABETProficiency = string.Empty;
                }
                else
                {
                    // Check to see if the attribute exists and default to "None" if 
                    // it does not.
                    if (!af.ContainsSysAttr("ABETProficiencyLevel"))
                    {
                        data.ABETProficiency = "None";
                    }
                    else
                    {
                        data.ABETProficiency = af.GetSysAttr("ABETProficiencyLevel");
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