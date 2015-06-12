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
using OSBLE.Areas.AssignmentWizard.Models;

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

        public override IWizardBaseController Prerequisite
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
                bool CriticalEditOccured = bool.Parse(Request.Params["CriticalEditStatus"]); 
                if(Assignment.HasRubric && CriticalEditOccured) 
                {
                    //If there is an existing rubric, but the assignment creator agreed 
                    //to a critical edit (adding/removing colummns or rows while evaluations exist), 
                    //then delete the rubric & any associated evaluations and allow UpdateRubric() to recreate the rubirc.
                    //Note: This would be handled by UpdateRubric() but deleting a rubric from there causes a crash for some reason...
                    db.Rubrics.Remove(Assignment.Rubric);

                    //Finding and removing associated rubric evaluations
                    List<RubricEvaluation> associatedRubricEvals = (from re in db.RubricEvaluations
                                                       where re.AssignmentID == Assignment.ID
                                                       select re).ToList();

                    foreach(RubricEvaluation re in associatedRubricEvals)
                    {
                        db.RubricEvaluations.Remove(re);
                    }
                    db.SaveChanges();
                }
                UpdateRubric();


            }
            else
            {
                //fail gracefully
            }
            return base.PostBack(Assignment);
        }
    }
}
