using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OSBLE.Models.FileSystem
{
    public class ReviewFilePath : FileSystemBase
    {
        private int _courseUserID;
        private string _reviewPrefix = "Reviews";

        public ReviewFilePath(IFileSystem pathBuilder, int cuID)
            : base(pathBuilder)
        {
            _courseUserID = cuID;
        }

        public override string GetPath()
        {
            string returnPath = Path.Combine(PathBuilder.GetPath(), _reviewPrefix, _courseUserID.ToString());
            return returnPath;
        }
    }
}
