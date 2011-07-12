using System;
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
            //This will simulate long load times
            /*Int64 i = 0;
            while (i < 1000000000)
            {
                i++;
            }*/

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

                //purposefully not using the is statement as we also want to make sure it is not a null SubmissionActivity
                if (activity as SubmissionActivity != null)
                {
                    AbstractAssignment assignment = db.AbstractAssignments.Find(activity.AbstractAssignmentID);

                    List<Deliverable> deliverables = new List<Deliverable>((assignment as StudioAssignment).Deliverables);

                    if (assignment != null && assignment.Category.CourseID == activeCourse.AbstractCourseID && activeCourse.AbstractRole.CanSubmit == true && assignment is StudioAssignment)
                    {
                        TeamUserMember teamUser = GetTeamUser(activity as SubmissionActivity, currentUser);

                        int i = 0;
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
                                    FileSystem.RemoveZipFile(activeCourse.AbstractCourse as Course, activity, teamUser);
                                    var path = Path.Combine(FileSystem.GetTeamUserSubmissionFolder(true, activeCourse.AbstractCourse as Course, (int)id, teamUser), deliverables[i].Name + extension);
                                    file.SaveAs(path);
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