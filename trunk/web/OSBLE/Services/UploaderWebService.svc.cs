using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.Web;
using System.Web.Security;

using OSBLE;
using OSBLE.Models;
using OSBLE.Models.Services.Uploader;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;

namespace OSBLE.Services
{
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class UploaderWebService
    {
        private string filePath;
        private string currentpath;
        private const double sessionTimeoutInMinutes = -15.0;
        private static Dictionary<string, UserSession> activeSessions = new Dictionary<string,UserSession>();

        private OSBLEContext db = new OSBLEContext();

        public UploaderWebService()
        {
            filePath = FileSystem.RootPath;
            currentpath = filePath;
        }

        /// <summary>
        /// Call this to remove expired sessions.  This
        /// function will extend the keep alive timer on the supplied key.
        /// </summary>
        /// <param name="authKey">The key to extend.</param>
        private void CleanActiveSessions(string authKey)
        {

            //update the current key
            if (activeSessions.Keys.Contains(authKey))
            {
                activeSessions[authKey].LastAccessTime = DateTime.Now;
            }

            //call the normal clean function
            CleanActiveSessions();
        }

        /// <summary>
        /// Call this at the end of every request to remove expired sessions
        /// </summary>
        private void CleanActiveSessions()
        {
            //set an expiration time of 15 minutes ago
            DateTime expirationDate = DateTime.Now.AddMinutes(sessionTimeoutInMinutes);
            List<string> expiredKeys = new List<string>();
            foreach (string key in activeSessions.Keys)
            {
                //log any expired keys
                if (activeSessions[key].LastAccessTime < expirationDate)
                {
                    //note that we can't modify a collection that we are iterating through
                    //so we must add to another collection first
                    expiredKeys.Add(key);
                }
            }

            //remove expired keys
            foreach (string key in expiredKeys)
            {
                activeSessions.Remove(key);
            }
        }

        /// <summary>
        /// A hack-ish way to get clients to recognize the FileListing class
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        public FileListing GetFakeFileListing()
        {
            return new FileListing();
        }

        /// <summary>
        /// A hack-ish way to get clients to recognize the DirectoryListing class
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        public DirectoryListing GetFakeDirectoryListing()
        {
            return new DirectoryListing();
        }

        /// <summary>
        /// A hack-ish way to get clients to recognize the ParentDirectoryListing class
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        public ParentDirectoryListing GetFakeParentDirectoryListing()
        {
            return new ParentDirectoryListing();
        }

        /// <summary>
        /// Returns a list of files and directories for the given path
        /// </summary>
        /// <param name="relativepath"></param>
        /// <returns></returns>
        private DirectoryListing BuildFileList(string relativepath)
        {

            //build a new listing, set some initial values
            DirectoryListing listing = new DirectoryListing();
            string currentpath = Path.Combine(filePath, relativepath);
            listing.Name = relativepath;
            listing.LastModified = File.GetLastWriteTime(currentpath);

            //handle files
            foreach (string file in Directory.GetFiles(currentpath))
            {
                FileListing fList = new FileListing();
                fList.Name = Path.GetFileName(file);
                fList.LastModified = File.GetLastWriteTime(file);
                listing.Files.Add(fList);
            }

            //Add a parent directory "..." at the top of every directory listing
            listing.Directories.Add(new ParentDirectoryListing());

            //handle other directories
            foreach (string folder in Directory.EnumerateDirectories(currentpath))
            {
                //recursively build the directory's subcontents.  Note that we have
                //to pass only the folder's name and not the complete path
                listing.Directories.Add(BuildFileList(folder.Substring(folder.LastIndexOf('\\') + 1)));
            }

            //return the completed listing
            return listing;
        }

        [OperationContract]
        public DirectoryListing GetFileList(int courseId, string authKey)
        {
            //only continue if we have a valid authentication key
            if (!IsValidKey(authKey))
            { 
                return new DirectoryListing();
            }

            //pull the current user for easier access
            UserProfile currentUser = activeSessions[authKey].UserProfile;

            //find the current course
            CoursesUsers cu = (from c in db.CoursesUsers
                               where c.CourseID == courseId && c.UserProfileID == currentUser.ID
                               select c).FirstOrDefault();
            if (cu != null)
            {
                if (cu.CourseRole.CanModify)
                {
                    return BuildFileList(FileSystem.GetCourseDocumentsPath(cu.Course as Course));
                }
            }
            return new DirectoryListing();
        }

        [OperationContract]
        public string GetFileUrl(string fileName)
        {
            //probably need to make sure that this string is web-accessible, but this is fine for testing
            string file = Path.Combine(filePath, Path.GetFileName(fileName));
            return file;
        }

        /// <summary>
        /// Returns a list locations that the current user can upload to.
        /// </summary>
        /// <param name="authKey"></param>
        /// <returns></returns>
        [OperationContract]
        public Dictionary<int, string> GetValidUploadLocations(string authKey)
        {

            //only continue if we have a valid authentication key
            if (!IsValidKey(authKey))
            {
                return new Dictionary<int,string>();
            }

            //stores the list of possible upload locations
            Dictionary<int, string> uploadLocations = new Dictionary<int, string>();

            //pull the current user for easier access
            UserProfile currentUser = activeSessions[authKey].UserProfile;

            //find all courses that the users is associated with
            List<CoursesUsers> courses = (from course in db.Courses
                                          join cu in db.CoursesUsers on course.ID equals cu.CourseID
                                          where
                                            course is Course
                                            &&
                                            course.Inactive == false
                                            &&
                                            cu.Hidden == false
                                            &&
                                            cu.UserProfileID == currentUser.ID
                                          select cu).ToList();
            foreach (CoursesUsers cu in courses)
            {
                if (cu.CourseRole.CanModify)
                {
                    uploadLocations.Add(cu.CourseID, String.Format("\"{0}\" Links", cu.Course.Name));
                }
            }
            return uploadLocations;
        }

        /// <summary>
        /// Will tell you if the supplied key is valid
        /// </summary>
        /// <param name="authKey"></param>
        /// <returns></returns>
        private bool IsValidKey(string authKey)
        {
            //clean our session list
            CleanActiveSessions();

            //after cleaning, all remaining keys should be valid
            if (activeSessions.Keys.Contains(authKey))
            {
                //if the key exists, might as well update it as well
                activeSessions[authKey].LastAccessTime = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        [OperationContract]
        public bool SyncFile(string fileName, byte[] data)
        {
            //uploads need to handle a check for lastmodified date
            string file = Path.Combine(filePath, fileName);
            using (FileStream fs = new FileStream(file, FileMode.Create))
            {
                fs.Write(data, 0, (int)data.Length);
            }
            return true;
        }

        [OperationContract]
        public bool PrepCurrentPath(DirectoryListing fileList)
        {
            string directory;
            for (int i = 0; i < fileList.Directories.Count; ++i)
            {
                // creates directory
                if (fileList.Directories[i].Name != "...")
                {
                    directory = Path.Combine(filePath, fileList.Directories[i].Name);
                    Directory.CreateDirectory(directory);
                }
            }
            return true;
        }

        /// <summary>
        /// Validates the supplied user/pass combination.  
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns>A unique key needed to make most web service calls.</returns>
        [OperationContract]
        public string ValidateUser(string userName, string password)
        {
            //clean up the active sessions table
            CleanActiveSessions();

            if (Membership.ValidateUser(userName, password))
            {
                UserProfile profile = (from p in db.UserProfiles
                                       where p.UserName == userName
                                       select p).First();

                //build our string to hash
                string email = profile.UserName;
                string date = DateTime.Now.ToLongTimeString();
                string hashString = email + date;

                //compute the hash
                SHA1Managed sha1 = new SHA1Managed();
                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                string hash = encoding.GetString(sha1.ComputeHash(encoding.GetBytes(hashString)));

                //save the hash for validating later calls
                activeSessions.Add(hash, new UserSession(profile));

                //return the hash to the caller
                return hash;
            }
            return "";
        }
    }
}
