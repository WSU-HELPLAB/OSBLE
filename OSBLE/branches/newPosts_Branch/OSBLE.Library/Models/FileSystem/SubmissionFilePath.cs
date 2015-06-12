using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OSBLE.Models.Assignments;

namespace OSBLE.Models.FileSystem
{
    public class SubmissionFilePath : FileSystemBase
    {
        private int _teamID;
        private string _submissionPrefix = "Submissions";

        public SubmissionFilePath(IFileSystem pathBuilder, int teamID)
            : base(pathBuilder)
        {
            _teamID = teamID;
        }

        public override string GetPath()
        {
            string returnPath = Path.Combine(PathBuilder.GetPath(), _submissionPrefix, _teamID.ToString());
            return returnPath;
        }
    }
}
