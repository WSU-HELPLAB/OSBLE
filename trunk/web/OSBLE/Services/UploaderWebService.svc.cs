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
        /// <param name="authToken">The key to extend.</param>
        private void CleanActiveSessions(string authToken)
        {

            //update the current key
            if (activeSessions.Keys.Contains(authToken))
            {
                activeSessions[authToken].LastAccessTime = DateTime.Now;
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
        /// Removes the supplied file from the selected course
        /// </summary>
        /// <param name="path"></param>
        /// <param name="courseId"></param>
        /// <param name="authToken"></param>
        /// <returns>True if a success.  False otherwise</returns>
        [OperationContract]
        public bool DeleteFile(string file, int courseId, string authToken)
        {
            if (!IsValidKey(authToken))
            {
                return false;
            }

            //pull the current user for easier access
            UserProfile currentUser = activeSessions[authToken].UserProfile;

            //make sure that the selected user has write privileges for the supplied course
            CoursesUsers currentCourse = (from cu in db.CoursesUsers
                                         where cu.CourseID == courseId && cu.UserProfileID == currentUser.ID
                                         select cu).FirstOrDefault();
            
            //make sure that we got something back
            if (currentCourse != null)
            {
                //only allow those that can modify the course (probably instructors) to remove
                //files
                if(currentCourse.CourseRole.CanModify == true)
                {
                    //do a simple pattern match to make sure that the file to be uploaded is in
                    //the correct course folder
                    string coursePath = FileSystem.GetCourseDocumentsPath(courseId);
                    if (file.IndexOf(coursePath) > -1)
                    {
                        if (Directory.Exists(file))
                        {
                            FileSystem.EmptyFolder(file);
                            Directory.Delete(file);
                            return true;
                        }
                        else if (File.Exists(file))
                        {
                            File.Delete(file);
                            return true;
                        }
                    }
                }
            }
            return false;
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
        /// Returns a list of files for the current course
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public DirectoryListing GetFileList(int courseId, string authToken)
        {
            //only continue if we have a valid authentication key
            if (!IsValidKey(authToken))
            { 
                return new DirectoryListing();
            }

            //pull the current user for easier access
            UserProfile currentUser = activeSessions[authToken].UserProfile;

            //find the current course
            CoursesUsers cu = (from c in db.CoursesUsers
                               where c.CourseID == courseId && c.UserProfileID == currentUser.ID
                               select c).FirstOrDefault();
            if (cu != null)
            {
                if (cu.CourseRole.CanModify)
                {
                    return FileSystem.GetCourseDocumentsFileList(cu.Course as Course, true);
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
        /// Returns a datetime of when the file was last modified
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="courseId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public DateTime GetLastModifiedDate(string fileName, int courseId, string authToken)
        {
            if (!IsValidKey(authToken))
            {
                return DateTime.FromFileTime(0L);
            }

            string file = Path.Combine(FileSystem.GetCourseDocumentsPath(courseId), fileName);
            if (File.Exists(file))
            {
                return File.GetLastWriteTime(file);
            }
            else
            {
                return DateTime.FromFileTime(0L);
            }
        }

        /// <summary>
        /// Returns a list locations that the current user can upload to.
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public Dictionary<int, string> GetValidUploadLocations(string authToken)
        {

            //only continue if we have a valid authentication key
            if (!IsValidKey(authToken))
            {
                return new Dictionary<int,string>();
            }

            //stores the list of possible upload locations
            Dictionary<int, string> uploadLocations = new Dictionary<int, string>();

            //pull the current user for easier access
            UserProfile currentUser = activeSessions[authToken].UserProfile;

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
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public bool IsValidKey(string authToken)
        {
            //clean our session list
            CleanActiveSessions();

            //after cleaning, all remaining keys should be valid
            if (activeSessions.Keys.Contains(authToken))
            {
                //if the key exists, might as well update it as well
                activeSessions[authToken].LastAccessTime = DateTime.Now;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Synces one file to the server if the user is valid
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        /// <param name="courseId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public bool SyncFile(string fileName, byte[] data, int courseId, string authToken)
        {
            if (!IsValidKey(authToken))
            {
                return false;
            }

            string file = Path.Combine(FileSystem.GetCourseDocumentsPath(courseId), fileName);

            using (FileStream fs = new FileStream(file, FileMode.Create))
            {
                fs.Write(data, 0, (int)data.Length);
            }

            return true; 
        }

        /// <summary>
        /// Synces all the directories on the current level if the user is valid
        /// </summary>
        /// <param name="dirList"></param>
        /// <param name="relative"></param>
        /// <param name="courseId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public bool PrepCurrentPath(DirectoryListing dirList, int courseId, string authToken)
        {
            if (!IsValidKey(authToken))
            {
               return false;
            }
            FileSystem.PrepCourseDocuments(dirList, courseId);
            return true;
        }

        /// <summary>
        /// Updates the supplied directory listing's internal ordering
        /// </summary>
        /// <param name="listing"></param>
        /// <param name="courseId"></param>
        /// <param name="authToken"></param>
        [OperationContract]
        public void UpdateListingOrder(DirectoryListing listing, int courseId, string authToken)
        {
            if (!IsValidKey(authToken))
            {
                return;
            }

            //pull the current user for easier access
            UserProfile currentUser = activeSessions[authToken].UserProfile;

            //make sure that the selected user has write privileges for the supplied course
            CoursesUsers currentCourse = (from cu in db.CoursesUsers
                                         where cu.CourseID == courseId && cu.UserProfileID == currentUser.ID
                                         select cu).FirstOrDefault();
            
            //make sure that we got something back
            if (currentCourse != null)
            {
                if (currentCourse.CourseRole.CanModify)
                {
                    FileSystem.UpdateFileOrdering(listing);
                }
            }
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
