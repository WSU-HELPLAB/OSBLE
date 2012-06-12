using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;

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
                    if (assignment.Category.CourseID == ActiveCourseUser.AbstractCourseID && ActiveCourseUser.AbstractRole.CanSubmit == true)
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
                TeamMember teamMember = GetTeamUser(assignment, CurrentUser);

                var score = (from c in assignment.Scores where c.CourseUserID == teamMember.CourseUserID select c).FirstOrDefault();

                if (score != null)
                {
                    if (score.HasGrade())
                    {
                        //MG: Users will not be able to open the submission window if they already have a grade, but in the rare case that a user gets here and has a grade
                        //we will ineloquent redirect them to to the index without any feedback. (Chances of this occuring are slim)
                        Cache["SubmissionReceived"] = false;
                        return RedirectToAction("Index", "Assignment");
                    }
                }

                if (assignment != null && (assignment.HasDeliverables == true || assignment.Type == AssignmentTypes.CriticalReview))
                {
                    List<dynamic> deliverables;
                    if(assignment.Type == AssignmentTypes.CriticalReview)
                    {
                        deliverables = new List<dynamic>((assignment.PreceedingAssignment).Deliverables);
                    }
                    else
                    {
                        deliverables = new List<dynamic>((assignment).Deliverables);
                    }


                    if (assignment.Category.CourseID == ActiveCourseUser.AbstractCourseID && ActiveCourseUser.AbstractRole.CanSubmit == true)
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
                                    string extension = Path.GetExtension(file.FileName);

                                    string[] allowFileExtensions = GetFileExtensions(type);

                                    if (allowFileExtensions.Contains(extension))
                                    {
                                        if (assignment.Type == AssignmentTypes.CriticalReview)
                                        {
                                            AssignmentTeam authorTeam = (from at in db.AssignmentTeams
                                                                         where at.TeamID == authorTeamID &&
                                                                         at.AssignmentID == assignment.PrecededingAssignmentID
                                                                             select at).FirstOrDefault();
                                            //MG&MK: file system for critical review assignments is laid out a bit differently, so 
                                            //critical review assignments must use different file system functions

                                            //If a submission of any extension exists delete it.  This is needed because they could submit a .c file and then a .cs file and the teacher would not know which one is the real one.
                                            string submission = FileSystem.GetCriticalReviewDeliverable(ActiveCourseUser.AbstractCourse as Course, assignment.ID, assignmentTeam, deliverables[i].Name, allowFileExtensions, authorTeam);
                                            if (submission != null)
                                            {
                                                FileInfo oldSubmission = new FileInfo(submission);

                                                if (oldSubmission.Exists)
                                                {
                                                    oldSubmission.Delete();
                                                }
                                            }

                                            //We need to remove the zipfile corrisponding to the authorTeamId being sent in as well as the regularly cached zip. 
                                            AssignmentTeam precedingAuthorAssignmentTeam = (from at in assignment.PreceedingAssignment.AssignmentTeams
                                                                                            where at.TeamID == authorTeamID
                                                                                            select at).FirstOrDefault();
                                            FileSystem.RemoveZipFile(ActiveCourseUser.AbstractCourse as Course, assignment, precedingAuthorAssignmentTeam );
                                            FileSystem.RemoveZipFile(ActiveCourseUser.AbstractCourse as Course, assignment, assignmentTeam);
                                            string path = Path.Combine(FileSystem.GetTeamUserSubmissionFolderForAuthorID(true, ActiveCourseUser.AbstractCourse as Course, (int)id, assignmentTeam, authorTeam.Team), deliverables[i].Name + extension);
                                            file.SaveAs(path);

                                            //unzip and rezip xps files because some XPS generators don't do it right
                                            if (extension.ToLower().CompareTo(".xps") == 0)
                                            {
                                                string extractPath = Path.Combine(FileSystem.GetTeamUserSubmissionFolderForAuthorID(true, ActiveCourseUser.AbstractCourse as Course, (int)id, assignmentTeam, authorTeam.Team), "extract");
                                                using (ZipFile oldZip = ZipFile.Read(path))
                                                {
                                                    oldZip.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
                                                }
                                                using (ZipFile newZip = new ZipFile())
                                                {
                                                    newZip.AddDirectory(extractPath);
                                                    newZip.Save(path);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            //If a submission of any extension exists delete it.  This is needed because they could submit a .c file and then a .cs file and the teacher would not know which one is the real one.
                                            string submission = FileSystem.GetDeliverable(ActiveCourseUser.AbstractCourse as Course, assignment.ID, assignmentTeam, deliverables[i].Name, allowFileExtensions);
                                            if (submission != null)
                                            {
                                                FileInfo oldSubmission = new FileInfo(submission);

                                                if (oldSubmission.Exists)
                                                {
                                                    oldSubmission.Delete();
                                                }
                                            }
                                            FileSystem.RemoveZipFile(ActiveCourseUser.AbstractCourse as Course, assignment, assignmentTeam);
                                            string path = Path.Combine(FileSystem.GetTeamUserSubmissionFolder(true, ActiveCourse.AbstractCourse as Course, (int)id, assignmentTeam), deliverables[i].Name + extension);
                                            file.SaveAs(path);

                                            //unzip and rezip xps files because some XPS generators don't do it right
                                            if (extension.ToLower().CompareTo(".xps") == 0)
                                            {
                                                string extractPath = Path.Combine(FileSystem.GetTeamUserSubmissionFolder(true, ActiveCourse.AbstractCourse as Course, (int)id, assignmentTeam), "extract");
                                                using (ZipFile oldZip = ZipFile.Read(path))
                                                {
                                                    oldZip.ExtractAll(extractPath, ExtractExistingFileAction.OverwriteSilently);
                                                }
                                                using (ZipFile newZip = new ZipFile())
                                                {
                                                    newZip.AddDirectory(extractPath);
                                                    newZip.Save(path);
                                                }
                                            }
                                        }
                                        
                                        DateTime? dueDate = assignment.DueDate;
                                        if (dueDate != null)
                                        {
                                            (new NotificationController()).SendFilesSubmittedNotification(assignment, assignmentTeam, deliverables[i].Name);
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
                                    var path = Path.Combine(FileSystem.GetTeamUserSubmissionFolder(true, ActiveCourseUser.AbstractCourse as Course, (int)id, assignmentTeam), CurrentUser.LastName + "_" + CurrentUser.FirstName + "_" + delName + ".txt");
                                    System.IO.File.WriteAllText(path, inbrowser);
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
