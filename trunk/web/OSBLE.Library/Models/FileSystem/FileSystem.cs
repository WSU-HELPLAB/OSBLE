using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

//the following is a diagram of our file system.  Items in brackets [] indicate
//using a key of sorts (e.g. the user id).  Items in curly braces {} indicate
//the intended use of the folder
/*
 *
 *                          FileSystem  
 *                         /          \  
 *                        /            \  
 *                    Courses          Users                                 
 *                     /                  \                                 
 *                [courseID]            [userId]
 *               /     |    \                \
 *              /      |     \         {global user content}
 *     CourseDocs      |      ZipFolder
 *          |    Assignments        \
 *          |          |             \
 *  {course docs}      |              \
 *                     |       Records.txt { %random number%.zip}
 *                     |
 *               [AssignmentId]
 *                     |       \
 *                     |        \
 *                Submissions   Reviews
 *                     |             \
 *                     |          [AuthorTeamId]
 *                     |                 |
 *                  [TeamID]      [ReviewTeamId]
 *                   /                   |
 *       {team submissions}      {critical reviews}
 *                                     
 * */
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
