using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using OSBLE;
using OSBLE.Models;
using OSBLE.Models.Services.Uploader;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;
using OSBLE.Models.HomePage;
using System.ServiceModel.Activation;

namespace OSBLE.Services
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "CourseFilesService" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select CourseFilesService.svc or CourseFilesService.svc.cs at the Solution Explorer and start debugging.
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class CourseFilesService
    {
        private AuthenticationService _authService = new AuthenticationService();
        private OSBLEContext _db = new OSBLEContext();
        
        [OperationContract]
        public ByteArrayResult GetFileByName(string file, int courseID, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new ByteArrayResult()
                {
                    Success = false,
                    ByteArray = null,
                    Message = "The authentication token passed to " +
                        System.Reflection.MethodInfo.GetCurrentMethod().Name +
                        " was invalid."
                };
            }
            UserProfile profile = _authService.GetActiveUser(authToken);

            // This is a web service, so the only people that can get files will 
            // be course owners. In other pieces of code internal to OSBLE and 
            // not exposed as a service, certain files will be accessible within 
            // the attributable storage location.

            CourseUser courseUser = (
                                      from cu in _db.CourseUsers
                                      where cu.UserProfileID == profile.ID
                                      &&
                                      cu.AbstractCourse is Course
                                      &&
                                      cu.AbstractCourseID == courseID
                                      select cu
                                      ).FirstOrDefault();
            // TODO: Make sure "CanUploadFiles" is the value to be checking
            if (null == courseUser || !courseUser.AbstractRole.CanUploadFiles)
            {
                // User cannot upload files for this course and therefore 
                // cannot download them either
                return new ByteArrayResult()
                {
                    Success = false,
                    ByteArray = null,
                    Message = "The specified user does not have access. User must be " +
                        "a course owner to access this service."
                };
            }

            // Get the main file system
            OSBLE.Models.FileSystem.FileSystem fs = new Models.FileSystem.FileSystem();
            if (null == fs)
            {
                return new ByteArrayResult()
                {
                    Success = false,
                    ByteArray = null,
                    Message = "CRITICAL ERROR: Could not get access to the primary " +
                        "OSBLE file system."
                };
            }

            // Now get the course path and make sure it's non-null
            OSBLE.Models.FileSystem.CourseFilePath cfp = fs.Course(courseID);
            if (null == cfp)
            {
                return new ByteArrayResult()
                {
                    Success = false,
                    ByteArray = null,
                    Message = "Could not find course data for course ID = " +
                        courseID.ToString() + "."
                };
            }

            // Get the attributable files storage
            OSBLE.Models.FileSystem.AttributableFilesFilePath afp = cfp.AttributableFiles;
            if (null == afp)
            {
                return new ByteArrayResult()
                {
                    Success = false,
                    ByteArray = null,
                    Message = "Could not find any files for the specified course"
                };
            }

            Stream s = afp.OpenFileRead(file);
            if (null == s)
            {
                return new ByteArrayResult()
                {
                    Success = false,
                    ByteArray = null,
                    Message = "Could not open file"
                };
            }

            // Read the whole thing into memory
            byte[] memFile = new byte[s.Length];
            s.Read(memFile, 0, memFile.Length);
            s.Close();
            s.Dispose();

            return new ByteArrayResult()
            {
                Success = true,
                ByteArray = memFile,
                Message = "Successfully retrieved file data"
            };
        }

        private StringArrayResult GetFileNamesWithAttr(string attrName, string attrValue,
            bool systemAttr, int courseID, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new StringArrayResult()
                {
                    Success = false,
                    StringArray = null,
                    Message = "The authentication token passed to " + 
                        System.Reflection.MethodInfo.GetCurrentMethod().Name +
                        " was invalid."
                };
            }
            UserProfile profile = _authService.GetActiveUser(authToken);

            // This is a web service, so the only people that can get files will 
            // be course owners. In other pieces of code internal to OSBLE and 
            // not exposed as a service, certain files will be accessible within 
            // the attributable storage location.

            CourseUser courseUser = (
                                      from cu in _db.CourseUsers
                                      where cu.UserProfileID == profile.ID
                                      &&
                                      cu.AbstractCourse is Course
                                      &&
                                      cu.AbstractCourseID == courseID
                                      select cu
                                      ).FirstOrDefault();
            // TODO: Make sure "CanUploadFiles" is the value to be checking
            if (null == courseUser || !courseUser.AbstractRole.CanUploadFiles)
            {
                // User cannot upload files for this course and therefore 
                // cannot get lists of files either
                return new StringArrayResult()
                {
                    Success = false,
                    StringArray = null,
                    Message = "The specified user does not have access. User must be " +
                        "a course owner to access this service."
                };
            }

            // Get the main file system
            OSBLE.Models.FileSystem.FileSystem fs = new Models.FileSystem.FileSystem();
            if (null == fs)
            {
                return new StringArrayResult()
                {
                    Success = false,
                    StringArray = null,
                    Message = "CRITICAL ERROR: Could not get access to the primary " +
                        "OSBLE file system."
                };
            }

            // Now get the course path and make sure it's non-null
            OSBLE.Models.FileSystem.CourseFilePath cfp = fs.Course(courseID);
            if (null == cfp)
            {
                return new StringArrayResult()
                {
                    Success = false,
                    StringArray = null,
                    Message = "Could not find course data for course ID = " + 
                        courseID.ToString() + "."
                };
            }

            // Get the attributable files storage
            OSBLE.Models.FileSystem.AttributableFilesFilePath afp = cfp.AttributableFiles;
            if (null == afp)
            {
                return new StringArrayResult()
                {
                    Success = false,
                    StringArray = null,
                    Message = "Could not find any files for the specified course"
                };
            }

            Models.FileSystem.FileCollection coll = null;
            if (systemAttr)
            {
                coll = afp.GetFilesWithSystemAttribute(attrName, attrValue);
            }
            else
            {
                coll = afp.GetFilesWithUserAttribute(attrName, attrValue);
            }
            if (null == coll)
            {
                return new StringArrayResult()
                {
                    Success = false,
                    StringArray = null,
                    Message = "Could not build a list of files with the specified attribute"
                };
            }

            // String out the paths and get just the file names
            string[] names = coll.ToArray();
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = Path.GetFileName(names[i]);
            }

            return new StringArrayResult()
            {
                Success = true,
                StringArray = names,
                Message = "Successfully obtained list of files with requested attribute"
            };
        }

        [OperationContract]
        public StringArrayResult GetFileNamesWithSysAttr(string attrName, string attrValue,
            int courseID, string authToken)
        {
            return GetFileNamesWithAttr(attrName, attrValue, true, courseID, authToken);
        }

        [OperationContract]
        public StringArrayResult GetFileNamesWithUserAttr(string attrName, string attrValue,
            int courseID, string authToken)
        {
            return GetFileNamesWithAttr(attrName, attrValue, false, courseID, authToken);
        }

        [OperationContract]
        public Result SubmitFile(string fileName, byte[] fileData,
            Dictionary<string, string> systemAttributes,
            Dictionary<string, string> userAttributes,
            int courseID, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return null;
            }
            UserProfile profile = _authService.GetActiveUser(authToken);

            CourseUser courseUser = (
                                      from cu in _db.CourseUsers
                                      where cu.UserProfileID == profile.ID
                                      &&
                                      cu.AbstractCourse is Course
                                      &&
                                      cu.AbstractCourseID == courseID
                                      select cu
                                      ).FirstOrDefault();
            // TODO: Make sure "CanUploadFiles" is the value to be checking
            if (null == courseUser || !courseUser.AbstractRole.CanUploadFiles)
            {
                // User cannot upload files for this course
                return new Result()
                {
                    Success = false,
                    Message = "The authenticated user cannot upload files for this course"
                };
            }

            OSBLE.Models.FileSystem.FileSystem fs = new Models.FileSystem.FileSystem();
            OSBLE.Models.FileSystem.CourseFilePath cfp = fs.Course(courseID);
            
            // The course path must exist
            if (null == cfp)
            {
                return new Result()
                {
                    Success = false,
                    Message = "File path for course " + courseID.ToString() +
                        " was not found."
                };
            }

            // Get the attributable files storage
            OSBLE.Models.FileSystem.AttributableFilesFilePath afp = cfp.AttributableFiles;
            if (null == afp)
            {
                return new Result()
                {
                    Success = false,
                    Message = "File path for attributed files in course " + courseID.ToString() +
                        " was not found."
                };
            }

            // Write the file
            MemoryStream ms = new MemoryStream(fileData);
            if (!afp.AddFile(fileName, ms, systemAttributes, userAttributes))
            {
                return new Result()
                {
                    Success = false,
                    Message = "Failed to upload file: \"" + fileName + "\"."
                };
            }

            // Success
            return new Result()
            {
                Success = true,
                Message = "File and attributes were successfully uploaded"
            };
        }

        public class Result
        {
            public bool Success;

            public string Message;
        }

        public class ByteArrayResult : Result
        {
            public byte[] ByteArray;
        }

        public class StringArrayResult : Result
        {
            public string[] StringArray;
        }
    }
}
