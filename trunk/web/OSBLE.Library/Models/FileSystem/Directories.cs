using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

// The following is a diagram of our file system.
// Items in brackets [] indicate using a key of sorts (e.g. the user id). It is likely 
// that many such folders exist within the directory if they are used in this way.
// Comments in curly braces {} indicate the intended use of the folder
/*
 * FileSystem
 *  |> Admin {OSBLE-wide configuration and data files}
 *  |> AdminAttr {XML attribute files corresponding to files in Admin}
 *  |> Users
 *     |> [userId] {global user content}    
 *  |> Courses
 *     |> [CourseID]
 *         |> CourseDocs
 *         |> CourseDocsAttr {stores XML attribute files that correspond to files in CourseDocs}
 *         |> Assignments
 *            |> [AssignmentID]
 *                |> AssignmentDocs
 *                |> AssignmentDocsAttr {XML attribute files corresponding to files in AssignmentDocs}
 *                |> Submissions
 *                   |> [TeamID] {team submissions}
 *                |> SubmissionsAttr
 *                   |> [TeamID] {XML attribute files corresponding to files in Submissions}
 *                |> Reviews
 *                   |> [AuthorTeamId]
 *                      |> [ReviewTeamId] {critical reviews}
 *         |> ZipFolder
 *            |> Records.txt
 *            |> {%random number%.zip}
 *         |> Gradebook
 *            |> {gradebook.zip / gradebook file}
 */
namespace OSBLE.Models.FileSystem
{
    /// <summary>
    /// Serves as the starting access point for all file system objects. When a file IO 
    /// operation of any kind needs to happen, the appropriate starting object should 
    /// be obtained from one of the static methods in this class.
    /// </summary>
    public static class Directories
    {
        [Obsolete("Use a method starting with \"Get\". This gives the folder for a course which shouldn't be needed anywhere in the OSBLE code. You should go directly to course documents, a specific assignment, or some other such thing.")]
        public static CourseFilePath Course(int id)
        {
            string path = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            return new CourseFilePath(
                System.IO.Path.Combine(path, "Courses", id.ToString()), id);
        }

        /// <summary>
        /// Gets the storage directory for data files related to administrative actions 
        /// and content. Currently these are OSBLE-wide settings. That is, there is not 
        /// a separate OSBLE administrative directory per course or anything like that. 
        /// There's just one for all of OSBLE.
        /// </summary>
        public static OSBLEDirectory GetAdmin()
        {
            string path = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            path = System.IO.Path.Combine(path, "Admin");

            // Make sure the directory exists
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            return new OSBLEDirectory(path);
        }

        public static AssignmentFilePath GetAssignment(int courseID, int assignmentID)
        {
            string path = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            path = System.IO.Path.Combine(path, "Courses", courseID.ToString(),
                "Assignments", assignmentID.ToString());

            // Make sure the directory exists
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            return new AssignmentFilePath(path, assignmentID);
        }

        public static OSBLEDirectory GetCourseDocs(int courseID)
        {
            string path = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            path = System.IO.Path.Combine(path, "Courses", courseID.ToString(), "CourseDocs");

            // Make sure the directory exists
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            return new OSBLEDirectory(path);
        }

        public static GradebookFilePath GetGradebook(int courseID)
        {
            string path = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            path = System.IO.Path.Combine(path, "Courses", courseID.ToString(), "Gradebook");

            // Make sure the directory exists
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }
            
            return new GradebookFilePath(path);
        }

        public static OSBLEDirectory GetReview(int courseID, int assignmentID, int authorTeamID, int reviewerTeamID)
        {
            string path = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            path = System.IO.Path.Combine(path, "Courses", courseID.ToString(),
                "Assignments", assignmentID.ToString(), "Reviews", authorTeamID.ToString(),
                reviewerTeamID.ToString());

            // Make sure the directory exists
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            return new OSBLEDirectory(path);
        }
    }
}
