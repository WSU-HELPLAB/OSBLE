using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.FileSystem;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using Ionic.Zip;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
    //[CanSubmitAssignments]
    public class SubmissionController : OSBLEController
    {
        //
        // GET: /Submission/Create

        public ActionResult Create(int? id, int? authorTeamID = null)
        {
            if (id != null)
            {
                Assignment assignment = db.Assignments.Find(id);

                if (assignment != null)
                {
                    if (assignment.CourseID == ActiveCourseUser.AbstractCourseID && ActiveCourseUser.AbstractRole.CanSubmit == true)
                    {

                        if (assignment.Type == AssignmentTypes.CriticalReview)
                        {
                            ViewBag.authorTeamID = authorTeamID;
                            setViewBagDeliverables((assignment.PreceedingAssignment).Deliverables);
                        }
                        else
                        {
                            setViewBagDeliverables((assignment).Deliverables);
                        }
                        return View();
                    }
                }
            }

            throw new Exception();
        }

        private void setViewBagDeliverables(IList<Deliverable> deliverables)
        {
            Dictionary<Deliverable, string[]> allowedFileExtensions = new Dictionary<Deliverable, string[]>();

            foreach (Deliverable deliverable in deliverables)
            {
                allowedFileExtensions.Add(deliverable, GetFileExtensions((DeliverableType)deliverable.Type));
            }

            ViewBag.Deliverables = allowedFileExtensions;
        }

        public ActionResult SubmittedSuccessfully()
        {
            return View();
        }

        //
        // POST: /Submission/Create

        [HttpPost]
        [CanSubmitAssignments]
        public ActionResult Create(int? id, IEnumerable<HttpPostedFileBase> files, int? authorTeamID = null)
        {
            if (id != null)
            {
                Assignment assignment = db.Assignments.Find(id);

                if (assignment != null && (assignment.HasDeliverables == true || assignment.Type == AssignmentTypes.CriticalReview))
                {
                    List<Deliverable> deliverables;
                    if(assignment.Type == AssignmentTypes.CriticalReview)
                    {
                        deliverables = new List<Deliverable>((assignment.PreceedingAssignment).Deliverables);
                    }
                    else
                    {
                        deliverables = new List<Deliverable>((assignment).Deliverables);
                    }


                    if (assignment.CourseID == ActiveCourseUser.AbstractCourseID && ActiveCourseUser.AbstractRole.CanSubmit == true)
                    {
                        AssignmentTeam assignmentTeam = GetAssignmentTeam(assignment, ActiveCourseUser);
                        
                        int i = 0;

                        //the files variable is null when submitting an in-browser text submission
                        if (files != null)
                        {  
                            foreach (var file in files)
                            {
                                if (file != null && file.ContentLength > 0)
                                {
                                    DeliverableType type = (DeliverableType)deliverables[i].Type;

                                    //jump over all DeliverableType.InBrowserText as they will be handled separately
                                    while (type == DeliverableType.InBrowserText)
                                    {
                                        i++;
                                        type = (DeliverableType)deliverables[i].Type;
                                    }
                                    string fileName = Path.GetFileName(file.FileName);
                                    string extension = Path.GetExtension(file.FileName).ToLower();
                                    string deliverableName = string.Format("{0}{1}", deliverables[i].Name, extension);

                                    string[] allowFileExtensions = GetFileExtensions(type);

                                    if (allowFileExtensions.Contains(extension))
                                    {
                                        if (assignment.Type == AssignmentTypes.CriticalReview)
                                        {
                                            AssignmentTeam authorTeam = (from at in db.AssignmentTeams
                                                                         where at.TeamID == authorTeamID &&
                                                                         at.AssignmentID == assignment.PrecededingAssignmentID
                                                                             select at).FirstOrDefault();

                                            ReviewTeam reviewTeam = (from tm in db.TeamMembers
                                                                     join t in db.Teams on tm.TeamID equals t.ID
                                                                     join rt in db.ReviewTeams on t.ID equals rt.ReviewTeamID
                                                                     where tm.CourseUserID == ActiveCourseUser.ID
                                                                     && rt.AssignmentID == assignment.ID
                                                                     select rt).FirstOrDefault();

                                            //MG&MK: file system for critical review assignments is laid out a bit differently, so 
                                            //critical review assignments must use different file system functions

                                            string trueFileName = reviewTeam.ReviewingTeam.Name + " review of " + authorTeam.Team.Name + " - " + deliverableName; 
                                            OSBLE.Models.FileSystem.AssignmentFilePath fs = 
                                                Models.FileSystem.Directories.GetAssignment(
                                                    ActiveCourseUser.AbstractCourseID, assignment.ID);
                                            fs.Review(authorTeam.TeamID, reviewTeam.ReviewTeamID).DeleteFile(trueFileName);                        

                                            //We need to remove the zipfile corresponding to the authorTeamId being sent in as well as the regularly cached zip. 
                                            //AssignmentTeam precedingAuthorAssignmentTeam = (from at in assignment.PreceedingAssignment.AssignmentTeams
                                            //                                                where at.TeamID == authorTeamID
                                            //                                                select at).FirstOrDefault();
                                            //FileSystem.RemoveZipFile(ActiveCourseUser.AbstractCourse as Course, assignment, precedingAuthorAssignmentTeam );
                                            //FileSystem.RemoveZipFile(ActiveCourseUser.AbstractCourse as Course, assignment, assignmentTeam);

                                            //add in the new file
                                            fs.Review(authorTeam.TeamID, reviewTeam.ReviewTeamID)
                                                .AddFile(trueFileName, file.InputStream);

                                            //unzip and rezip xps files because some XPS generators don't do it right
                                            //if (extension.ToLower().CompareTo(".xps") == 0)
                                            //{
                                            //    //XPS documents require the actual file path, so get that.
                                            //    OSBLE.Models.FileSystem.FileCollection fileCollection =
                                            //        OSBLE.Models.FileSystem.Directories.GetAssignment(
                                            //            ActiveCourseUser.AbstractCourseID, assignment.ID)
                                            //        .Review(authorTeam.TeamID, reviewTeam.ReviewTeamID)
                                            //        .File(deliverables[i].Name);
                                            //    string path = fileCollection.FirstOrDefault();

                                            //    string extractPath = Path.Combine(FileSystem.GetTeamUserSubmissionFolderForAuthorID(true, ActiveCourseUser.AbstractCourse as Course, (int)id, assignmentTeam, authorTeam.Team), "extract");
                                            //    using (ZipFile oldZip = ZipFile.Read(path))
                                            //    {
                                            //        oldZip.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
                                            //    }
                                            //    using (ZipFile newZip = new ZipFile())
                                            //    {
                                            //        newZip.AddDirectory(extractPath);
                                            //        newZip.Save(path);
                                            //    }
                                            //}
                                        }
                                        else
                                        {
                                            //If a submission of any extension exists delete it.  This is needed because they could submit a .c file and then a .cs file and the teacher would not know which one is the real one.
                                            //string submission = FileSystem.GetDeliverable(ActiveCourseUser.AbstractCourse as Course, assignment.ID, assignmentTeam, deliverables[i].Name, allowFileExtensions);
                                            //if (submission != null)
                                            //{
                                            //    FileInfo oldSubmission = new FileInfo(submission);

                                            //    if (oldSubmission.Exists)
                                            //    {
                                            //        oldSubmission.Delete();
                                            //    }
                                            //}
                                            //FileSystem.RemoveZipFile(ActiveCourseUser.AbstractCourse as Course, assignment, assignmentTeam);
                                            //string path = Path.Combine(FileSystem.GetTeamUserSubmissionFolder(true, ActiveCourseUser.AbstractCourse as Course, (int)id, assignmentTeam), deliverables[i].Name + extension);
                                            //file.SaveAs(path);

                                            OSBLEDirectory submissions = Directories.GetAssignmentSubmission(ActiveCourseUser.AbstractCourseID, assignment.ID, assignmentTeam.TeamID);
                                            submissions.DeleteFile(file.FileName);

                                            DateTime? dueDate = assignment.DueDate;
                                            if (dueDate != null)
                                            {
                                                (new NotificationController()).SendFilesSubmittedNotification(assignment, assignmentTeam, deliverables[i].Name);
                                            }

                                            submissions.AddFile(assignmentTeam.Team.Name+" "+deliverableName, file.InputStream);

                                            ////unzip and rezip xps files because some XPS generators don't do it right
                                            //if (extension.ToLower().CompareTo(".xps") == 0)
                                            //{
                                            //    string extractPath = Path.Combine(FileSystem.GetTeamUserSubmissionFolder(true, ActiveCourseUser.AbstractCourse as Course, (int)id, assignmentTeam), "extract");
                                            //    using (ZipFile oldZip = ZipFile.Read(path))
                                            //    {
                                            //        oldZip.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
                                            //    }
                                            //    using (ZipFile newZip = new ZipFile())
                                            //    {
                                            //        newZip.AddDirectory(extractPath);
                                            //        newZip.Save(path);
                                            //    }
                                            //}
                                        }                                     
                                    }
                                    else
                                    {
                                        //The submission view handles incorrect extension types, so this area of code is unlikely to be reached. In the case that it does a occur, 
                                        //we will ineloquently redirect them to assignment index without feedback.
                                        Cache["SubmissionReceived"] = false;
                                        return RedirectToAction("Index", "Assignment");
                                    }
                                }
                                i++;
                            }
                        }

                        // Creates the text files from text boxes
                        int j = 0;
                        string delName;
                        do
                        {
                            delName = Request.Params["desiredName[" + j + "]"];
                            if (delName != null)
                            {
                                string inbrowser = Request.Params["inBrowserText[" + j + "]"];
                                if (inbrowser.Length > 0)
                                {
                                    if (assignment.Type == AssignmentTypes.CriticalReview)
                                    {
                                        AssignmentTeam authorTeam = (from at in db.AssignmentTeams
                                                                     where at.TeamID == authorTeamID &&
                                                                     at.AssignmentID == assignment.PrecededingAssignmentID
                                                                     select at).FirstOrDefault();

                                        ReviewTeam reviewTeam = (from tm in db.TeamMembers
                                                                 join t in db.Teams on tm.TeamID equals t.ID
                                                                 join rt in db.ReviewTeams on t.ID equals rt.ReviewTeamID
                                                                 where tm.CourseUserID == ActiveCourseUser.ID
                                                                 && rt.AssignmentID == assignment.ID
                                                                 select rt).FirstOrDefault();

                                        //MG&MK: file system for critical review assignments is laid out a bit differently, so 
                                        //critical review assignments must use different file system functions
                                        OSBLE.Models.FileSystem.AssignmentFilePath fs =
                                            Models.FileSystem.Directories.GetAssignment(
                                                ActiveCourseUser.AbstractCourseID, assignment.ID);
                                        fs.Review(authorTeam.TeamID, reviewTeam.ReviewTeamID).DeleteDir(null);

                                        MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(inbrowser));
                                        //add in the new file
                                        fs.Review(authorTeam.TeamID, reviewTeam.ReviewTeamID)
                                            .AddFile((CurrentUser.LastName + "_" + CurrentUser.FirstName + "_" + delName + ".txt"), ms);
                                    }
                                    else
                                    {
                                        OSBLEDirectory submission = Directories.GetAssignmentSubmission(ActiveCourseUser.AbstractCourse.ID, assignment.ID, assignmentTeam.TeamID);
                                        //var path = Path.Combine(FileSystem.GetTeamUserSubmissionFolder(true, ActiveCourseUser.AbstractCourse as Course, (int)id, assignmentTeam), CurrentUser.LastName + "_" + CurrentUser.FirstName + "_" + delName + ".txt");
                                        MemoryStream ms = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(inbrowser));
                                        submission.AddFile((CurrentUser.LastName + "_" + CurrentUser.FirstName + "_" + delName + ".txt"), ms);
                                    }
                                }
                            }
                            j++;
                        } while (delName != null);
                        Cache["SubmissionReceived"] = true;
                        Cache["SubmissionReceivedAssignmentID"] = assignment.ID;
                        if (authorTeamID != null)
                        {
                            Cache["SubmissionForAuthorTeamID"] = authorTeamID;
                        }
                        return Redirect(Request.UrlReferrer.ToString());
                    }
                }
            }

            return Create(id);
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
