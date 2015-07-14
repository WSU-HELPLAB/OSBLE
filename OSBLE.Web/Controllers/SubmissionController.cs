using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

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
                    if (assignment.CourseID == ActiveCourseUser.AbstractCourseID && (ActiveCourseUser.AbstractRole.CanSubmit == true || ActiveCourseUser.AbstractRole.CanUploadFiles))
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

                var sub = new SubmitEvent
                {
                    AssignmentId = id.Value,
                    SenderId = CurrentUser.ID
                };
                Posts.SubmitAssignment(sub);


                Assignment assignment = db.Assignments.Find(id);

                if (assignment != null && (assignment.HasDeliverables == true ||
                                            assignment.Type == AssignmentTypes.CriticalReview ||
                                            assignment.Type == AssignmentTypes.AnchoredDiscussion))
                {
                    var deliverables = AssignmentSubmissionHelper.UpdateDeliverables(files, assignment);

                    if (assignment.CourseID == ActiveCourseUser.AbstractCourseID && (ActiveCourseUser.AbstractRole.CanSubmit == true || ActiveCourseUser.AbstractRole.CanUploadFiles == true))
                    {
                        AssignmentTeam assignmentTeam;
                        List<string> deliverableNames = new List<string>();
                        if (AssignmentSubmissionHelper.SubmitAssignments(Request.Params, ActiveCourseUser, CurrentUser.FirstName, CurrentUser.LastName, id, files, authorTeamID, assignment, deliverables, out assignmentTeam, ref deliverableNames))
                        {
                            //The submission view handles incorrect extension types, so this area of code is unlikely to be reached. In the case that it does a occur, 
                            //we will ineloquently redirect them to assignment index without feedback.
                            Cache["SubmissionReceived"] = false;
                            return RedirectToAction("Index", "Assignment");
                        }

                        foreach (var name in deliverableNames)
                        {
                            (new NotificationController()).SendFilesSubmittedNotification(assignment, assignmentTeam, name);
                        }

                        Cache["SubmissionReceived"] = true;
                        Cache["SubmissionReceivedAssignmentID"] = assignment.ID;
                        if (authorTeamID != null)
                        {
                            Cache["SubmissionForAuthorTeamID"] = authorTeamID;
                        }
                        if (assignment.Type == AssignmentTypes.AnchoredDiscussion)
                            return RedirectToAction("Index", "AnchoredDiscussionController");
                        else
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
