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
 *                          ____FileSystem_____
 *                         /                   \  
 *                        /                     \  
 *                    Courses                  Users                                 
 *                     /                          \                                 
 *                     |                        [userId]
 *                     |                           |
 *                     |                    {global user content}
 *                     |
 *                [courseID]____________________________                    
 *               /     |  \ \_________                  \
 *              /      |   \          \                AttributableFiles
 *     CourseDocs      |    ZipFolder  \                 /     \
 *          |    Assignments     \      Gradebook      data    attr
 *          |          |          \         |            
 *  {course docs}      |           \     {gradebook.zip / gradebook file}
 *                     |  Records.txt { %random number%.zip}
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

// Added May 15, 2013 by Evan Olds
// Description of contents in the "AttributableFiles" folder for a course:
// This is a collection of files and accompanying attribute files. The idea is 
// that this is just a collection of files with attributes. The attributes can then 
// determine what the files will be used for, who can access them, etc.
// It is intended for files to be put here only by instructor uploads (although there 
// may be other priviledged roles that can upload here in the future).
// data folder: the actual uploaded files go here
// attr folder: XML attribute files go here, corresponding to the data files
//  - The attribute files contain a list of system and user attributes.
// See file: AttributableFilesFilePath.cs
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
