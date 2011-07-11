using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Ionic.Zip;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Services.Uploader;
using OSBLE.Models.Users;

//the folllowing is a diagram of our file system.  Items in brackes [] indicate
//using a key of sorts (e.g. the user id).  Items in curly braces {} indicate
//the intended use of the folder
/*
 *
 *                FileSystem
 *               /    \    \
 *              /      \   ZipFolder
 *          Courses   Users    \
 *             /         \    Records.txt { %hash%.zip}
 *         [courseID]  [userId]
 *           /      \      \
 *          /        \ {global user content}
 *     CourseDocs     \
 *         |          Assignments
 * {course docs go here}   |
 *                         |
 *               [AssignmentActivityId]
 *                       /   \
 *                      /     \
 *                  AADocs   Submissions
 *                     |             \
 *       {assignment activity docs}   \
 *                                   [TeamUserMemberId]
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
                    defaultOrderingCount++;
                    while (ordering.ContainsValue(defaultOrderingCount))
                    {
                        defaultOrderingCount++;
                    }
                    fList.SortOrder = defaultOrderingCount;
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
                    defaultOrderingCount++;
                    while (ordering.ContainsValue(defaultOrderingCount))
                    {
                        defaultOrderingCount++;
                    }
                    dlisting.SortOrder = defaultOrderingCount;
                    defaultOrderingCount++;
                }
                listing.Directories.Add(dlisting);
            }

            //return the completed listing
            return listing;
        }

        private static string getZipFolderLocation()
        {
            return getRootPath() + "ZipFolder";
        }

        private static string getZipFilesRecords()
        {
            return getZipFolderLocation() + "\\" + "Records.txt";
        }

        private static void RemoveOldZipFiles()
        {
            string records = getZipFilesRecords();
            string[] fileLines = { };
            try
            {
                using (StreamReader sr = new StreamReader(records))
                {
                    fileLines = sr.ReadToEnd().Split("\r\n".ToCharArray());
                }

                //delete the old file since we have it saved in memory
                new FileInfo(records).Delete();

                try
                {
                    using (StreamWriter sw = new StreamWriter(records))
                    {
                        foreach (string line in fileLines)
                        {
                            if (line.Trim() != "")
                            {
                                string[] lineSections = line.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                DateTime dt = DateTime.Parse(lineSections[2]);
                                dt = dt.AddDays(7);

                                //if the file is more than 7 days old remove the file
                                if (dt < DateTime.Now)
                                {
                                    FileInfo zipFile = new FileInfo(getZipFolderLocation() + "\\" + lineSections[1]);
                                    zipFile.Delete();
                                }
                                else
                                {
                                    //if not older than 7 days then write the file back into the records file
                                    sw.WriteLine(line);
                                }
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    throw new Exception("{root}\\ZipFolder\\Records.txt is corrupt", e);
                }
            }
            catch
            {
                //the records file must not exist so we got to make sure the directory exists

                DirectoryInfo info = new DirectoryInfo(getZipFolderLocation());
                if (!(info.Exists))
                {
                    info.Create();
                }

                return;
            }
        }

        /// <summary>
        /// This generates a new fileName that in the given directory with the given extension
        /// </summary>
        /// <param name="directory">This must be a valid directory</param>
        /// <param name="extension">This must be an extension name with the . included</param>
        /// <returns>The full name (including directory of a file name that is guaranteed not to exist already
        /// in the given directory</returns>
        private static string GeneratedUnusedFileName(string directory, string extension)
        {
            FileInfo info;
            do
            {
                Random rand = new Random();
                info = new FileInfo(directory + "\\" + rand.Next().ToString() + extension);
            } while (info.Exists);
            return info.FullName;
        }

        private static void AddEntryToZipRecords(string zipFileName, string realName, DateTime created)
        {
            using (StreamWriter sw = new StreamWriter(getZipFilesRecords(), true))
            {
                sw.WriteLine(zipFileName.Split(new char[] { '\\' }).Last() + "," + SanitizeCommas(realName) + "," + created.ToString());
            }
        }

        private static string UnSanitizeCommas(string s)
        {
            s = s.Replace("&comma;", ",");
            s = s.Replace("&amp;", "&");

            return s;
        }

        private static string SanitizeCommas(string s)
        {
            string newString = s.Replace("&", "&amp;");
            newString = s.Replace(",", "&comma;");
            return newString;
        }

        private static string FindZipFileLocation(string realName)
        {
            try
            {
                using (StreamReader sr = new StreamReader(getZipFilesRecords()))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.Trim() != "")
                        {
                            string[] lineSections = line.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            if (lineSections[1] == realName)
                            {
                                return getZipFolderLocation() + "\\" + lineSections[0];
                            }
                        }
                    }

                    return null;
                }
            }
            catch
            {
                //error to guess we didn't find it
                return null;
            }
        }

        private static void deleteFile(string path)
        {
            new FileInfo(path).Delete();
        }

        private static string getRealFileZipName(AbstractAssignmentActivity activity, TeamUserMember member = null)
        {
            string s = "activityID = " + activity.ID.ToString();
            if (member != null)
            {
                s += " TeamuserMemberID = " + member.ID;
            }
            return s;
        }

        public static void RemoveZipFile(AbstractAssignmentActivity activity, TeamUserMember teamUser)
        {
            bool foundTeamUserZip = false, foundActivityZip = false;
            string pathTeamUser = FindZipFileLocation(getRealFileZipName(activity, teamUser));

            if (pathTeamUser != null)
            {
                foundTeamUserZip = true;
                deleteFile(pathTeamUser);
            }

            string pathActivity = FindZipFileLocation(getRealFileZipName(activity));

            if (pathActivity != null)
            {
                foundActivityZip = true;
                deleteFile(pathActivity);
            }

            if (foundActivityZip || foundTeamUserZip)
            {
                //we deleted a file so we got to update the records

                string recordFile;
                using (StreamReader sr = new StreamReader(getZipFilesRecords()))
                {
                    recordFile = sr.ReadToEnd();
                }

                deleteFile(getZipFilesRecords());

                string activityRealname = getRealFileZipName(activity);
                string teamUserRealname = getRealFileZipName(activity, teamUser);

                using (StreamWriter sw = new StreamWriter(getZipFilesRecords()))
                {
                    foreach (string line in recordFile.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.Trim() != "")
                        {
                            string realName = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[1];
                            if (realName != activityRealname && realName != teamUserRealname)
                            {
                                sw.WriteLine(line);
                            }
                        }
                    }
                }
            }
        }

        public static FileStream FindZipFile(AbstractAssignmentActivity activity, TeamUserMember teamUser = null)
        {
            string location = FindZipFileLocation(getRealFileZipName(activity, teamUser));
            if (location != null)
            {
                return GetDocumentForRead(location);
            }
            return null;
        }

        public static bool CreateZipFolder(ZipFile zipFile, AbstractAssignmentActivity activity, TeamUserMember teamUser = null)
        {
            RemoveOldZipFiles();
            string path = getZipFolderLocation();

            string zipFileName = GeneratedUnusedFileName(path, ".zip");

            zipFile.Save(zipFileName);

            AddEntryToZipRecords(zipFileName, getRealFileZipName(activity, teamUser), DateTime.Now);

            return true;
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

        public static string GetAssignmentActivitySubmissionFolder(Course course, int assignmentActivityID)
        {
            string path = getCoursePath(course);
            path += "Assignments" + "\\" + assignmentActivityID + "\\Submissions";
            return path;
        }

        public static string GetTeamUserSubmissionFolder(bool createPathIfNotExists, Course course, int assignmentActivityID, TeamUserMember subbmitter)
        {
            string path = GetAssignmentActivitySubmissionFolder(course, assignmentActivityID);
            path += "\\" + subbmitter.ID;

            if (!Directory.Exists(path) && createPathIfNotExists)
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public static string GetDeliverable(Course course, int assignmentActivityID, TeamUserMember subbmitter, string fileName, string[] possibleFileExtensions)
        {
            string path = GetTeamUserSubmissionFolder(false, course, assignmentActivityID, subbmitter) + "\\";

            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);

                foreach (string extension in possibleFileExtensions)
                {
                    if (files.Contains(path + fileName + extension))
                    {
                        return (path + fileName + extension);
                    }
                }
            }
            return null;
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
        /// As an add measure this function will do nothing if you are not in debug mode
        /// </summary>
        public static void WipeOutFileSystem()
        {
#if DEBUG
            EmptyFolder(getRootPath());
#endif
        }
    }
}