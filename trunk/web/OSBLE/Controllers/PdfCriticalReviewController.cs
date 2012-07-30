using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Annotate;
using System.Configuration;
using System.Net;

namespace OSBLE.Controllers
{
    public class PdfCriticalReviewController : OSBLEController
    {
        //
        // GET: /PdfCriticalReview/

        public ActionResult Index()
        {
            return View();
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
            ReviewTeam reviewTeam = (from rt in CRassignment.ReviewTeams
                                     where rt.ReviewTeamID == at.TeamID
                                     &&
                                     rt.AuthorTeamID == authorTeamID
                                     select rt).FirstOrDefault();
            if (reviewTeam != null && CRassignment.Type == AssignmentTypes.CriticalReview)
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
                            api.SetDocumentAnonymity(CurrentUser, uploadResult.DocumentCode, uploadResult.DocumentDate, CRassignment.CriticalReviewSettings);
                            api.GiveAccessToDocument(CurrentUser, uploadResult.DocumentCode, uploadResult.DocumentDate);

                            //log the user in to annotate
                            string loginString = api.GetAnnotateLoginUrl(CurrentUser, uploadResult.DocumentCode, uploadResult.DocumentDate);
                            string loginResult = client.DownloadString(loginString);

                            //load the annotate url for the view
                            ViewBag.AnnotateUrl = api.GetAnnotateDocumentUrl(uploadResult.DocumentCode, uploadResult.DocumentDate);
                        }
                    }
                }
                else
                {
                    return RedirectToRoute(new { controller = "Home", action = "Index" });
                }
            }
            return View();
        }
    }
}
