using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Models.FileSystem
{
    public class AssignmentFilePath : OSBLEDirectory
    {
        private int _assignmentId;

        public AssignmentFilePath(string path, int id)
            : base(path)
        {
            _assignmentId = id;
        }

        /// <summary>
        /// Gets the path for attributable files for this assignment. These can be things 
        /// like files for detailed descriptions of the assignment requirements, files 
        /// for assignment solutions, and so on.
        /// </summary>
        public OSBLEDirectory AttributableFiles
        {
            get
            {
                return new OSBLEDirectory(
                    Path.Combine(m_path, "AssignmentDocs"),
                    Path.Combine(m_path, "AssignmentDocsAttr"));
            }
        }

        public OSBLEDirectory Review(int authorTeamID, int reviewerTeamID)
        {
            return new OSBLEDirectory(
                Path.Combine(m_path, "Reviews", authorTeamID.ToString(), reviewerTeamID.ToString()),
                Path.Combine(m_path, "ReviewsAttr", authorTeamID.ToString(), reviewerTeamID.ToString()));
        }

        public OSBLEDirectory Review(Team authorTeam, Team reviewerTeam)
        {
            return Review(authorTeam.ID, reviewerTeam.ID);
        }

        public OSBLEDirectory Submission(int teamID)
        {
            // Within this assignment folder is a "Submissions" subdirectory and 
            // folders named with team IDs within that. See FileSystem.cs for 
            // a diagram of the file system.

            return new OSBLEDirectory(
                Path.Combine(m_path, "Submissions", teamID.ToString()),
                Path.Combine(m_path, "SubmissionsAttr", teamID.ToString()));
        }

        public OSBLEDirectory Submission(Team team)
        {
            return Submission(team.ID);
        }
    }
}
