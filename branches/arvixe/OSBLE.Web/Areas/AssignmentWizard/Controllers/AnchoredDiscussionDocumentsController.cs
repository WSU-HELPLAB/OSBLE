using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using System.IO;

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
        public ActionResult Index(Assignment model, HttpPostedFileBase file)
        {            
            //TODO: handle file upload with annotate
            if (file.ContentLength > 0)
            {
                var fileName = Path.GetFileName(file.FileName);
                var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName);
                //file.SaveAs(path);
            }
            //TODO: handle multiple files uploaded
            //change file >> files
            //foreach (var file in files)
            //{
            //    if (file.ContentLength > 0)
            //    {
            //        var fileName = Path.GetFileName(file.FileName);
            //        var path = Path.Combine(Server.MapPath("~/App_Data/uploads"), fileName);
            //        file.SaveAs(path);
            //    }
            //}


            
            Assignment = db.Assignments.Find(model.ID);
            WasUpdateSuccessful = true;
            return base.PostBack(model);
        }
    }
}
