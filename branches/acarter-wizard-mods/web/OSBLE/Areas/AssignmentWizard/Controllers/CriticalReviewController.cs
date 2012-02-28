using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class CriticalReviewController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "CriticalReview"; }
        }

        public override string ControllerDescription
        {
            get { return "Assign students or teams to review documents"; }
        }

        public override ICollection<WizardBaseController> Prerequisites
        {
            get
            {
                List<WizardBaseController> prereqs = new List<WizardBaseController>();
                prereqs.Add(new BasicsController());
                prereqs.Add(new TeamController());
                prereqs.Add(new PreviousAssignmentController());
                return prereqs;
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                return (new AssignmentTypes[] { AssignmentTypes.CriticalReview }).ToList();
            }
        }

        public override bool IsRequired
        {
            get
            {
                return true;
            }
        }

        private void SetUpViewBag()
        {
            //pull previous team configurations
            List<Assignment> previousTeamAssignments = (from assignment in db.Assignments
                                                        where assignment.Category.Course.ID == activeCourse.AbstractCourseID
                                                        where assignment.AssignmentTeams.Count > 0
                                                        select assignment).ToList();

            //place items into the viewbag
            ViewBag.Teams = Assignment.AssignmentTeams.Select(t => t.Team).OrderBy(t => t.Name).ToList();
            ViewBag.PreviousTeamAssignments = previousTeamAssignments;
        }

        public override ActionResult Index()
        {
            base.Index();
            SetUpViewBag();
            return View(Assignment);
        }
    }
}
