﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Hosting;
using Ionic.Zip;
using OSBLE.Models.Courses;
using OSBLE.Models.Services.Uploader;
using OSBLE.Models.Users;
using OSBLE.Models.Assignments;

namespace OSBLE
{
    // *** DO NOT USE THIS CLASS. IT IS IN THE PROCESS OF BEING REMOVED, 
    // BUT THIS IS A LARGE TASK SO SOME CODE STILL LINGERS UNTIL THE 
    // REFACTORING IS COMPLETE ***
    [Obsolete("Use objects in the Models.FileSystem namespace")]
    public class FileSystem
    { 

        #region old FileSystem Code (deprecated)


        public static string RootPath
        {
            get
            {
                return ConfigurationManager.AppSettings["OSBLEFileSystem"];
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
                fList.FileUrl = CourseDocumentPathToWebUrl(file);

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

        private static string getZipFolderLocation(Course course)
        {
            return OSBLE.Models.FileSystem.Directories.GetCourseZipFolder(course.ID).GetPath();
        }

        private static string getZipFilesRecords(Course course)
        {
            return getZipFolderLocation(course) + "\\" + "Records.txt";
        }

        private static void RemoveOldZipFiles(Course course)
        {
            string records = getZipFilesRecords(course);
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
                                if (dt < DateTime.UtcNow)
                                {
                                    FileInfo zipFile = new FileInfo(getZipFolderLocation(course) + "\\" + lineSections[1]);
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

                DirectoryInfo info = new DirectoryInfo(getZipFolderLocation(course));
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

        private static void AddEntryToZipRecords(Course course, string zipFileName, string realName, DateTime created)
        {
            using (StreamWriter sw = new StreamWriter(getZipFilesRecords(course), true))
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

        private static string FindZipFileLocation(Course course, string realName)
        {
            try
            {
                using (StreamReader sr = new StreamReader(getZipFilesRecords(course)))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.Trim() != "")
                        {
                            string[] lineSections = line.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                            if (lineSections[1] == realName)
                            {
                                return getZipFolderLocation(course) + "\\" + lineSections[0];
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

        private static string getRealFileZipName(Assignment activity, AssignmentTeam team = null)
        {
            string s = "assignmentID = " + activity.ID.ToString();
            if (team != null)
            {
                s += " TeamID = " + team.TeamID.ToString();
            }
            return s;
        }

        public static void RemoveZipFile(Course course, Assignment assignment, AssignmentTeam team)
        {
            bool foundTeamUserZip = false, foundActivityZip = false;
            string pathTeamUser = FindZipFileLocation(course, getRealFileZipName(assignment, team));

            if (pathTeamUser != null)
            {
                foundTeamUserZip = true;
                File.Delete(pathTeamUser);
            }

            string pathActivity = FindZipFileLocation(course, getRealFileZipName(assignment));

            if (pathActivity != null)
            {
                foundActivityZip = true;
                File.Delete(pathActivity);
            }

            if (foundActivityZip || foundTeamUserZip)
            {
                //we deleted a file so we got to update the records

                string recordFile;
                using (StreamReader sr = new StreamReader(getZipFilesRecords(course)))
                {
                    recordFile = sr.ReadToEnd();
                }

                File.Delete(getZipFilesRecords(course));

                string assignmentRealname = getRealFileZipName(assignment);
                string teamRealname = getRealFileZipName(assignment, team);

                using (StreamWriter sw = new StreamWriter(getZipFilesRecords(course)))
                {
                    foreach (string line in recordFile.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (line.Trim() != "")
                        {
                            string realName = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[1];
                            if (realName != assignmentRealname && realName != teamRealname)
                            {
                                sw.WriteLine(line);
                            }
                        }
                    }
                }
            }
        }

        public static string GetReviewFolderLocation(Course course, int assignmentID)
        {
            return GetAssignmentIDFolder(course, assignmentID);
        }

        /// <summary>
        /// This gets the path and if it does not exist it will create it
        /// </summary>
        /// <param name="course"></param>
        /// <param name="abstractAssignmentActivityID"></param>
        /// <param name="teamUserID"></param>
        /// <returns></returns>
        public static string GetTeamUserReviewFolderLocation(bool createPathIfNotExists, Course course, int assignmentID, int teamID)
        {
            string path = GetReviewFolderLocation(course, assignmentID) + "\\Reviews\\" + teamID;
            DirectoryInfo info = new DirectoryInfo(path);

            if (!info.Exists && createPathIfNotExists)
            {
                info.Create();
            }
            return path;
        }

        public static FileStream FindZipFile(Course course, Assignment assignment, AssignmentTeam team = null)
        {
            string location = FindZipFileLocation(course, getRealFileZipName(assignment, team));
            if (location != null)
            {
                return GetDocumentForRead(location);
            }
            return null;
        }

        public static bool CreateZipFolder(Course course, ZipFile zipFile, Assignment assignment, AssignmentTeam team = null)
        {
            RemoveOldZipFiles(course);
            string path = getZipFolderLocation(course);

            string zipFileName = GeneratedUnusedFileName(path, ".zip");

            zipFile.Save(zipFileName);

            AddEntryToZipRecords(course, zipFileName, getRealFileZipName(assignment, team), DateTime.UtcNow);

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
            string path = Models.FileSystem.Directories.GetCourseDocs(course.ID).GetPath();
            return BuildFileList(path, includeParentLink);
        }

        public static string GetCourseDocumentsPath(int courseId)
        {
            return Models.FileSystem.Directories.GetCourseDocs(courseId).GetPath();
        }

        public static string GetAssignmentIDFolder(Course course, int assignmentID)
        {
            return OSBLE.Models.FileSystem.Directories.GetAssignment(course.ID, assignmentID).GetPath();
        }

        public static string GetAssignmentSubmissionFolder(Course course, int assignmentID)
        {
            return GetAssignmentIDFolder(course, assignmentID) + "\\Submissions";
        }

        public static string GetTeamUserSubmissionFolder(bool createPathIfNotExists, Course course, int assignmentID, IAssignmentTeam submitterTeam)
        {
            string path = GetAssignmentSubmissionFolder(course, assignmentID);
            path += "\\" + submitterTeam.TeamID.ToString();

            if (!Directory.Exists(path) && createPathIfNotExists)
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }

        public static string GetTeamUserSubmissionFolderForAuthorID(bool createPathIfNotExists, 
            Course course, 
            int assignmentID, 
            IAssignmentTeam submitterTeam, 
            Team authorTeam)
        {
            //string path = GetTeamUserSubmissionFolder(false, course, assignmentID, submitterTeam);
            //path += "\\" + authorTeam.Name.ToString();

            //if (!Directory.Exists(path) && createPathIfNotExists)
            //{
            //    Directory.CreateDirectory(path);
            //}

            string path = OSBLE.Models.FileSystem.Directories.GetAssignment(
                course.ID, assignmentID)
                .Review(authorTeam.ID, submitterTeam.TeamID)
                .GetPath();

            return path;
        }

        public static string GetDeliverable(Course course, int assignmentID, AssignmentTeam subbmitterTeam, string fileName)
        {
            return GetTeamUserSubmissionFolder(false, course, assignmentID, subbmitterTeam) + "\\" + fileName;
        }

        public static string GetDeliverable(Course course, int assignmentID, AssignmentTeam subbmitterTeam, string fileName, string[] possibleFileExtensions)
        {
            string path = GetTeamUserSubmissionFolder(false, course, assignmentID, subbmitterTeam) + "\\";

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
            //Get the last directory declared within the Web.Config string for the FileSystem
            string fileSystemPath = ConfigurationManager.AppSettings["FileSystemDirectory"];
            string[] dirs =  fileSystemPath.Split(new char[]{'\\','/'}).Where(s => s.Length > 0).ToArray();
            int startOfWebPath = filePath.IndexOf(dirs.Last(), StringComparison.Ordinal);

            //get the raw url (not web accessible due to MVC restrictions)
            string rawUrl = VirtualPathUtility.ToAbsolute("~/" + filePath.Substring(startOfWebPath));

            //I thought about using regex here, but this seemed easier
            string[] rawPieces = rawUrl.Split('/');

            //from the pieces, we need the course Id and the name of the document
            int courseId = Convert.ToInt32(rawPieces[3]);
            string[] docPath = new string[rawPieces.Length - 5];
            Array.Copy(rawPieces, 5, docPath, 0, rawPieces.Length - 5);
            string path = string.Join("/", docPath);
            path =  HttpUtility.UrlEncode(path);

            //finally, we can build a web-accessible url
            string url = String.Format("/FileHandler/CourseDocument?courseId={0}&filePath={1}", courseId, path);
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

        public static Stream GetProfilePictureOrDefault(UserProfile userProfile)
        {
            if (null != userProfile.ProfileImage)
            {
                return new System.IO.MemoryStream(userProfile.ProfileImage.Picture);
            }
            
            return GetDefaultProfilePicture();
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

        /// <summary>
        /// Returns the last submit time for a particular submission
        /// </summary>
        /// <param name="team"></param>
        /// <returns></returns>
        public static DateTime? GetSubmissionTime(IAssignmentTeam team, Team authorTeam = null)
        {
            DateTime? timeSubmitted = null;
            if (team != null)
            {
                

                DirectoryInfo submissionFolder;
                if (team.Assignment.Type == AssignmentTypes.CriticalReview && authorTeam != null)
                {
                    submissionFolder = new DirectoryInfo
                                                (FileSystem.GetTeamUserSubmissionFolderForAuthorID
                                                    (false, team.Assignment.Course, team.AssignmentID, team, authorTeam)
                                                );
                }
                else
                {
                    submissionFolder = new DirectoryInfo
                                            (FileSystem.GetTeamUserSubmissionFolder
                                                (
                                                    false,
                                                    team.Assignment.Course,
                                                    team.Assignment.ID,
                                                    team
                                                )
                                            );
                }

                if (submissionFolder != null && submissionFolder.Exists && submissionFolder.GetFiles().Count() > 0)
                {
                    //unfortunately LastWriteTime for a directory does not take into account it's file or
                    //sub directories and these we need to check to see when the last file was written too.
                    timeSubmitted = submissionFolder.LastWriteTime;
                    foreach (FileInfo file in submissionFolder.GetFiles())
                    {
                        if (file.LastWriteTime > timeSubmitted)
                        {
                            timeSubmitted = file.LastWriteTimeUtc;
                        }
                    }
                }
            }
            return timeSubmitted;
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
            EmptyFolder(RootPath);
#endif
        }
    }
        #endregion
}
