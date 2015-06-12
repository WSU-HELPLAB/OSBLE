using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Collections;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class StudentRubricController : RubricBaseController
    {
        public override string ControllerDescription
        {
            get
            {
                return "The student will use a grading rubric";
            }
        }

        public override string ControllerName
        {
            get
            {
                return "StudentRubric";
            }
        }

        public override string PrettyName
        {
            get
            {
                return "Student Rubric";
            }
        }

        public override WizardBaseController Prerequisite
        {
            get
            {
                return new TeamController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                List<AssignmentTypes> types = new List<AssignmentTypes>();
                types.Add(AssignmentTypes.CriticalReview);
                return types;
            }
        }


        public override ActionResult Index()
        {
            base.Index();
            return LoadRubric(Assignment.StudentRubricID);
        }


        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            int rID;

            if (Int32.TryParse(Request.Params["AssignmentOption"], out rID))
            {
                return LoadExistingRubric(rID);
            }

            //reset our assignment
            Assignment = db.Assignments.Find(model.ID);

            if (Assignment != null)
            {
                if (Assignment.HasStudentRubric)
                {
                    //delete the old rubric (to be replaced with the one in the HTML)
                    db.Rubrics.Remove(Assignment.StudentRubric);
                    db.SaveChanges();
                }

                //Load the rubric from the view
                int rubricID = LoadRubricFromHTML();

                Assignment.StudentRubricID = rubricID;
                db.Entry(Assignment).State = System.Data.EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                //fail gracefully
            }
            return base.PostBack(Assignment);
        }
    }
}
