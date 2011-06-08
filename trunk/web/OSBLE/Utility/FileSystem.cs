using System.IO;
using System.Web;
using OSBLE.Models.HomePage;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.Services.Uploader;
using System;

//the folllowing is a diagram of our file system.  Items in brackes [] indicate
//using a key of sorts (e.g. the user id).  Items in curly braces {} indicate
//the intended use of the folder
/*
 *
 *                FileSystem
 *                  /    \
 *                 /      \
 *              Courses   Users
 *               /          \
 *         [courseID]     [userId]
 *           /     \          \
 *          /       \   {global user content}
 *     CourseDocs    \
 *         |          Assignments
 * {course docs go here}   |
 *                         |
 *                   [GradableId]
 *                         |
 *               [AssignmentActivityId]
 *                       /   \
 *                      /     \
 *                  AADocs   Submissions
 *                     |             \
 *       {assignment activity docs}   \
 *                                   [TeamId]
 *                                        |
 *                                {team submissions}
 * */

namespace OSBLE
{
    public static class FileSystem
    {

        private static string getRootPath()
        {
            return HttpContext.Current.Server.MapPath("\\FileSystem\\");
        }

        private static string getCoursePath(AbstractCourse course)
        {
            return getRootPath() + "Courses\\" + course.ID + "\\";
        }

        private static string getUserPath(UserProfile userprofile)
        {
            return getRootPath() + "Users\\" + userprofile.ID + "\\";
        }

        private static string getProfilePicturePath(UserProfile userProfile)
        {
            return getUserPath(userProfile) + "profile.jpg";
        }

        public static string RootPath
        {
            get
            {
                return getRootPath();
            }
        }

        public static string GetCourseDocumentsPath(AbstractCourse course)
        {
            string location = string.Format("{0}\\CourseDocs", getCoursePath(course));

            //make sure that the directory exists
            if (!Directory.Exists(location))
            {
                Directory.CreateDirectory(location);
            }
            return location;
        }


        public static string GetCourseDocumentsPath(int courseId)
        {
            Course c = new Course() {ID = courseId };
            return GetCourseDocumentsPath(c);
        }

        /// <summary>
        /// Converts the supplied file path to a web-accessible URL
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string CourseDocumentPathToWebUrl(string filePath)
        {
            int startOfWebPath = filePath.IndexOf("FileSystem");

            //get the raw url (not web accessible due to MVC restrictions)
            string rawUrl = VirtualPathUtility.ToAbsolute("~/" + filePath.Substring(startOfWebPath));

            //I thought about using regex here, but this seemed easier
            string[] rawPieces = rawUrl.Split('/');

            //from the pieces, we need the course Id and the name of the document
            int courseId = Convert.ToInt32(rawPieces[3]);
            string[] docPath = new string[rawPieces.Length - 5];
            Array.Copy(rawPieces, 5, docPath, 0, rawPieces.Length - 5);

            //finally, we can build a web-accessible url
            string url = String.Format("/FileHandler/CourseDocument/{0}/{1}", courseId, string.Join(",", docPath));
            return url;
        }

        /// <summary>
        /// Returns a list of course documents wrapped in a DirectoryListing object
        /// </summary>
        /// <param name="course"></param>
        /// <param name="includeParentLink"></param>
        /// <returns></returns>
        public static DirectoryListing GetCourseDocumentsFileList(AbstractCourse course, bool includeParentLink = true)
        {
            string path = GetCourseDocumentsPath(course);
            return BuildFileList(path, includeParentLink);
        }

        /// <summary>
        /// Returns a list of files and directories for the given path
        /// </summary>
        /// <param name="relativepath"></param>
        /// <returns></returns>
        private static DirectoryListing BuildFileList(string path, bool includeParentLink = true)
        {

            //build a new listing, set some initial values
            DirectoryListing listing = new DirectoryListing();
            listing.Name = path.Substring(path.LastIndexOf('\\') + 1);
            listing.LastModified = File.GetLastWriteTime(path);

            //handle files
            foreach (string file in Directory.GetFiles(path))
            {
                FileListing fList = new FileListing();
                fList.Name = Path.GetFileName(file);
                fList.LastModified = File.GetLastWriteTime(file);
                fList.FileUrl = FileSystem.CourseDocumentPathToWebUrl(file);
                listing.Files.Add(fList);
            }

            //Add a parent directory "..." at the top of every directory listing if requested
            if (includeParentLink)
            {
                listing.Directories.Add(new ParentDirectoryListing());
            }

            //handle other directories
            foreach (string folder in Directory.EnumerateDirectories(path))
            {
                //recursively build the directory's subcontents.  Note that we have
                //to pass only the folder's name and not the complete path
                listing.Directories.Add(BuildFileList(folder, includeParentLink));
            }

            //return the completed listing
            return listing;
        }

        public static FileStream GetDocumentForRead(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public static FileStream GetDefaultProfilePicture()
        {
            return new FileStream(HttpContext.Current.Server.MapPath("\\Content\\images\\default.jpg"), FileMode.Open, FileAccess.Read);
        }

        public static FileStream GetProfilePictureOrDefault(UserProfile userProfile)
        {
            if (File.Exists(getProfilePicturePath(userProfile)))
            {
                return new FileStream(getProfilePicturePath(userProfile), FileMode.Open, FileAccess.Read);
            }
            else
            {
                return GetDefaultProfilePicture();
            }
        }

        public static FileStream GetProfilePictureForWrite(UserProfile userProfile)
        {
            if (!Directory.Exists(getUserPath(userProfile)))
            {
                Directory.CreateDirectory(getUserPath(userProfile));
            }
            return new FileStream(getProfilePicturePath(userProfile), FileMode.Create, FileAccess.Write);
        }

        public static void DeleteProfilePicture(UserProfile userProfile)
        {
            if (File.Exists(getProfilePicturePath(userProfile)))
            {
                File.Delete(getProfilePicturePath(userProfile));
            }
        }

        private static void emptyFolder(string path)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo parent = new DirectoryInfo(path);

                foreach (FileInfo fi in parent.GetFiles())
                {
                    fi.Delete();
                }

                foreach (DirectoryInfo di in parent.GetDirectories())
                {
                    emptyFolder(di.FullName);
                    di.Delete();
                }
            }
        }

        /// <summary>
        /// Never, EVER use this function.
        /// Unless you want to wipe out the filesystem. Then by all means use it.
        /// (Used in Sample Data generation on model change.)
        /// </summary>
        public static void WipeOutFileSystem()
        {
            emptyFolder(getRootPath());
        }
    }
}