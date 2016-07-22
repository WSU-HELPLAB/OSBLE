using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Controllers;
using OSBLE.Areas.AssignmentDetails.ViewModels;
using OSBLE.Attributes;
using OSBLE.Models.FileSystem;
using OSBLE.Utility;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentDetails.Controllers
{
    public class HomeController : OSBLEController
    {
        public ActionResult Index(int assignmentId)
        {
            Course course = db.AbstractCourses.Where(ac => ac.ID == ActiveCourseUser.AbstractCourseID).FirstOrDefault() as Course;
            ViewBag.HideMail = course.HideMail;

            Assignment assignment = db.Assignments.Find(assignmentId);

            //If the assignment does not exist, or the assignment has not been released and the user is a student: kick them out
            if (assignment == null 
                || (assignment.ReleaseDate > DateTime.UtcNow && ActiveCourseUser.AbstractRoleID == (int)OSBLE.Models.Courses.CourseRole.CourseRoles.Student))
            {
                return RedirectToRoute(new { action = "Index", controller = "Assignment", area = "" });
            }
            AssignmentDetailsFactory factory = new AssignmentDetailsFactory();
            AssignmentDetailsViewModel viewModel = factory.Bake(assignment, ActiveCourseUser);

            // E.O.: There can be files associated with an assignment description and solutions. We look 
            // for these now.
            ViewBag.DescFilesHTML = string.Empty;
            ViewBag.SoluFilesHTML = string.Empty;
            if (assignment.CourseID.HasValue)
            {
                OSBLE.Models.FileSystem.AssignmentFilePath fs =
                    OSBLE.Models.FileSystem.Directories.GetAssignment(
                        assignment.CourseID.Value, assignmentId);
                OSBLEDirectory attrFiles = fs.AttributableFiles;
                OSBLE.Models.FileSystem.FileCollection files = 
                    attrFiles.GetFilesWithSystemAttribute("assignment_description", assignmentId.ToString());
                if (files.Count > 0)
                {
                    StringBuilder sb = new StringBuilder("<ul>");
                    foreach (string fileName in files)
                    {
                        //Check to see if the user is an admin
                        if (CurrentUser.CanCreateCourses == true)
                        {                                  
                            //Build the URL action for deleting
                            //Assignment file deletion is handled different.
                            string UrlAction = Url.Action("DeleteAssignmentFile", "Home", new { courseID = assignment.CourseID.Value, assignmentID = assignment.ID, fileName = System.IO.Path.GetFileName(fileName) });
                            
                            // Make a link for the file
                            sb.AppendFormat(
                                "<li><a href=\"/Services/CourseFilesOps.ashx?cmd=assignment_file_download" +
                                "&courseID={0}&assignmentID={1}&filename={2}\">{2}      </a>" + 
                                "<a href=\"" + UrlAction + "\"><img src=\"/Content/images/delete_up.png\" alt=\"Delete Button\"></img></a>" +                             
                                "</li>",
                                assignment.CourseID.Value,
                                assignment.ID,
                                System.IO.Path.GetFileName(fileName));
                        }
                        else
                        {
                            sb.AppendFormat(
                            "<li><a href=\"/Services/CourseFilesOps.ashx?cmd=assignment_file_download" +
                            "&courseID={0}&assignmentID={1}&filename={2}\">{2}</li>",
                            assignment.CourseID.Value,
                            assignment.ID,
                            System.IO.Path.GetFileName(fileName));
                        }
                    }
                    sb.Append("</ul>");
                    ViewBag.DescFilesHTML = sb.ToString();                   
                }
                
                //Check for solution files and if they exist create a string to send to the assignment details
                attrFiles = fs.AttributableFiles;
                files = attrFiles.GetFilesWithSystemAttribute("assignment_solution", assignmentId.ToString());

                //Check active course user

                /////////////////////////
                // This is checking hard coded AbstractCourseID values,
                // needs to check if the User is enrolled in the current course ID
                // this will cause errors in the future, noted for now
                //////////////////////////
                
                //if (files.Count > 0 && (ActiveCourseUser.AbstractCourseID == 1 ||ActiveCourseUser.AbstractCourseID == 2 ||
                //    ActiveCourseUser.AbstractCourseID == 3 || ActiveCourseUser.AbstractCourseID == 5))
                if(currentCourses.Contains(ActiveCourseUser))
                {
                    bool pastCourseDueDate = DBHelper.AssignmentDueDatePast(assignmentId,
                        ActiveCourseUser.AbstractCourseID);
                    DateTime? CourseTimeAfterDueDate = DBHelper.AssignmentDueDateWithLateHoursInCourseTime(assignmentId,
                        ActiveCourseUser.AbstractCourseID);
                    
                    StringBuilder sb;

                    // make sure we don't have an incorrect assignmentId
                    if (CourseTimeAfterDueDate != null)
                    {
                        sb = new StringBuilder("<ul>");

                        foreach (string fileName in files)
                        {
                            //Check to see if the user can modify the course
                            if (ActiveCourseUser.AbstractRole.CanModify)
                            {
                                //Build the URL action for deleting
                                //Assignment file deletion is handled different.
                                string UrlAction = Url.Action("DeleteAssignmentFile", "Home", new { courseID = assignment.CourseID.Value, assignmentID = assignment.ID, fileName = System.IO.Path.GetFileName(fileName) });

                                string fName = System.IO.Path.GetFileName(fileName);
                                // Make a link for the file
                                sb.AppendFormat(
                                    "<li><a href=\"/Services/CourseFilesOps.ashx?cmd=assignment_file_download" +
                                    "&courseID={0}&assignmentID={1}&filename={2}\">{3}      </a>" +
                                    "<a href=\"" + UrlAction + "\"><img src=\"/Content/images/delete_up.png\" alt=\"Delete Button\"></img></a>" +
                                    "</li>",
                                    assignment.CourseID.Value,
                                    assignment.ID,
                                    fName, 
                                    fName + " (Viewable after " + CourseTimeAfterDueDate + ")");
                            }
                            else if (!pastCourseDueDate)
                            {
                                sb.AppendFormat(
                                    "<li>Viewable after {0}", CourseTimeAfterDueDate);
                            }
                            // past due date, give link to the file
                            else
                            {
                                sb.AppendFormat(
                                "<li><a href=\"/Services/CourseFilesOps.ashx?cmd=assignment_file_download" +
                                "&courseID={0}&assignmentID={1}&filename={2}\">{2}</li>",
                                assignment.CourseID.Value,
                                assignment.ID,
                                System.IO.Path.GetFileName(fileName));
                            }
                        }
                        sb.Append("</ul>");
                        ViewBag.SoluFilesHTML = sb.ToString();
                    }
                    
                }

            }

            //discussion assignments and critical reviews require their own special view
            if (assignment.Type == AssignmentTypes.TeamEvaluation)
            {
                return View("TeamEvaluationIndex", viewModel);
            }
            else if (assignment.Type == AssignmentTypes.DiscussionAssignment ||
                    assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
            {
                return View("DiscussionAssignmentIndex", viewModel);
            }
            //MG&MK: For teamevaluation assignments, assignment details uses the 
            //previous assingment teams for displaying. So, we are forcing 
            //TeamEvaluation assignment to use TeamIndex.
            else if (assignment.HasTeams || assignment.Type == AssignmentTypes.TeamEvaluation)
            {
                return View("TeamIndex", viewModel);
            }
            else
            {
                return View("Index", viewModel);
            }
        }

        [CanModifyCourse]
        public ActionResult ToggleDraft(int assignmentId)
        {
            new AssignmentController().ToggleDraft(assignmentId);
            return Index(assignmentId);
        }

        //For deleting an assignment file.
        [CanModifyCourse]
        public ActionResult DeleteAssignmentFile(int courseID, int assignmentID, string fileName)
        {
            //Get the filename from the path
            fileName = System.IO.Path.GetFileName(fileName);

            // Get the attributable file storage
            OSBLEDirectory attrFiles =
               OSBLE.Models.FileSystem.Directories.GetAssignment(courseID, assignmentID).AttributableFiles;
            
            //If no files exist return to the assignment details page.
            if (null == attrFiles)
            {
                return Index(assignmentID);
            }

            if (fileName != null)
            {
                int slashIndex = fileName.LastIndexOf('\\');
                if (-1 == slashIndex)
                {
                    slashIndex = fileName.LastIndexOf('/');
                }
                if (-1 != slashIndex)
                {
                    // If the file exists in some nested folders then get the 
                    // correct directory object first.
                    attrFiles = (OSBLEDirectory)attrFiles.GetDir(fileName.Substring(0, slashIndex));

                    // Also remove the path from the beginning of the file name
                    fileName = fileName.Substring(slashIndex + 1);
                }
            }

            // Perform the actual deletion
            attrFiles.DeleteFile(fileName);

            //Return to the assignment details page
            return Index(assignmentID);
        }

        [CanModifyCourse]
        public ActionResult DeleteAssignment(int assignmentId)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);
            db.Assignments.Remove(assignment);
            db.SaveChanges();
            return RedirectToRoute(new { action = "Index", controller = "Assignment", area = "" });
        }

        [HttpPost]
        public ActionResult SaveABET()
        {
            // Get the file storage for this assignment's submissions
            OSBLE.Models.FileSystem.AssignmentFilePath afs =
                OSBLE.Models.FileSystem.Directories.GetAssignment(
                    Convert.ToInt32(Request.Form["hdnCourseID"]),
                    Convert.ToInt32(Request.Form["hdnAssignmentID"]));

            foreach (string key in Request.Form.AllKeys)
            {
                if (key.StartsWith("slctProficiency"))
                {
                    // The key will end with the team ID
                    int teamID;
                    if (!int.TryParse(key.Substring(key.IndexOf('y') + 1), out teamID))
                    {
                        continue;
                    }

                    // Get the submission for the team
                    OSBLE.Models.FileSystem.OSBLEDirectory attrFP =
                        afs.Submission(teamID) as OSBLE.Models.FileSystem.OSBLEDirectory;
                    OSBLEFile file = attrFP.FirstFile;
                    if (null == file)
                    {
                        continue;
                    }

                    // Update the attribute for the submission and save
                    file.ABETProficiencyLevel = Request.Form[key];
                    file.SaveAttrs();
                }
            }

            // Send the user back to the assignments index page
            return RedirectToRoute(new { action = "Index", controller = "Assignment", area = "" });
        }
    }
}
