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

        public static AssignmentFilePath GetAssignment(int courseID, int assignmentID, string r = null)
        {
            string path = r ?? HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            
            path = System.IO.Path.Combine(path, "Courses", courseID.ToString(),
                "Assignments", assignmentID.ToString());

            // Make sure the directory exists
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            return new AssignmentFilePath(path, assignmentID);
        }

        /// <summary>
        /// Gets the directory for an assignment submission or null if a submission has not 
        /// been made by the specified team.
        /// </summary>
        public static OSBLEDirectory GetAssignmentSubmission(int courseID, int assignmentID, int teamID)
        {
            string root = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            string data = System.IO.Path.Combine(root, "Courses", courseID.ToString(),
                "Assignments", assignmentID.ToString(), "Submissions", teamID.ToString());
            string attr = System.IO.Path.Combine(root, "Courses", courseID.ToString(),
                "Assignments", assignmentID.ToString(), "SubmissionsAttr", teamID.ToString());

            // Make sure the data directory exists
            if (!System.IO.Directory.Exists(data))
            {
                return null;
            }

            return new OSBLEDirectory(data, attr);
        }

        public static OSBLEDirectory GetCourseDocs(int courseID)
        {
            string path = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            path = System.IO.Path.Combine(path, "Courses", courseID.ToString(), 
                "CourseDocs");

            // Make sure the directory exists
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            return new OSBLEDirectory(path);
        }

        public static OSBLEDirectory GetCourseZipFolder(int courseID)
        {
            string path = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            path = System.IO.Path.Combine(path, "Courses", courseID.ToString(),
                "ZipFolder");

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

        /// <summary>
        /// Gets the directory that's used to store content specific to 
        /// a user.
        /// TODO: See if this is needed. It looks like user profile pictures used to be 
        /// stored here but are now in the database and nothing else is written here.
        /// </summary>
        public static OSBLEDirectory GetUser(int userID)
        {
            string root = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
            
            // Get the data and attribute paths
            string dataPath = System.IO.Path.Combine(root, "Users", userID.ToString());
            string attrPath = System.IO.Path.Combine(root, "UsersAttr", userID.ToString());

            // Make sure the data directory exists
            if (!System.IO.Directory.Exists(dataPath))
            {
                System.IO.Directory.CreateDirectory(dataPath);
            }

            return new OSBLEDirectory(dataPath, attrPath);
        }
    }
}
