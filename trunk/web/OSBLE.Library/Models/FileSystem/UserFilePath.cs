using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OSBLE.Models.FileSystem
{
    public class UserFilePath : FileSystemBase
    {
        private int _userID;
        private string _userPathPrefix = "Users";

        public UserFilePath(IFileSystem pathBuilder, int userID)
            : base(pathBuilder)
        {
            _userID = userID;
        }

        public override string GetPath()
        {
            string returnPath = Path.Combine(PathBuilder.GetPath(), _userPathPrefix, _userID.ToString());
            return returnPath;
        }
    }
}
