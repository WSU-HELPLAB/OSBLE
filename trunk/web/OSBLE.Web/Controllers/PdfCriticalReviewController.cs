using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Annotate;
using System.Configuration;
using System.Net;
using OSBLE.Models.Courses;
using OSBLE.Attributes;

namespace OSBLE.Controllers
{
    [ForceHttp]
    public class PdfCriticalReviewController : OSBLEController
    {
        //
        // GET: /PdfCriticalReview/

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// For instructors to grade PDF documents on basic assignments
        /// </summary>
        /// <param name="assignmentID"></param>
        /// <param name="authorTeamID"></param>
        /// <returns></returns>
        [CanGradeCourse]
        public ActionResult Grade(int assignmentID, int authorTeamID, bool forceUpdate = false)
        {
            WebClient client = new WebClient();
            Assignment assignment = db.Assignments.Find(assignmentID);
            AssignmentTeam at = GetAssignmentTeam(assignment, ActiveCourseUser);
            ReviewTeam reviewTeam = null;
            if (at != null)
            {
                reviewTeam = (from rt in assignment.ReviewTeams
                              where rt.ReviewTeamID == at.TeamID
                              &&
                              rt.AuthorTeamID == authorTeamID
                              select rt).FirstOrDefault();
            }

            //Send off to Annotate if we have exactly one deliverable and that deliverable is a PDF document
            if (assignment.Deliverables.Count == 1 && assignment.Deliverables[0].DeliverableType == DeliverableType.PDF)
            {
                AnnotateApi api = new AnnotateApi(ConfigurationManager.AppSettings["AnnotateUserName"], ConfigurationManager.AppSettings["AnnotateApiKey"]);

                AnnotateResult uploadResult = api.UploadDocument((int)assignment.ID, authorTeamID, forceUpdate);
                if (uploadResult.Result == ResultCode.OK)
                {
                    AnnotateResult createResult = api.CreateAccount(CurrentUser);
                    if (createResult.Result == ResultCode.OK)
                    {
                        //instructors get to see everyone, regardless of CR settings
                        CriticalReviewSettings settings = new CriticalReviewSettings();
                        settings.AnonymizeComments = false;
                        api.SetDocumentAnonymity(CurrentUser, uploadResult.DocumentCode, uploadResult.DocumentDate, settings);
                        api.GiveAccessToDocument(CurrentUser, uploadResult.DocumentCode, uploadResult.DocumentDate);

                        //log the user in to annotate
                        string loginString = api.GetAnnotateLoginUrl(CurrentUser, uploadResult.DocumentCode, uploadResult.DocumentDate);

                        //load the annotate url for the view
                        ViewBag.AnnotateUrl = loginString;
                    }
                }
            }
            else
            {
                return RedirectToRoute(new { controller = "Home", action = "Index", area = "" });
            }

            return View("Review");
        }

        /// <summary>
        /// Loads a PDF for critical review
        /// </summary>
        /// <param name="assignmentID"></param>
        /// <param name="authorTeamID"></param>
        /// <returns></returns>
        public ActionResult Review(int assignmentID, int authorTeamID)
        {
            WebClient client = new WebClient();
            Assignment CRassignment = db.Assignments.Find(assignmentID);
            AssignmentTeam at = GetAssignmentTeam(CRassignment, ActiveCourseUser);
            ReviewTeam reviewTeam = null;
            if (at != null)
            {
                reviewTeam = (from rt in CRassignment.ReviewTeams
                              where rt.ReviewTeamID == at.TeamID
                              &&
                              rt.AuthorTeamID == authorTeamID
                              select rt).FirstOrDefault();
            }
            bool canAccessReview = false;

            //Determine whether or not the current user can access the document
            if (CRassignment.Type == AssignmentTypes.CriticalReview)
            {
                //is the user a reviewer?
                if (reviewTeam != null)
                {
                    canAccessReview = true;
                }

                //or, is the user an instructor?
                else if (ActiveCourseUser.AbstractRole.CanGrade)
                {
                    canAccessReview = true;
                }

                //or, has the review been published and the current user is the author of the docment?
                else if (CRassignment.IsCriticalReviewPublished == true)
                {
                    reviewTeam = (from rt in CRassignment.ReviewTeams
                                  where rt.AuthorTeamID == authorTeamID
                                  select rt
                                ).FirstOrDefault();
                    if (reviewTeam != null)
                    {
                        TeamMember tm = reviewTeam.AuthorTeam.TeamMembers.Where(t => t.CourseUserID == ActiveCourseUser.ID).FirstOrDefault();
                        if (tm != null)
                        {
                            canAccessReview = true;
                        }
                    }
                }
            }

            if (canAccessReview)
            {
                //Send off to Annotate if we have exactly one deliverable and that deliverable is a PDF document
                if (CRassignment.PreceedingAssignment.Deliverables.Count == 1 && CRassignment.PreceedingAssignment.Deliverables[0].DeliverableType == DeliverableType.PDF)
                {
                    AnnotateApi api = new AnnotateApi(ConfigurationManager.AppSettings["AnnotateUserName"], ConfigurationManager.AppSettings["AnnotateApiKey"]);
                    AnnotateResult uploadResult = api.UploadDocument((int)CRassignment.PrecededingAssignmentID, authorTeamID);
                    if (uploadResult.Result == ResultCode.OK)
                    {
                        AnnotateResult createResult = api.CreateAccount(CurrentUser);
                        if (createResult.Result == ResultCode.OK)
                        {
                            //instructors get to see everyone, regardless of CR settings
                            CriticalReviewSettings settings = CRassignment.CriticalReviewSettings;
                            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                            {
                                settings.AnonymizeComments = false;
                            }
                            api.SetDocumentAnonymity(CurrentUser, uploadResult.DocumentCode, uploadResult.DocumentDate, settings);
                            api.GiveAccessToDocument(CurrentUser, uploadResult.DocumentCode, uploadResult.DocumentDate);

                            //log the user in to annotate
                            string loginString = api.GetAnnotateLoginUrl(CurrentUser, uploadResult.DocumentCode, uploadResult.DocumentDate);

                            //load the annotate url for the view
                            ViewBag.AnnotateUrl = loginString;
                        }
                    }
                }
                else
                {
                    return RedirectToRoute(new { controller = "Home", action = "Index", area = "" });
                }
            }
            return View();
        }
    }
}
