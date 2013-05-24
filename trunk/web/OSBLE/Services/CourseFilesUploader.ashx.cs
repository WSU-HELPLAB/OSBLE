// Created 5-21-13 by Evan Olds for the OSBLE project
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using OSBLE;
using OSBLE.Models;
using OSBLE.Models.Services.Uploader;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;
using OSBLE.Models.HomePage;
using System.ServiceModel.Activation;
using OSBLE.Models.FileSystem;

namespace OSBLE.Services
{
    /// <summary>
    /// Service that allows uploading files for a course. The intention here is to have something 
    /// that processes uploads from a form with the &lt;input type=file&gt; element on a page.
    /// </summary>
    public class CourseFilesUploader : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            HttpFileCollection coll = context.Request.Files;

            // This web service returns XML
            context.Response.ContentType = "text/xml";

            if (0 == coll.Count)
            {
                WriteErrorResponse(context,
                    "Course file upload service requires one or more files in the request. " + 
                    "It's possible that your browser did not correctly send the file data " + 
                    "and you may need to update your browser if the problem persists.");
                return;
            }

            // We're expecting the course ID to be in a parameter (required)
            string courseIDParam = context.Request.Params["courseID"];
            if (string.IsNullOrEmpty(courseIDParam))
            {
                WriteErrorResponse(context,
                    "Course file upload service requires a \"courseID\" parameter.");
                return;
            }

            // Make sure the course ID is an integer and a valid course ID at that
            int courseID;
            if (!int.TryParse(courseIDParam, out courseID))
            {
                WriteErrorResponse(context,
                    "The course ID must be a valid integer value.");
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

            // Make sure this user has permission to upload to this course
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
            if (null == courseUser || !courseUser.AbstractRole.CanUploadFiles)
            {
                // User cannot upload files for this course
                context.Response.Write(
                    "<CourseFilesUploaderResponse success=\"false\">" +
                    "  <Message>The specified user does not have access to course with ID=" + 
                        courseID.ToString() + ". User must be " +
                        "a course owner to access this service.</Message>" +
                    "</CourseFilesUploaderResponse>");
                return;
            }

            // We will look for an optional "fileusage" parameter that tells us where 
            // the file(s) will go. By default we use "generic" if the parameter is 
            // absent.
            string fileUsage = context.Request.Params["fileusage"];
            if (string.IsNullOrEmpty(fileUsage))
            {
                // Default to "generic", which puts files in the CourseDocs folder.
                fileUsage = "generic";
            }
            else
            {
                fileUsage = fileUsage.ToLower();
            }

            // Save based on the usage
            if ("generic" == fileUsage)
            {
                FileSystemBase location = (new Models.FileSystem.FileSystem()).Course(courseID).CourseDocs;
                
                // Save each file to the target directory
                for (int i = 0; i < coll.Count; i++)
                {
                    HttpPostedFile postedFile = coll[i];
                    string fileName = Path.GetFileName(postedFile.FileName);
                    location.AddFile(fileName, postedFile.InputStream);
                }

                context.Response.Write(string.Format(
                    "<CourseFilesUploaderResponse success=\"true\">" +
                    "  <Message>Successfully uploaded {0} files</Message>" +
                    "</CourseFilesUploaderResponse>", coll.Count));
                return;
            }
            else if ("assignment_description" == fileUsage ||
                "assignment_solution" == fileUsage)
            {
                // In this case we also need an assignment ID parameter
                string aIDString = context.Request.Params["assignmentID"];
                if (string.IsNullOrEmpty(aIDString))
                {
                    WriteErrorResponse(context,
                        "An \"assignmentID\" parameter is required when uploading a " +
                        "file for an assignment " +
                        (("assignment_description" == fileUsage) ? "description." : "solution."));
                    return;
                }

                int aID;
                if (!int.TryParse(aIDString, out aID))
                {
                    WriteErrorResponse(context,
                        "The \"assignmentID\" parameter must be an integer value.");
                    return;
                }

                // Assignment must exist
                Models.FileSystem.AssignmentFilePath afs =
                    (new Models.FileSystem.FileSystem()).Course(courseID).Assignment(aID);
                if (null == afs)
                {
                    WriteErrorResponse(context,
                        "Could not get assignment file path for assignment: " + aIDString);
                    return;
                }

                // Get the attributable files storage for this assignment
                OSBLE.Models.FileSystem.AttributableFilesFilePath attrFiles = afs.AttributableFiles;
                if (null == attrFiles)
                {
                    WriteErrorResponse(context,
                        "Internal error: could not get attributable files manager for assignment");
                    return;
                }

                // Set up the system attributes for this file
                Dictionary<string, string> sys = new Dictionary<string, string>();
                sys.Add("created", DateTime.Now.ToString());
                sys.Add(fileUsage, aIDString);
                sys.Add("uploadedby", up.UserName);

                // Save each file to the target directory
                for (int i = 0; i < coll.Count; i++)
                {
                    HttpPostedFile postedFile = coll[i];
                    string fileName = Path.GetFileName(postedFile.FileName);
                    attrFiles.AddFile(fileName, postedFile.InputStream, sys, null);
                }

                context.Response.Write(string.Format(
                    "<CourseFilesUploaderResponse success=\"true\">" +
                    "  <Message>Successfully uploaded {0} files</Message>" +
                    "</CourseFilesUploaderResponse>", coll.Count));
                return;
            }

            // Coming here implies we didn't recognize the file usage
            WriteErrorResponse(context, "Unsupported file usage: " + fileUsage);
        }

        private static void WriteErrorResponse(HttpContext context, string errorMessage)
        {
            context.Response.Write(string.Format(
                "<CourseFilesUploaderResponse success=\"false\">" +
                "  <Message>{0}</Message>" +
                "</CourseFilesUploaderResponse>", errorMessage));
        }

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}