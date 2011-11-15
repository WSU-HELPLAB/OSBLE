﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using Ionic.Zip;

namespace OSBLE.Controllers
{
    [Authorize]
    [CanSubmitAssignments]
    public class SubmissionController : OSBLEController
    {
        //
        // GET: /Submission/Create

        public ActionResult Create(int? id)
        {
            if (id != null)
            {
                AbstractAssignmentActivity activity = db.AbstractAssignmentActivities.Find(id);

                if (activity != null)
                {
                    AbstractAssignment assignment = db.AbstractAssignments.Find(activity.AbstractAssignmentID);

                    if (assignment != null && assignment.Category.CourseID == activeCourse.AbstractCourseID && activeCourse.AbstractRole.CanSubmit == true && assignment is StudioAssignment)
                    {
                        setViewBagDeliverables((assignment as StudioAssignment).Deliverables);

                        return View();
                    }
                }
            }

            throw new Exception();
        }

        private void setViewBagDeliverables(ICollection<Deliverable> deliverables)
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
        public ActionResult Create(int? id, IEnumerable<HttpPostedFileBase> files)
        {
            if (id != null)
            {
                AbstractAssignmentActivity activity = db.AbstractAssignmentActivities.Find(id);

                TeamUserMember teamUserMember = GetTeamUser(activity, currentUser);

                var score = (from c in activity.Scores where c.TeamUserMemberID == teamUserMember.ID select c).FirstOrDefault();

                if (score != null)
                {
                    if(score.Points != -1)
                        throw new Exception("Cannot submit to an assignmentActivity that already has a score");
                }

                //purposefully not using the is statement as we also want to make sure it is not a null SubmissionActivity
                if (activity as SubmissionActivity != null)
                {
                    AbstractAssignment assignment = db.AbstractAssignments.Find(activity.AbstractAssignmentID);

                    List<Deliverable> deliverables = new List<Deliverable>((assignment as StudioAssignment).Deliverables);

                    if (assignment != null && assignment.Category.CourseID == activeCourse.AbstractCourseID && activeCourse.AbstractRole.CanSubmit == true && assignment is StudioAssignment)
                    {
                        TeamUserMember teamUser = GetTeamUser(activity as SubmissionActivity, currentUser);

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
                                        //If a submission of any extension exists delete it.  This is needed because they could submit a .c file and then a .cs file and the teacher would not know which one is the real one.
                                        string submission = FileSystem.GetDeliverable(activeCourse.AbstractCourse as Course, activity.ID, teamUser, deliverables[i].Name, allowFileExtensions);
                                        if (submission != null)
                                        {
                                            FileInfo oldSubmission = new FileInfo(submission);

                                            if (oldSubmission.Exists)
                                            {
                                                oldSubmission.Delete();
                                            }
                                        }
                                        FileSystem.RemoveZipFile(activeCourse.AbstractCourse as Course, activity, teamUser);
                                        string path = Path.Combine(FileSystem.GetTeamUserSubmissionFolder(true, activeCourse.AbstractCourse as Course, (int)id, teamUser), deliverables[i].Name + extension);
                                        file.SaveAs(path);

                                        //unzip and rezip xps files because some XPS generators don't do it right
                                        if (extension.ToLower().CompareTo(".xps") == 0)
                                        {
                                            string extractPath = Path.Combine(FileSystem.GetTeamUserSubmissionFolder(true, activeCourse.AbstractCourse as Course, (int)id, teamUser), "extract");
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

                                        DateTime? dueDate = GetDueDate(activity);
                                        if (dueDate != null && dueDate < DateTime.Now)
                                        {
                                            (new NotificationController()).SendFilesSubmittedNotification(activity, teamUser, deliverables[i].Name);
                                        }
                                    }
                                    else
                                    {
                                        ModelState.AddModelError("FileExtensionMatch", "The file " + fileName + " does not have an allowed extension please convert the file to the correct type");
                                        setViewBagDeliverables((assignment as StudioAssignment).Deliverables);
                                        return View();
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
                                    var path = Path.Combine(FileSystem.GetTeamUserSubmissionFolder(true, activeCourse.AbstractCourse as Course, (int)id, teamUser), delName + ".txt");
                                    System.IO.File.WriteAllText(path, inbrowser);
                                }
                            }
                            j++;
                        } while (delName != null);

                        Session["SubmissionReceived"] = true;
                        return RedirectToAction("Index", "Assignment");
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
