using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OSBLE.Models.Courses;
using OSBLE.Models.Assignments;

namespace OSBLE.Models.FileSystem
{
    public class CourseFilePath : FileSystemBase
    {
        private int _courseID;
        private string _coursePathPrefix = "Courses";

        public CourseFilePath(IFileSystem pathBuilder, int courseID)
            : base(pathBuilder)
        {
            _courseID = courseID;
        }

        public AssignmentFilePath Assignment(int id)
        {
            AssignmentFilePath afp = new AssignmentFilePath(this, id);
            return afp;
        }

        public AssignmentFilePath Assignment(Assignment assignment)
        {
            AssignmentFilePath afp = new AssignmentFilePath(this, assignment.ID);
            return afp;
        }

        public override string GetPath()
        {
            string returnPath = Path.Combine(PathBuilder.GetPath(), _coursePathPrefix, _courseID.ToString());
            return returnPath;
        }
    }
}
