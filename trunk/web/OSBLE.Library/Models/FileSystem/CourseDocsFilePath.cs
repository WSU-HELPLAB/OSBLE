using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OSBLE.Models.FileSystem
{
    public class CourseDocsFilePath : FileSystemBase
    {
        private string _courseDocsPrefix = "CourseDocs";
        public CourseDocsFilePath(IFileSystem pathBuilder)
            :base(pathBuilder)
        {

        }

        public override string GetPath()
        {
            string returnPath = Path.Combine(PathBuilder.GetPath(), _courseDocsPrefix);
            return returnPath;
        }
    }
}
