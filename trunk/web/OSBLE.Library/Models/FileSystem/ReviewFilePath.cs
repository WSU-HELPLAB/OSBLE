using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OSBLE.Models.FileSystem
{
    public class ReviewFilePath : FileSystemBase
    {
        private int _authorTeamID;
        private int _reviewerTeamID;
        private string _reviewPrefix = "Reviews";

        public ReviewFilePath(IFileSystem pathBuilder, int authorTeamID, int reviewerTeamID)
            : base(pathBuilder)
        {
            _authorTeamID = authorTeamID;
            _reviewerTeamID = reviewerTeamID;
        }

        public override string GetPath()
        {
            string returnPath = Path.Combine(
                                            PathBuilder.GetPath(), 
                                            _reviewPrefix, 
                                            _authorTeamID.ToString(),
                                            _reviewerTeamID.ToString()
                                            );
            return returnPath;
        }
    }
}
