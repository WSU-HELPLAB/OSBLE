using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OSBLE.Models.FileSystem
{
    public class GradebookFilePath : FileSystemBase
    {
        private string _gradebookPrefix = "Gradebook";
        public GradebookFilePath(IFileSystem pathBuilder)
            :base(pathBuilder)
        {

        }

        public override string GetPath()
        {
            string returnPath = Path.Combine(PathBuilder.GetPath(), _gradebookPrefix);
            return returnPath;
        }
    }
}
