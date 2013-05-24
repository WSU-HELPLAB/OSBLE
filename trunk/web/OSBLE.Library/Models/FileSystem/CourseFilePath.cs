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

        public GradebookFilePath Gradebook()
        {
            GradebookFilePath gfp = new GradebookFilePath(this);
            return gfp;
        }

        public CourseDocsFilePath CourseDocs()
        {
            CourseDocsFilePath cfp = new CourseDocsFilePath(this);
            return cfp;
        }

        public override string GetPath()
        {
            string returnPath = Path.Combine(PathBuilder.GetPath(), _coursePathPrefix, _courseID.ToString());
            return returnPath;
        }

        /// <summary>
        /// E.O.
        /// Gets the path for attributable files for this course.
        /// </summary>
        public AttributableFilesFilePath AttributableFiles
        {
            get
            {
                return new AttributableFilesFilePath(this);
            }
        }

        /// <summary>
        /// E.O. - (don't know why this wasn't already here)
        /// Gets the file system for the "course documents" for this course. As of 
        /// now these are files that appear on the homepage and are accessible for 
        /// all students in the course as well as the teacher.
        /// </summary>
        public FileSystemBase CourseDocs
        {
            get
            {
                return new FileSystem(Path.Combine(GetPath(), "CourseDocs"));
            }
        }
    }
}
