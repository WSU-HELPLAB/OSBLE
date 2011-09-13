using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Assignments;

namespace OSBLE.Controllers.Assignments.Wizard
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

        public override ICollection<WizardBaseController> Prerequisites
        {
            get
            {
                List<WizardBaseController> prereqs = new List<WizardBaseController>();
                prereqs.Add(new BasicsController());
                return prereqs;
            }
        }

        protected override object IndexAction()
        {
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");
            ViewBag.AllowedFileNames = from c in FileSystem.GetCourseDocumentsFileList(activeCourse.AbstractCourse, includeParentLink: false).Files select c.Name;
            return Assignment;
        }

        protected override object IndexActionPostback()
        {
            ParseFormValues();
            return new object();
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

                if (Requeset.Form.AllKeys.Contains(deliverableNameKey))
                {

                    //first, start with the ID
                    int deliverableId = 0;
                    if (!Int32.TryParse(Requeset.Form[databaseIdKey].ToString(), out deliverableId))
                    {
                        //something bad happened, get us out of here
                        continue;
                    }

                    if (deliverableId != 0)
                    {
                        Deliverable previousDeliverable = Assignment.Deliverables.Where(d => d.AssignmentID == deliverableId).FirstOrDefault();
                        if (previousDeliverable != null)
                        {
                            deliverables.Add(previousDeliverable);
                        }
                    }
                    else
                    {
                        //build the new deliverable
                        Deliverable deliverable = new Deliverable();
                        deliverable.AssignmentID = 0;
                        deliverable.Name = Requeset.Form[deliverableNameKey];
                        deliverable.Type = Convert.ToInt32(Requeset.Form[deliverableTypeKey]);
                        deliverables.Add(deliverable);
                    }
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
