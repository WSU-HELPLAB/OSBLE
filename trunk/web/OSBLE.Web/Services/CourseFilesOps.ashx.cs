﻿// Added 5-23-13 by Evan Olds for the OSBLE project
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models;
using OSBLE.Models.Courses;
using OSBLE.Models.FileSystem;

namespace OSBLE.Services
{
    /// <summary>
    /// "Service" handler for various file operations such as getting the list of files 
    /// associated with an assignment, TODO: deleting files
    /// Does NOT handle file uploads. See CourseFilesUploader for that.
    /// </summary>
    public class CourseFilesOps : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            // This web service returns XML in most cases
            context.Response.ContentType = "text/xml";

            // We need a "cmd" parameter to tell us what to deliver
            string cmdParam = context.Request.Params["cmd"];
            if (string.IsNullOrEmpty(cmdParam))
            {
                WriteErrorResponse(context,
                    "Course file operations service requires a \"cmd\" parameter.");
                return;
            }

            // We need a "courseID" parameter
            int courseID = -1;
            if (!VerifyIntParam(context, "courseID", ref courseID))
            {
                // Can't operate without this value
                return;
            }

            // Try to get the current OSBLE user
            Models.Users.UserProfile up = OSBLE.Utility.OsbleAuthentication.CurrentUser;
            if (null == up)
            {
                // In the future what I'd like to do here is look for a user name and 
                // password in the request headers. This would allow this web service to 
                // be used by other sources, but for now it requires a logged in OSBLE user.
                WriteErrorResponse(context,
                    "Could not get active OSBLE user for request. Please login.");
                return;
            }

            // The permissions for service actions depend on the course user
            OSBLEContext db = new OSBLEContext();
            CourseUser courseUser =
                (from cu in db.CourseUsers
                 where cu.UserProfileID == up.ID &&
                 cu.AbstractCourse is Course &&
                 cu.AbstractCourseID == courseID
                 select cu).FirstOrDefault();
            if (null == courseUser)
            {
                WriteErrorResponse(context,
                    "User does not have permission to perform this action.");
                return;
            }

            // Now look at the command and handle it appropriately
            if ("course_files_list" == cmdParam)
            {
                HandleCourseFileListingRequest(context, up, courseID);
                return;
            }
            else if ("assignment_files_list" == cmdParam)
            {
                // Make sure they have access to this course. Right now we only let 
                // people who can modify the course have access to this service.
                if (!VerifyModifyPermissions(context, up, courseID)) { return; }

                // The client wants a list of files from the attributable storage location 
                // for the assignment.

                // First make sure we have the "assignmentID" parameter
                int aID = -1;
                if (!VerifyIntParam(context, "assignmentID", ref aID))
                {
                    return;
                }

                // Get the attributable file storage
                AttributableFilesPath attrFiles =
                    (new Models.FileSystem.FileSystem()).Course(courseID).Assignment(aID).AttributableFiles;
                if (null == attrFiles)
                {
                    WriteErrorResponse(context,
                        "Internal error: could not get attributable files manager for assignment");
                    return;
                }

                // Get XML file listing packaged up and return it to the client
                context.Response.Write(
                    "<CourseFilesOpsResponse success=\"true\">" +
                    attrFiles.GetXMLListing(courseUser, false) +
                    "</CourseFilesOpsResponse>");
                return;
            }
            else if ("assignment_file_download" == cmdParam)
            {
                // First make sure we have the "assignmentID" parameter
                int aID = -1;
                if (!VerifyIntParam(context, "assignmentID", ref aID))
                {
                    return;
                }

                // Now make sure we have the "filename" parameter
                string fileName = context.Request.Params["filename"];
                if (string.IsNullOrEmpty(fileName))
                {
                    WriteErrorResponse(context, "Missing required parameter: \"filename\"");
                    return;
                }
                fileName = System.IO.Path.GetFileName(fileName);

                // Get the attributable file storage
                AttributableFilesPath attrFiles =
                    (new Models.FileSystem.FileSystem()).Course(courseID).Assignment(aID).AttributableFiles;
                if (null == attrFiles)
                {
                    WriteErrorResponse(context,
                        "Internal error: could not get attributable files manager for assignment");
                    return;
                }

                // Make sure the file exists
                AttributableFile af = attrFiles.GetFile(fileName);
                if (null == af)
                {
                    WriteErrorResponse(context,
                        "Internal error: could not get attributable file");
                    return;
                }

                // Make sure the user has permission to download
                if (null == courseUser || !af.CanUserDownload(courseUser))
                {
                    WriteErrorResponse(context,
                        "User does not have permission to download this file");
                    return;
                }

                if (fileName.ToLower().EndsWith(".pdf"))
                {
                    context.Response.ContentType = "application/pdf";
                }
                else
                {
                    context.Response.ContentType = "application/octet-stream";
                }
                context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");

                // Transmit file data
                context.Response.TransmitFile(af.DataFileName);
                return;
            }
            else if ("create_folder" == cmdParam)
            {
                // Make sure they have access to this course. Right now we only let 
                // people who can modify the course have access to this service function.
                if (!VerifyModifyPermissions(context, up, courseID)) { return; }

                // Make sure the folder name parameter is present
                string folderName = string.Empty;
                if (!VerifyStringParam(context, "folder_name", ref folderName)) { return; }

                if (string.IsNullOrEmpty(folderName))
                {
                    WriteErrorResponse(context,
                        "The following parameter cannot be an empty string: folder_name");
                    return;
                }

                // Make sure the folder name is OK
                if (!VerifyFolderPath(context, ref folderName)) { return; }

                // Get the attributable file storage
                AttributableFilesPath attrFiles =
                    (new Models.FileSystem.FileSystem()).Course(courseID).CourseDocs as
                    OSBLE.Models.FileSystem.AttributableFilesPath;
                if (null == attrFiles)
                {
                    WriteErrorResponse(context,
                        "Internal error: could not get attributable files manager for course files.");
                    return;
                }

                // Combine the relative path from the request (which has been checked 
                // to make sure it's ok) with the path of the course files.
                string path = System.IO.Path.Combine(attrFiles.GetPath(), folderName);
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }

                // Return success message with new file listing
                context.Response.Write(
                    "<CourseFilesOpsResponse success=\"true\">" +
                    attrFiles.GetXMLListing(courseUser, true) +
                    "</CourseFilesOpsResponse>");
                return;
            }
            else if ("delete_folder" == cmdParam)
            {
                HandleFolderDeletionRequest(context, up, courseID);
                return;
            }
            else if ("rename_folder" == cmdParam)
            {
                // Make sure they have access to this course. Right now we only let 
                // people who can modify the course have access to this service function.
                if (!VerifyModifyPermissions(context, up, courseID)) { return; }

                // Make sure the folder name parameter is present
                string folderName = string.Empty;
                if (!VerifyStringParam(context, "folder_name", ref folderName)) { return; }

                if (string.IsNullOrEmpty(folderName))
                {
                    WriteErrorResponse(context,
                        "The following parameter cannot be an empty string: folder_name");
                    return;
                }

                // Make sure the folder name is OK
                if (!VerifyFolderPath(context, ref folderName)) { return; }

                // Get the attributable file storage
                AttributableFilesPath attrFiles =
                    (new Models.FileSystem.FileSystem()).Course(courseID).CourseDocs as
                    OSBLE.Models.FileSystem.AttributableFilesPath;
                if (null == attrFiles)
                {
                    WriteErrorResponse(context,
                        "Internal error: could not get attributable files manager for course files.");
                    return;
                }

                // Combine the relative path from the request (which has been checked 
                // to make sure it's ok) with the path of the course files.
                string path = System.IO.Path.Combine(attrFiles.GetPath(), folderName);
                if (!System.IO.Directory.Exists(path))
                {
                    // We can't rename a directory that doesn't exist
                    WriteErrorResponse(context,
                        "Error: Could not find folder to rename: " + folderName);
                    return;
                }

                // Now make sure we have the new_name parameter
                string newName = string.Empty;
                if (!VerifyStringParam(context, "new_name", ref newName)) { return; }

                // Verify that it's OK
                if (!VerifyFolderPath(context, ref newName)) { return; }
                // Also it must be just the folder name and not have / or \
                if (newName.Contains('/') || newName.Contains('\\'))
                {
                    WriteErrorResponse(context,
                        "New folder name must not contain a path, just the new folder name.");
                    return;
                }
                // Lastly, it must not be empty
                if (string.IsNullOrEmpty(newName))
                {
                    WriteErrorResponse(context,
                        "New folder name cannot be empty.");
                    return;
                }

                string newNameFull = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(path), newName);

                // Do the actual rename (move)
                System.IO.Directory.Move(path, newNameFull);

                // Do the same for the corresponding folder in the attributable 
                // files directory
                path = System.IO.Path.Combine(attrFiles.AttrFilesPath, folderName);
                if (System.IO.Directory.Exists(path))
                {
                    newNameFull = System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(path), newName);
                    System.IO.Directory.Move(path, newNameFull);
                }

                // Return success message with new file listing
                context.Response.Write(
                    "<CourseFilesOpsResponse success=\"true\">" +
                    attrFiles.GetXMLListing(courseUser, true) +
                    "</CourseFilesOpsResponse>");
                return;
            }
            
            // Coming here implies an unknown command
            WriteErrorResponse(context, "Unknown command: " + cmdParam);
        }

        private static bool VerifyCoursePermissions(HttpContext context, Models.Users.UserProfile up,
            int courseID)
        {
            OSBLEContext _db = new OSBLEContext();
            CourseUser courseUser = (
                                      from cu in _db.CourseUsers
                                      where cu.UserProfileID == up.ID
                                      &&
                                      cu.AbstractCourse is Course
                                      &&
                                      cu.AbstractCourseID == courseID
                                      select cu
                                      ).FirstOrDefault();
            if (null == courseUser)
            {
                WriteErrorResponse(context,
                    "The specified user does not have access to course with ID=" +
                    courseID.ToString() + ".");
                return false;
            }

            return true;
        }

        private static bool VerifyIntParam(HttpContext context, string paramName, ref int value)
        {
            string paramString = context.Request.Params[paramName];
            if (string.IsNullOrEmpty(paramString))
            {
                WriteErrorResponse(context, string.Format(
                    "Missing required parameter: \"{0}\".",
                    paramName));
                return false;
            }

            int val;
            if (!int.TryParse(paramString, out val))
            {
                WriteErrorResponse(context, string.Format(
                    "Parameter \"{0}\" must be an integer value.",
                    paramName));
                return false;
            }

            value = val;
            return true;
        }

        /// <summary>
        /// Verifies that a specific folder path would be ok to have in the 
        /// file system. Note that this is NOT a check to see if the specified 
        /// folder exists. It is a check to make sure the folder path has 
        /// valid characters, doesn't go up to prior directories, etc.
        /// If the string starts with the / or \ character then this will be 
        /// removed.
        /// </summary>
        private bool VerifyFolderPath(HttpContext context, ref string folderPath)
        {
            // If it starts with / or \ just strip that off
            while (folderPath.StartsWith("\\"))
            {
                folderPath = folderPath.Substring(1);
            }
            while (folderPath.StartsWith("/"))
            {
                folderPath = folderPath.Substring(1);
            }

            // Folder name can't have ..\ or ../
            if (folderPath.Contains("..\\") || folderPath.Contains("../"))
            {
                WriteErrorResponse(
                    context, "Specified folder name was not allowed.");
                return false;
            }

            // It also cannot have invalid path characters
            char[] invalid = System.IO.Path.GetInvalidPathChars();
            foreach (char ic in invalid)
            {
                if (folderPath.Contains(ic))
                {
                    WriteErrorResponse(
                        context, "Folder name contains invalid character: '" +
                        ic.ToString() + "'");
                    return  false;
                }
            }

            // Tests show that the array of invalid characters checked above will not 
            // contain the ':' character, so we check for that here.
            if (folderPath.Contains(':'))
            {
                WriteErrorResponse(
                    context, "Folder name contains invalid character: ':'");
                return false;
            }

            return true;
        }

        private static bool VerifyModifyPermissions(HttpContext context, Models.Users.UserProfile up,
            int courseID)
        {
            OSBLEContext _db = new OSBLEContext();
            CourseUser courseUser = (
                                      from cu in _db.CourseUsers
                                      where cu.UserProfileID == up.ID
                                      &&
                                      cu.AbstractCourse is Course
                                      &&
                                      cu.AbstractCourseID == courseID
                                      select cu
                                      ).FirstOrDefault();
            if (null == courseUser || !courseUser.AbstractRole.CanModify)
            {
                // User cannot modify this course
                WriteErrorResponse(context,
                    "The specified user does not have permission to modify course with ID=" +
                    courseID.ToString() + ".");
                return false;
            }

            return true;
        }

        private static bool VerifyStringParam(HttpContext context, string paramName, ref string value)
        {
            string paramString = context.Request.Params[paramName];
            if (string.IsNullOrEmpty(paramString))
            {
                WriteErrorResponse(context, string.Format(
                    "Missing required parameter: \"{0}\".",
                    paramName));
                return false;
            }

            value = paramString;
            return true;
        }

        private static void WriteErrorResponse(HttpContext context, string errorMessage)
        {
            context.Response.Write(string.Format(
                "<CourseFilesOpsResponse success=\"false\">" +
                "  <Message>{0}</Message>" +
                "</CourseFilesOpsResponse>", errorMessage));
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }

        private void HandleCourseFileListingRequest(HttpContext context, 
            Models.Users.UserProfile up, int courseID)
        {
            // Get the attributable file storage
            AttributableFilesPath attrFiles =
                (new Models.FileSystem.FileSystem()).Course(courseID).CourseDocs as
                OSBLE.Models.FileSystem.AttributableFilesPath;
            if (null == attrFiles)
            {
                WriteErrorResponse(context,
                    "Internal error: could not get attributable files manager for course files.");
                return;
            }

            // The permission-oriented attributes depend on the course user
            OSBLEContext db = new OSBLEContext();
            CourseUser courseUser =
                (from cu in db.CourseUsers
                 where cu.UserProfileID == up.ID &&
                 cu.AbstractCourse is Course &&
                 cu.AbstractCourseID == courseID
                 select cu).FirstOrDefault();
            if (null == courseUser)
            {
                WriteErrorResponse(context,
                    "User does not have permission to see files in this course.");
                return;
            }

            // Get XML file listing packaged up and return it to the client
            context.Response.Write(
                "<CourseFilesOpsResponse success=\"true\">" +
                attrFiles.GetXMLListing(courseUser, true) +
                "</CourseFilesOpsResponse>");
        }

        private void HandleFolderDeletionRequest(HttpContext context,
            Models.Users.UserProfile up, int courseID)
        {
            // Make sure they have access to this course. Right now we only let 
            // people who can modify the course have access to this service function.
            if (!VerifyModifyPermissions(context, up, courseID)) { return; }

            // The permission-oriented attributes depend on the course user
            OSBLEContext db = new OSBLEContext();
            CourseUser courseUser =
                (from cu in db.CourseUsers
                 where cu.UserProfileID == up.ID &&
                 cu.AbstractCourse is Course &&
                 cu.AbstractCourseID == courseID
                 select cu).FirstOrDefault();
            if (null == courseUser)
            {
                WriteErrorResponse(context,
                    "User does not have permission to view or modify files in this course.");
                return;
            }

            // Make sure the folder name parameter is present
            string folderName = string.Empty;
            if (!VerifyStringParam(context, "folder_name", ref folderName)) { return; }

            if (string.IsNullOrEmpty(folderName))
            {
                WriteErrorResponse(context,
                    "The following parameter cannot be an empty string: folder_name");
                return;
            }

            // Can't delete the "root"
            if ("/" == folderName || "\\" == folderName)
            {
                WriteErrorResponse(context,
                    "Cannot delete the primary course files folder.");
                return;
            }

            // Make sure the folder name is OK (doesn't go up a level with ../ or 
            // other things like that)
            if (!VerifyFolderPath(context, ref folderName)) { return; }

            // Get the attributable file storage
            AttributableFilesPath attrFiles =
                (new Models.FileSystem.FileSystem()).Course(courseID).CourseDocs as
                OSBLE.Models.FileSystem.AttributableFilesPath;
            if (null == attrFiles)
            {
                WriteErrorResponse(context,
                    "Internal error: could not get attributable files manager for course files.");
                return;
            }

            // Combine the relative path from the request (which has been checked 
            // to make sure it's ok) with the path of the course files.
            string path = System.IO.Path.Combine(attrFiles.GetPath(), folderName);
            if (!System.IO.Directory.Exists(path))
            {
                WriteErrorResponse(context,
                    "Folder could not be deleted because it does not exist on the server.");
                return;
            }

            // Delete this folder and everything inside it
            System.IO.Directory.Delete(path, true);

            // Return success message with new file listing
            context.Response.Write(
                "<CourseFilesOpsResponse success=\"true\">" +
                attrFiles.GetXMLListing(courseUser, true) +
                "</CourseFilesOpsResponse>");
        }
    }
}