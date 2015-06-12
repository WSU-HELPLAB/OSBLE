using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OSBLE.Models.Courses;
using OSBLE.Models.Assignments;

namespace OSBLE.Models.FileSystem
{
    public class CourseFilePath : OSBLEDirectory
    {
        private int _courseID;

        public CourseFilePath(string path, int courseID)
            : base(path)
        {
            _courseID = courseID;
        }

        public AssignmentFilePath Assignment(int id)
        {
            return new AssignmentFilePath(
                System.IO.Path.Combine(m_path, "Assignments", id.ToString()), id);
        }

        public AssignmentFilePath Assignment(Assignment assignment)
        {
            return Assignment(assignment.ID);
        }

        /// <summary>
        /// Gets the file system for the "course documents" for this course. As of 
        /// now these are files that appear on the homepage and are accessible for 
        /// all students in the course as well as the teacher.
        /// </summary>
        public OSBLEDirectory CourseDocs
        {
            get
            {
                return new OSBLEDirectory(
                    Path.Combine(m_path, "CourseDocs"),
                    Path.Combine(m_path, "CourseDocsAttr"));
            }
        }

        public GradebookFilePath Gradebook()
        {
            return new GradebookFilePath(Path.Combine(m_path, "Gradebook"));
        }
    }
}
