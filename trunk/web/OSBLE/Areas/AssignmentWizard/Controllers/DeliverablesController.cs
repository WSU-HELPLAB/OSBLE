using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using System.Data.Entity.Validation;
using System.Diagnostics;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class DeliverablesController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "Deliverables"; }
        }

        public override string ControllerDescription
        {
            get { return "This assignment requires that students submit one or more files"; }
        }

        public override WizardBaseController Prerequisite
        {
            get
            {
                return new CommentCategoryController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                List<AssignmentTypes> types = base.AllAssignmentTypes.ToList();
                types.Remove(AssignmentTypes.DiscussionAssignment);
                types.Remove(AssignmentTypes.TeamEvaluation);
                types.Remove(AssignmentTypes.CriticalReview);
                types.Remove(AssignmentTypes.CriticalReviewDiscussion);
                return types;
            }
        }

        private new void SetUpViewBag()
        {
            List<SelectListItem> allItems = GetListOfDeliverableTypes();

            //AC: This borks code.  Not sure why.
            //if we're in an inline review assignment type, then use only the deliverables that work for us
            /*
            if (Assignment.HasCommentCategories)
            {
                List<SelectListItem> restrictedItems = new List<SelectListItem>();
                string[] validTypes = {"xps", "code", "video", "text"};
                foreach(string type in validTypes)
                {
                    SelectListItem item = allItems.Where(sli => sli.Text.ToLower().Contains(type)).FirstOrDefault();
                    if (item != null)
                    {
                        restrictedItems.Add(item);
                    }
                }
                allItems = restrictedItems;
            }
             * */

            ViewBag.DeliverableTypes = new SelectList(allItems, "Value", "Text");
            ViewBag.AllowedFileNames = from c in FileSystem.GetCourseDocumentsFileList(ActiveCourse.AbstractCourse, includeParentLink: false).Files select c.Name;
        }

        public override ActionResult Index()
        {
            base.Index();
            ModelState.Clear();
            SetUpViewBag();
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = db.Assignments.Find(model.ID);
            ParseFormValues();
            WasUpdateSuccessful = true;

            //update our DB.  Note that we don't have logic for inserting new assignments
            //as we require that the basics controller come first and that handles new
            //assignment creation.
            db.Entry(Assignment).State = System.Data.EntityState.Modified;
            db.SaveChanges();
            return base.PostBack(model);
        }

        private void ParseFormValues()
        {
            int i = 0;
            bool hasMoreElements = true;
            string prefix = "Assignment.Deliverables";
            string deliverableNameKey = "";
            string deliverableTypeKey = "";
            string databaseIdKey = "";

            //the assignment might possibly already have a list of deliverables.
            //As such, we need to create a new list that we will eventually assign
            //back to the assignment.
            List<Deliverable> deliverables = new List<Deliverable>();

            while (hasMoreElements)
            {
                deliverableNameKey = string.Format("{0}[{1}].Name", prefix, i);
                deliverableTypeKey = string.Format("{0}[{1}].Type", prefix, i);
                databaseIdKey = string.Format("{0}[{1}].DatabaseId", prefix, i);
                
                if (Request.Form.AllKeys.Contains(deliverableNameKey))
                {
                    //build the new deliverable
                    Deliverable deliverable = new Deliverable();
                    deliverable.AssignmentID = Assignment.ID;
                    deliverable.Name = Request.Form[deliverableNameKey];
                    deliverable.Type = Convert.ToInt32(Request.Form[deliverableTypeKey]);
                    deliverables.Add(deliverable);
                }
                else
                {
                    hasMoreElements = false;
                }

                i++;
            }

            //clear out old deliverables and reassign
            Assignment.Deliverables.Clear();
            Assignment.Deliverables = deliverables;
        }
    }
}
