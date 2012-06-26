using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class DiscussionTeamsController : TeamController
    {
        public override string ControllerName
        {
            get { return "DiscussionTeams"; }
        }

        public override string PrettyName
        {
            get
            {
                return "Discussion Teams";
            }
        }

        public override string ControllerDescription
        {
            get { return "Students will discuss a topic in teams"; }
        }

        public override WizardBaseController Prerequisite
        {
            get
            {
                return new DiscussionController();
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

            //Grabbing a list of moderators (and potentially TAs) that will be used to
            //allow instructors to assign to Moderators/TAs to discussion teams
            if (Assignment.DiscussionSettings != null && Assignment.DiscussionSettings.TAsCanPostToAll)
            {
                ViewBag.Moderators = (from cu in db.CourseUsers
                                      where cu.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator
                                      && cu.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                                      select cu).ToList();
            }
            else
            {
                ViewBag.Moderators = (from cu in db.CourseUsers
                                      where (cu.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator
                                      || cu.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
                                      && cu.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                                      select cu).ToList();
            }
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
