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

        public override string PrettyName
        {
            get
            {
                return "Assignment Teams";
            }
        }

        public override string ControllerDescription
        {
            get { return "The assignment is team-based"; }
        }

        public override WizardBaseController Prerequisite
        {
            get 
            {
                return new BasicsController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                return base.AllAssignmentTypes;
            }
        }

        public override bool IsRequired
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Sets up the viewbag for various controller actions.
        /// </summary>
        /// <param name="assignmentWithTeams">Supply the assignment that you want pull the teams from.
        /// This is used for pulling team configurations from other assignments
        /// </param>
        protected void SetUpViewBag(List<IAssignmentTeam> teams)
        {
            //Guaranteed to pull all people enrolled in the course that can submit files
            //(probably students).
            List<CourseUser> users = (from cu in db.CourseUsers
                                       where cu.AbstractCourseID == ActiveCourse.AbstractCourseID
                                       && cu.AbstractRole.CanSubmit
                                       orderby cu.UserProfile.LastName, cu.UserProfile.FirstName
                                       select cu).ToList();
            List<CourseUser> allUsers = users.ToList();

            //remove students currently on the team list from our complete user list
            foreach (IAssignmentTeam team in teams)
            {
                foreach (TeamMember member in team.Team.TeamMembers)
                {
                    //If we're in a postback condition, our list of teams will include little more than the CourseUserId
                    //As such, we can't access member.CourseUser
                    CourseUser user = users.Find(u => u.ID == member.CourseUserID);
                    users.Remove(user);

                    //add the more detailed CourseUser info to the member
                    member.CourseUser = user;
                }
            }

            //pull previous team configurations
            List<Assignment> previousTeamAssignments = (from assignment in db.Assignments
                                                        where assignment.Category.Course.ID == ActiveCourse.AbstractCourseID
                                                        where assignment.AssignmentTeams.Count > 0
                                                        select assignment).ToList();

            //place items into the viewbag
            ViewBag.AllUsers = allUsers;
            ViewBag.UnassignedUsers = users;
            ViewBag.Teams = teams;
            ViewBag.PreviousTeamAssignments = previousTeamAssignments;
        }

        /// <summary>
        /// Sets up the viewbag for various controller actions.  Will pull team information from
        /// the assignment currently being edited.
        /// </summary>
        protected new void SetUpViewBag()
        {
            SetUpViewBag(Assignment.AssignmentTeams.Cast<IAssignmentTeam>().ToList());
        }

        public override ActionResult Index() 
        {
            base.Index();
            SetUpViewBag();
            return View(Assignment);
        }

        [HttpPost]
        public virtual ActionResult Index(Assignment model)
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
                List<IAssignmentTeam> teams = Assignment.AssignmentTeams.Cast<IAssignmentTeam>().ToList();
                ParseFormValues(teams);
                IList<IAssignmentTeam> castedTeams = CastTeamAsConcreteType(teams, typeof(AssignmentTeam));
                Assignment.AssignmentTeams = castedTeams.Cast<AssignmentTeam>().ToList();
                db.SaveChanges();

                //We need to force the update as our model validation fails by default because
                //we're not guaranteeing that the Assignment will be fully represented in our view.
                WasUpdateSuccessful = true;
                SetUpViewBag();
            }
            return base.PostBack(Assignment);
        }

        /// <summary>
        /// AC: As of 2012-02-27, we have two different team uses (AssignmentTeam, DiscussionTeam).  Both inherit from
        /// IAssignmentTeam, but when creating new objects, I need to instantiate a concrete class.  This method
        /// allows the programmer to convert between concrete implementations of IAssignmentTeam.
        /// </summary>
        /// <param name="genericTeam"></param>
        /// <param name="teamType"></param>
        /// <returns></returns>
        protected IList<IAssignmentTeam> CastTeamAsConcreteType(IList<IAssignmentTeam> genericTeam, Type teamType)
        {
            List<IAssignmentTeam> castedTeams = new List<IAssignmentTeam>();
            foreach (IAssignmentTeam team in genericTeam)
            {
                IAssignmentTeam t = Activator.CreateInstance(teamType) as IAssignmentTeam;
                t.AssignmentID = team.AssignmentID;
                t.Assignment = team.Assignment;
                t.TeamID = team.TeamID;
                t.Team = team.Team;
                castedTeams.Add(t);
            }
            return castedTeams;
        }

        protected void ParseFormValues(IList<IAssignmentTeam> previousTeams)
        {
            //our new list of teams
            List<Team> teams = previousTeams.Select(at => at.Team).ToList();

            //Now that we've captured any existing teams, wipe out the old association
            //as well as the old team members
            previousTeams.Clear();
            foreach (Team team in teams)
            {
                team.TeamMembers.Clear();
            }
            db.SaveChanges();

            //update all prior teams (those already stored in the DB)
            string[] teamKeys = Request.Form.AllKeys.Where(k => k.Contains("team_")).ToArray();
            foreach (string key in teamKeys)
            {
                string teamName = Request.Form[key];
                int teamId = 0;

                //skip any bad apples
                if (!Int32.TryParse(key.Split('_')[1], out teamId))
                {
                    continue;
                }

                //update the team name
                Team team = teams.Find(t => t.ID == teamId);
                if (team == null)
                {
                    continue;
                }
                team.Name = teamName;

                //clear any existing team members
                team.TeamMembers.Clear();

                db.Entry(team).State = System.Data.EntityState.Modified;
            }

            //get all relevant form keys
            string[] keys = Request.Form.AllKeys.Where(k => k.Contains("student_")).ToArray();
            foreach (string key in keys)
            {
                string TeamName = Request.Form[key];
                int courseUserId = 0;
                if(!Int32.TryParse(key.Split('_')[1], out courseUserId))
                {
                    continue;
                }

                //if the team doesn't exist, create it before continuing
                if (teams.Count(t => t.Name.CompareTo(TeamName) == 0) == 0)
                {
                    Team newTeam = new Team()
                    {
                        Name = TeamName
                    };
                    teams.Add(newTeam);
                }
                Team team = teams.Find(t => t.Name.CompareTo(TeamName) == 0);
                TeamMember tm = new TeamMember() 
                { 
                    CourseUserID = courseUserId,
                    Team = team
                };
                team.TeamMembers.Add(tm);
            }

            //Remove any empty teams.  This is a possibility when a team was loaded from
            //the database and then removed using the team creation tool.  Because we
            //retrieved it from the DB and added it to our list of teams, it will exist
            //but it won't have anyone assigned to it.
            Team[] emptyTeams = teams.Where(tm => tm.TeamMembers.Count == 0).ToArray();
            foreach (Team team in emptyTeams)
            {
                teams.Remove(team);

                //remove from the db
                if (team.ID > 0)
                {
                    db.Teams.Remove(team);
                }
            }

            //attach the new teams to the assignment
            foreach (Team team in teams)
            {
                previousTeams.Add(new AssignmentTeam()
                { 
                        Assignment = Assignment, 
                        AssignmentID = Assignment.ID, 
                        Team = team,
                        TeamID = team.ID
                });
            }
        }
    }
}
