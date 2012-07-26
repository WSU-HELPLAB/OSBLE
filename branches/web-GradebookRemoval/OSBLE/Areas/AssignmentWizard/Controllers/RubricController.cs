using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Assignments;
using System.Text.RegularExpressions;
using OSBLE.Models.Courses;
using OSBLE.Areas.AssignmentWizard.ViewModels;
using System.IO;
using OSBLE.Resources;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class RubricController : RubricBaseController
    {
        public override string ControllerName
        {
            get { return "Rubric";  }
        }

        public override string ControllerDescription
        {
            get { return "The instructor will use a grading rubric"; }
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
                //return new List<AssignmentTypes>();
                List<AssignmentTypes> types = base.AllAssignmentTypes.ToList();
                types.Remove(AssignmentTypes.TeamEvaluation);
                return types;
            }
        }

        public override ActionResult Index()
        {
            base.Index();
            return LoadRubric(Assignment.RubricID);
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
                if (Assignment.HasRubric)
                {
                    //delete the old rubric (to be replaced with the one in the HTML)
                    db.Rubrics.Remove(Assignment.Rubric);
                    db.SaveChanges();
                }

                //Load the rubric from the view
                int rubricID = LoadRubricFromHTML();

                Assignment.RubricID = rubricID;
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
