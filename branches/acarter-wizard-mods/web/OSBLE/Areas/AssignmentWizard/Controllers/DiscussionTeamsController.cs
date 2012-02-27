using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class DiscussionTeamsController : TeamController
    {
        public override string ControllerName
        {
            get { return "DiscussionTeams"; }
        }

        public override string ControllerDescription
        {
            get { return "Students will discuss a topic in teams"; }
        }

        public override ICollection<WizardBaseController> Prerequisites
        {
            get
            {
                List<WizardBaseController> prereqs = new List<WizardBaseController>();
                prereqs.Add(new BasicsController());
                prereqs.Add(new TeamController());
                prereqs.Add(new DiscussionController());
                return prereqs;
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                return (new AssignmentTypes[] { AssignmentTypes.DiscussionAssignment }).ToList();
            }
        }

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
            SetUpViewBag(Assignment.DiscussionTeams.Cast<IAssignmentTeam>().ToList());
            return View(Assignment);
        }

        [HttpPost]
        public override ActionResult Index(Assignment model)
        {
            //reset our assignment
            Assignment = db.Assignments.Find(model.ID);

            //two postback options: 
            //   Load a prior team configuraiton.  This will be donoted by the presence of the
            //      "AutoGenFromPastButton" key in postback.
            //   Save team configuration.  If we don't have the above key, then we must be
            //      wanting to do that.
            if (Request.Form.AllKeys.Contains("AutoGenFromPastButton"))
            {
                //we don't want to continue so force success to be false
                WasUpdateSuccessful = false;
                int assignmentId = Assignment.ID;
                Int32.TryParse(Request.Form["AutoGenFromPastSelect"].ToString(), out assignmentId);
                Assignment otherAssignment = db.Assignments.Find(assignmentId);
                SetUpViewBag(otherAssignment.AssignmentTeams.Cast<IAssignmentTeam>().ToList());
            }
            else
            {
                List<IAssignmentTeam> teams = Assignment.DiscussionTeams.Cast<IAssignmentTeam>().ToList();
                ParseFormValues(teams);
                IList<IAssignmentTeam> castedTeams = CastTeamAsConcreteType(teams, typeof(DiscussionTeam));
                Assignment.DiscussionTeams = castedTeams.Cast<DiscussionTeam>().ToList();
                db.SaveChanges();

                //We need to force the update as our model validation fails by default because
                //we're not guaranteeing that the Assignment will be fully represented in our view.
                WasUpdateSuccessful = true;
                SetUpViewBag();
            }
            return base.PostBack(Assignment);
        }
    }
}
