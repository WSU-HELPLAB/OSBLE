using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OSBLE.Models.FileSystem
{
    public class GradebookFilePath : OSBLEDirectory
    {
        public GradebookFilePath(string path)
            : base(path) { }

        public override OSBLEDirectory GetDir(string subdirName)
        {
            throw new NotSupportedException(
                "Subdirectories are not supported within the gradebook directory.");
        }
    }
}
