using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using System.IO;
using OSBLE.Controllers;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class AnchoredDiscussionDocumentsController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "AnchoredDiscussionDocuments"; }
        }

        public override string PrettyName
        {
            get
            {
                return "Select Review Documents";
            }
        }

        public override string ControllerDescription
        {
            get { return "This assignment requires that instructors submit one or more files for an Anchored Discussion."; }
        }

        public override IWizardBaseController Prerequisite
        {
            get
            {
                //
                return new BasicsController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                return (new AssignmentTypes[] { AssignmentTypes.AnchoredDiscussion }).ToList();
            }
        }

        /// <summary>
        /// The previous assignment component is required for all assignments listed in ValidAssignmentTypes
        /// </summary>
        public override bool IsRequired
        {
            get
            {
                return true;
            }
        }

        public override ActionResult Index()
        {
            base.Index();
            ModelState.Clear();
            ActiveCourseUser.AbstractRole.CanSubmit = true;
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model, IEnumerable<HttpPostedFileBase> files)
        {
            Assignment = db.Assignments.Find(model.ID);
            
            //remove old teams
            //Assignment.AssignmentTeams.Clear();
            //Assignment.ReviewTeams.Clear();    
            //db.SaveChanges();

            //TODO: handle file upload with annotate   
            //foreach file create an a team!
            //foreach (var file in files)
            //{
            //    if (file.ContentLength > 0)
            //    {
            //        var fileName = Path.GetFileName(file.FileName);
            //        ViewBag.UploadPath = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName);

            //        Assignment.AssignmentTeams.Add(new AssignmentTeam
            //        {
            //            Assignment = Assignment,
            //            AssignmentID = Assignment.ID,
            //            //Team = ,
            //            TeamID = ActiveCourseUser.ID,
            //        });

            //        Assignment.ReviewTeams.Add(new ReviewTeam
            //        {
            //            Assignment = Assignment,
            //            AssignmentID = Assignment.ID,
            //            //AuthorTeam = ,
            //            AuthorTeamID = ActiveCourseUser.ID,
            //            //ReviewingTeam = ,
            //            ReviewTeamID = ActiveCourseUser.ID,
                        
            //        });
            //        db.SaveChanges();
            //    }
            //}

            //submit file to the system
            SubmissionController submission = new SubmissionController();            
            submission.Create(model.ID, files, ActiveCourseUser.AbstractCourse.ID);

            

            
            WasUpdateSuccessful = true;
            return base.PostBack(model);

        }
    }
}
