using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using OSBLE.Models.Courses;
using OSBLE.Models.Services.Uploader;
using OSBLE.Models.Users;

//the folllowing is a diagram of our file system.  Items in brackes [] indicate
//using a key of sorts (e.g. the user id).  Items in curly braces {} indicate
//the intended use of the folder
/*
 *
 *                FileSystem
 *               /    \     \
 *              /      \     \
 *          Courses   Users  Recaptcha
 *             /        \             \
 *         [courseID]   [userId]     Keys.txt
 *           /     \       \
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

        private static string OrderingFileName
        {
            get
            {
                return ".ordering";
            }
        }

        /// <summary>
        /// Returns a list of files and directories for the given path
        /// </summary>
        /// <param name="relativepath"></param>
        /// <returns></returns>
        private static DirectoryListing BuildFileList(string path, bool includeParentLink = true)
        {
            //see if we have an ordering file for the supplied path
            Dictionary<string, int> ordering = new Dictionary<string, int>();
            string orderingPath = Path.Combine(path, OrderingFileName);
            if (File.Exists(orderingPath))
            {
                ordering = GetFileOrdering(orderingPath);
            }

            //build a new listing, set some initial values
            DirectoryListing listing = new DirectoryListing();
            listing.AbsolutePath = path;
            listing.Name = path.Substring(path.LastIndexOf('\\') + 1);
            listing.LastModified = File.GetLastWriteTime(path);

            //handle files
            int defaultOrderingCount = 1000;
            foreach (string file in Directory.GetFiles(path))
            {
                //the ordering file is used to denote ordering of files and should not be
                //displayed in the file list.
                if (Path.GetFileName(file).CompareTo(OrderingFileName) == 0)
                {
                    continue;
                }
                FileListing fList = new FileListing();
                fList.AbsolutePath = file;
                fList.Name = Path.GetFileName(file);
                fList.LastModified = File.GetLastWriteTime(file);
                fList.FileUrl = FileSystem.CourseDocumentPathToWebUrl(file);

                //set file ordering if it exists
                if (ordering.ContainsKey(fList.Name))
                {
                    fList.SortOrder = ordering[fList.Name];
                }
                else
                {
                    fList.SortOrder = defaultOrderingCount;
                    defaultOrderingCount++;
                }
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
                DirectoryListing dlisting = BuildFileList(folder, includeParentLink);
                if (ordering.ContainsKey(dlisting.Name))
                {
                    dlisting.SortOrder = ordering[dlisting.Name];
                }
                else
                {
                    dlisting.SortOrder = defaultOrderingCount;
                    defaultOrderingCount++;
                }
                listing.Directories.Add(dlisting);
            }

            //return the completed listing
            return listing;
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
            Course c = new Course() { ID = courseId };
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

        public static FileStream GetDocumentForRead(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public static FileStream GetDefaultProfilePicture()
        {
            return new FileStream(HttpContext.Current.Server.MapPath("\\Content\\images\\default.jpg"), FileMode.Open, FileAccess.Read);
        }

        /// <summary>
        /// Gets the ordering scheme for the current directory
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private static Dictionary<string, int> GetFileOrdering(string filePath)
        {
            Dictionary<string, int> ordering = new Dictionary<string, int>();
            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] pieces = line.Split(',');
                    try
                    {
                        ordering.Add(pieces[0], Convert.ToInt32(pieces[1]));
                    }
                    catch
                    {
                        //lazy exception handling.
                    }
                }
            }

            return ordering;
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

        public static void EmptyFolder(string path)
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
                    EmptyFolder(di.FullName);
                    di.Delete();
                }
            }
        }

        public static void PrepCourseDocuments(DirectoryListing listing, int courseId, string previousPath = "")
        {
            string coursePath = GetCourseDocumentsPath(courseId);
            string rootPath = Path.Combine(coursePath, previousPath);
            foreach (DirectoryListing dl in listing.Directories)
            {
                string directoryPath = Path.Combine(rootPath, dl.Name);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                PrepCourseDocuments(dl, courseId, previousPath + dl.Name + "\\");
            }
        }

        public static void UpdateFileOrdering(DirectoryListing listing)
        {
            string orderingFile = Path.Combine(listing.AbsolutePath, OrderingFileName);
            using (StreamWriter writer = new StreamWriter(orderingFile))
            {

                //files first
                foreach (FileListing flisting in listing.Files)
                {
                    string line = string.Format("{0},{1}", flisting.Name, flisting.SortOrder);
                    writer.WriteLine(line);
                }

                //then directories
                foreach (DirectoryListing dlisting in listing.Directories)
                {
                    if (dlisting is ParentDirectoryListing)
                    {
                        continue;
                    }
                    string line = string.Format("{0},{1}", dlisting.Name, dlisting.SortOrder);
                    writer.WriteLine(line);

                    UpdateFileOrdering(dlisting);
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
            EmptyFolder(getRootPath());
        }
    }
}