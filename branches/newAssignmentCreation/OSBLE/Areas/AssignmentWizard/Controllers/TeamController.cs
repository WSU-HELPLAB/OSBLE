using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class TeamController : WizardBaseController
    {

        public override string ControllerName
        {
            get { return "Team"; }
        }

        public override string ControllerDescription
        {
            get { return "The assignment is team-based"; }
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

        private void SetUpViewBag()
        {
            //Guaranteed to pull all people enrolled in the course that can submit files
            //(probably students).
            List<CourseUsers> users = (from cu in db.CourseUsers
                                       where cu.ID == activeCourse.ID
                                       && cu.AbstractRole.CanSubmit
                                       select cu).ToList();

            //We'll need to cross the current teams with the list of course users
            List<AssignmentTeam> teams = (from t in db.AssignmentTeams
                                         where t.AssignmentID == Assignment.ID
                                         select t).ToList();

            //remove students currently on the team list from our complete user list
            foreach (AssignmentTeam team in teams)
            {
                foreach (TeamMember member in team.Team.TeamMembers)
                {
                    users.Remove(member.CourseUser);
                }
            }

            //place items into the viewbag
            ViewBag.UnassignedUsers = users;
            ViewBag.Teams = teams;
        }

        public override ActionResult Index() 
        {
            base.Index();
            SetUpViewBag();
            return View(Assignment);
        }

    }
}
