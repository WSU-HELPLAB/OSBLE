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
 * FileSystem {OSBLE-wide configuration and data files, as well as the folders listed below}
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
 *                   |> [ReviewTeamId] {critical reviews}
 *         |> ZipFolder
 *            |> Records.txt
 *            |> {%random number%.zip}
 *         |> Gradebook
 *            |> {gradebook.zip / gradebook file}
 */
namespace OSBLE.Models.FileSystem
{
    public class FileSystem : FileSystemBase
    {
        private string _fileSystemRoot = "";

        public FileSystem()
            : base(new EmptyFilePathDecorator())
        {
            _fileSystemRoot = HttpContext.Current.Server.MapPath("~\\App_Data\\FileSystem\\");
        }

        public FileSystem(string rootPath)
            : base(new EmptyFilePathDecorator())
        {
            _fileSystemRoot = rootPath;
        }

        public CourseFilePath Course(int id)
        {
            CourseFilePath cfp = new CourseFilePath(this, id);
            return cfp;
        }

        public CourseFilePath Course(Course course)
        {
            CourseFilePath cfp = new CourseFilePath(this, course.ID);
            return cfp;
        }

        public UserFilePath Users(int id)
        {
            UserFilePath ufp = new UserFilePath(this, id);
            return ufp;
        }

        public UserFilePath Users(UserProfile user)
        {
            UserFilePath ufp = new UserFilePath(this, user.ID);
            return ufp;
        }

        public override string GetPath()
        {
            //FileSystem comes first
            return _fileSystemRoot;
        }
    }
}
