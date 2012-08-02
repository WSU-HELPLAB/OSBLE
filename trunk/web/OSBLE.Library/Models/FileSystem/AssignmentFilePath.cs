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
            SubmissionFilePath sfp = new SubmissionFilePath(this, teamID);
            return sfp;
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
    }
}
