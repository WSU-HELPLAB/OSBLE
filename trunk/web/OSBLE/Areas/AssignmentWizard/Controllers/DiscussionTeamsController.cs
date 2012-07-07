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

        public void SetUpModeratorViewBag()
        {
            //Grabbing a list of moderators (and potentially TAs) that will be used to
            //allow instructors to assign to Moderators/TAs to discussion teams
            List<CourseUser> Moderators;
            if (Assignment.DiscussionSettings != null && Assignment.DiscussionSettings.TAsCanPostToAll)
            {
                Moderators = (from cu in db.CourseUsers
                                      where cu.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator
                                      && cu.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                                      orderby cu.UserProfile.LastName, cu.UserProfile.FirstName
                                      select cu).ToList();
                ViewBag.ModeratorListTitle = "Moderators";
            }
            else
            {
                Moderators = (from cu in db.CourseUsers
                                      where (cu.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator
                                      || cu.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
                                      && cu.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                                      orderby cu.UserProfile.LastName, cu.UserProfile.FirstName
                                      select cu).ToList();
                ViewBag.ModeratorListTitle = "Moderators/TAs";
            }
            ViewBag.DisplayModeratorList = "inline";
            if (Moderators.Count == 0)
            {
                ViewBag.DisplayModeratorList = "none";
            }
            ViewBag.Moderators = Moderators;
        }

        public override ActionResult Index()
        {
            base.Index();
            SetUpViewBag(Assignment.DiscussionTeams.Cast<IAssignmentTeam>().ToList());
            SetUpModeratorViewBag();

            return View(Assignment);
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
                if (!Int32.TryParse(key.Split('_')[1], out courseUserId))
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
            //get all moderator form keys
            string[] modKeys = Request.Form.AllKeys.Where(k => k.Contains("moderator_")).ToArray();
            foreach (string key in modKeys)
            {
                //grab the comma seperated string that has all the teams the moderator is on
                string[] TeamList = Request.Form[key].Split(',');

                int courseUserId = 0;
                if (!Int32.TryParse(key.Split('_')[1], out courseUserId)) //if we fail to get a courseUserId, move on.
                {
                    continue;
                }

                for (int i = 0; i < TeamList.Count(); i++)
                {

                    //Here, unlike for students, we will only add the moderators to preexisting teams. 
                    if (TeamList[i] != "")
                    {
                        Team team = teams.Find(t => t.Name.CompareTo(TeamList[i]) == 0);
                        if (team != null)
                        {
                            TeamMember tm = new TeamMember()
                            {
                                CourseUserID = courseUserId,
                                Team = team
                            };
                            team.TeamMembers.Add(tm);
                        }
                    }
                }

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

        [HttpPost]
        public override ActionResult Index(Assignment model)
        {
            //reset our assignment
            Assignment = db.Assignments.Find(model.ID);

            //two postback options: 
            //   Load a prior team configuraiton.  This will be denoted by the presence of the
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
                //clear out old teams.  AC: Not sure why EF isn't handling this automaticaly
                DiscussionTeam[] oldTeams = Assignment.DiscussionTeams.ToArray();
                for (int i = 0; i < oldTeams.Length; i++)
                {
                    db.Entry(oldTeams[i]).State = System.Data.EntityState.Deleted;
                }
                db.SaveChanges();
                Assignment.DiscussionTeams = new List<DiscussionTeam>();
                db.SaveChanges();
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
