using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Areas.ABETTagging.Models;
using OSBLE.Models.Assignments;
using OSBLE.Controllers;
using OSBLE.Areas.AssignmentDetails.ViewModels;
using OSBLE.Attributes;
using OSBLE.Models.FileSystem;

namespace OSBLE.Areas.ABETTagging.Controllers
{
    public class ABETController : OSBLEController
    {
        //
        // GET: /ABETTagging/Home/

        public ActionResult Index(int assignmentID)
        {
            Assignment assignment = db.Assignments.Find(assignmentID);

            // Initialize the model that we will use to pass data to 
            // the view.
            ABETTaggingViewModel model = new ABETTaggingViewModel(
                assignment, ActiveCourseUser.AbstractRoleID);

            // TODO: Figure out how to use this. We don't want students getting 
            // access to assignments that haven't been released.
            /*
            if (assignment.ReleaseDate > DateTime.UtcNow && ActiveCourseUser.AbstractRoleID == (int)OSBLE.Models.Courses.CourseRole.CourseRoles.Student)
            {
                model.TestValue = "Assignment has not yet been released";
            }
            */

            return View(model);
        }

        [HttpPost]
        public ActionResult Index()
        {
            // Pull the course and assignment ID from the form values
            string strCourseID = Request.Form["courseid"];
            int courseID;
            if (!int.TryParse(strCourseID, out courseID))
            {
                throw new Exception(
                    "Could not parse course ID from string: " +
                    strCourseID);
            }
            string strAssignmentID = Request.Form["assignmentid"];
            int aID;
            if (!int.TryParse(strAssignmentID, out aID))
            {
                throw new Exception(
                    "Could not parse assignment ID from string: " +
                    strAssignmentID);
            }

            // Maps a team ID to the attributable submission file. We want to 
            // keep these in memory as we alter their properties and then 
            // commit everything to disk at the end.
            Dictionary<int, AttributableFile> teamFiles = 
                new Dictionary<int, AttributableFile>();
            
            // Go through all the form keys
            foreach (string key in Request.Form.Keys)
            {
                string val = Request.Form[key];

                // First parse out the team ID
                int teamID = -1;
                if (key.StartsWith("radios"))
                {
                    if (!int.TryParse(key.Substring(6), out teamID))
                    {
                        continue;
                    }
                }
                else if (key.StartsWith("outcome"))
                {
                    int uIndex = key.IndexOf('_');
                    if (-1 == uIndex) { continue; }
                    // The stuff after the underscore isn't used
                    if (!int.TryParse(key.Substring(7, uIndex - 7), out teamID))
                    {
                        continue;
                    }
                }
  
                // If we failed to parse out a teamID then we continue
                if (-1 == teamID) { continue; }

                // Get the file
                AttributableFile file = null;
                if (teamFiles.ContainsKey(teamID))
                {
                    // It's already cached
                    file = teamFiles[teamID];
                }
                else // We have to retrieve it
                {
                    OSBLE.Models.FileSystem.FileSystem fs =
                        new OSBLE.Models.FileSystem.FileSystem();
                    IFileSystem ifs = fs.
                        Course(courseID).
                        Assignment(aID).
                        Submission(teamID);
                    file = (ifs as AttributableFilesFilePath).FirstFile;
                    if (null == file)
                    {
                        throw new Exception(
                            "Could not find assignment submission for team with ID=" +
                            teamID.ToString());
                    }

                    // Delete all "ABETOutcome" attributes. This is because the form 
                    // data for the post only contains information about checked options 
                    // for these and not unchecked. So we clear all then re-add the 
                    // checked
                    file.DeleteSysAttrs("ABETOutcome");

                    teamFiles.Add(teamID, file);
                }

                // If the key starts with "radios" then it's a proficiency 
                // level for a team.
                if (key.StartsWith("radios"))
                {
                    // Set the "ABETProficiencyLevel" system attribute
                    file.SetSysAttr("ABETProficiencyLevel", val);
                }

                // If the key is of the form "outcomeX_teamY" then it's an 
                // ABET outcome that is checked. All outcomes NOT in the 
                // form data are implicitly unchecked. Upon retrieving the 
                // file we already deleted the outcomes, so we just need to 
                // add the ones from the form data.
                if (key.StartsWith("outcome"))
                {
                    // Make sure we add, not set, because there can be multiple 
                    // attributes with this same name.
                    file.AddSysAttr("ABETOutcome", val);
                }
            }

            // Now go through and save attributes for any files that have 
            // been modified
            foreach (KeyValuePair<int, AttributableFile> kvp in teamFiles)
            {
                AttributableFile file = kvp.Value;
                if (file.Modified)
                {
                    file.SaveAttrs();
                }
            }

            return RedirectToAction("Index", "../Assignment");
        }
    }
}
