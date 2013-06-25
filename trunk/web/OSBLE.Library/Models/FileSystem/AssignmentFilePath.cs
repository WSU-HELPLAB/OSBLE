using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Models.FileSystem
{
    public class AssignmentFilePath : FileSystemBase
    {
        private int _assignmentId;
        private string _assignmentPathPrefix = "Assignments";

        public AssignmentFilePath(IFileSystem pathBuilder, int id)
            : base(pathBuilder)
        {
            _assignmentId = id;
        }

        public IFileSystem Submission(int teamID)
        {
            // Within this assignment folder is a "Submissions" subdirectory and 
            // folders named with team IDs within that. See FileSystem.cs for 
            // a diagram of the file system.

            string p = this.GetPath();
            return new AttributableFilesPath(
                this, // For compatibility
                Path.Combine(p, "Submissions", teamID.ToString()),
                Path.Combine(p, "SubmissionsAttr", teamID.ToString()));
        }

        public IFileSystem Submission(Team team)
        {
            return Submission(team.ID);
        }

        public IFileSystem Review(int authorTeamID, int reviewerTeamID)
        {
            ReviewFilePath review = new ReviewFilePath(this, authorTeamID, reviewerTeamID);
            return review;
        }

        public IFileSystem Review(Team authorTeam, Team reviewerTeam)
        {
            return Review(authorTeam.ID, reviewerTeam.ID);
        }

        public override string GetPath()
        {
            string returnPath = Path.Combine(PathBuilder.GetPath(), _assignmentPathPrefix, _assignmentId.ToString());
            return returnPath;
        }

        /// <summary>
        /// Gets the path for attributable files for this assignment. These can be things 
        /// like files for detailed descriptions of the assignment requirements, files 
        /// for assignment solutions, and so on.
        /// </summary>
        public AttributableFilesPath AttributableFiles
        {
            get
            {
                string path = this.GetPath();
                return new AttributableFilesPath(
                    new FileSystem(Path.Combine(path, "AssignmentDocs")),
                    Path.Combine(path, "AssignmentDocs"),
                    Path.Combine(path, "AssignmentDocsAttr"));
            }
        }
    }
}
