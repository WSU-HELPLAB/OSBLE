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
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace OSBLE.Areas.AssignmentDetails.Controllers
{
    public class HomeController : OSBLEController
    {
        public ActionResult Index(int assignmentId)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);

            //If the assignment does not exist, or the assignment has not been released and the user is a student: kick them out
            if (assignment == null 
                || (assignment.ReleaseDate > DateTime.UtcNow && ActiveCourseUser.AbstractRoleID == (int)OSBLE.Models.Courses.CourseRole.CourseRoles.Student))
            {
                return RedirectToRoute(new { action = "Index", controller = "Assignment", area = "" });
            }
            AssignmentDetailsFactory factory = new AssignmentDetailsFactory();
            AssignmentDetailsViewModel viewModel = factory.Bake(assignment, ActiveCourseUser);

            // E.O.: There can be files associated with an assignment description. We look 
            // for these now.
            ViewBag.DescFilesHTML = string.Empty;
            if (assignment.CourseID.HasValue)
            {
                OSBLE.Models.FileSystem.AssignmentFilePath fs =
                    OSBLE.Models.FileSystem.Directories.GetAssignment(
                        assignment.CourseID.Value, assignmentId);
                
                OSBLEDirectory attrFiles = fs.AttributableFiles;
                
                //OSBLE.Models.FileSystem.FileCollection files = 
                //    attrFiles.GetFilesWithSystemAttribute("assignment_description", assignmentId.ToString());

                List<IListBlobItem> files = new List<IListBlobItem>();
                files = fs.GetAssignmentBlobs();

                if (files.Count > 0)
                {
                    StringBuilder sb = new StringBuilder("<ul>");
                    foreach (var BlobFile in files.OfType<CloudBlob>())
                    {
                        BlobFile.FetchAttributes();
                        if (BlobFile.Metadata["FileName"] != "dir.osble" && (BlobFile.Metadata["assignment_solution"] == null))
                        {
                            // Make a link for the file
                            sb.AppendFormat(
                                "<li><a href=\"/Services/CourseFilesOps.ashx?cmd=assignment_file_download" +
                                "&courseID={0}&assignmentID={1}&filename={2}\">{2}</li>",
                                assignment.CourseID.Value,
                                assignment.ID,
                                BlobFile.Metadata["FileName"].ToString());
                        }
                    }
                    sb.Append("</ul>");
                    ViewBag.DescFilesHTML = sb.ToString();
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
