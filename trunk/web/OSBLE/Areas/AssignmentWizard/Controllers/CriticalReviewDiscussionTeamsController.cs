using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class CriticalReviewDiscussionTeamsController : WizardBaseController
    {

        public override string ControllerName
        {
            get { return "CriticalReviewDiscussionTeams"; }
        }

        public override string PrettyName
        {
            get
            {
                return "CR Discussion Teams";
            }
        }

        public override string ControllerDescription
        {
            get { return "CriticalReviewDiscussionTeams"; }
        }

        public override bool IsRequired
        {
            get
            {
                return true;
            }
        }

        public override WizardBaseController Prerequisite
        {
            get { return new PreviousAssignmentController(); }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                List<AssignmentTypes> prereqs = new List<AssignmentTypes>();

                prereqs.Add(AssignmentTypes.CriticalReviewDiscussion);

                return prereqs;
            }
        }

        public void SetUpModeratorViewBag()
        {
            //Grabbing a list of moderators (and potentially TAs) that will be used to
            //allow instructors to assign to Moderators/TAs to discussion teams
            List<CourseUser> Moderators;
            if (Assignment.DiscussionSettings != null && Assignment.DiscussionSettings.TAsCanPostToAllDiscussions)
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
            ViewBag.Moderators = Moderators;

            //Setting up css for displaying moderators
            ViewBag.DisplayModeratorList = "inline";
            if (Moderators.Count == 0)
            {
                ViewBag.DisplayModeratorList = "none";
            }
            
        }

        public override ActionResult Index()
        {
            base.Index();

            //Only creating the discussion teams for the assignment if DiscussionTeams do not exist. 
            if (Assignment.DiscussionTeams == null || Assignment.DiscussionTeams.Count == 0)
            {

                List<DiscussionTeam> discussionTeams = new List<DiscussionTeam>();
                Dictionary<int, Team> TeamDict = new Dictionary<int, Team>();

                List<Team> authorTeams = (from rt in Assignment.PreceedingAssignment.ReviewTeams
                                          select rt.AuthorTeam).Distinct().ToList();

                int i = 1;
                //Foreach author team, creating a new DiscussionTeam who consists of all the people
                //who reviewed that author team as well as that author team.
                foreach (Team authorTeam in authorTeams)
                {
                    DiscussionTeam dt = new DiscussionTeam();
                    Team team = new Team();

                    List<Team> reviewTeams = (from rt in Assignment.PreceedingAssignment.ReviewTeams
                                              where rt.AuthorTeamID == authorTeam.ID
                                              select rt.ReviewingTeam).ToList();

                    //Adding all the reviewing team members to the Team
                    foreach (Team reviewerTeam in reviewTeams)
                    {
                        //Generating a list of all team members in the review team and the author team
                        List<TeamMember> newTeamMembers = reviewerTeam.TeamMembers.ToList();

                        //Going through all the team members, creating new team members out of them and associating them with our newly created team.
                        foreach (TeamMember tm in newTeamMembers)
                        {
                            TeamMember newTm = new TeamMember();
                            newTm.CourseUserID = tm.CourseUserID;
                            newTm.Team = team;
                            newTm.CourseUser = tm.CourseUser;

                            //Checking to see if member already exists in team. Only adding them if they are not on team. 
                            bool alreadyInTeam = false;
                            foreach (TeamMember currentMember in team.TeamMembers)
                            {
                                if (currentMember.CourseUserID == newTm.CourseUserID)
                                {
                                    alreadyInTeam = true;
                                }
                            }
                            if (alreadyInTeam == false)
                            {
                                team.TeamMembers.Add(newTm);
                            }
                        }
                    }

                    dt.AuthorTeam = authorTeam;
                    dt.AuthorTeamID = authorTeam.ID;

                    //Naming the team and associating the team with a discussion team. Note: Nameing is: Discussion Team 01...02..10..11...
                    if (i < 10)
                    {
                        team.Name = "Discussion Team 0" + i.ToString();
                    }
                    else
                    {
                        team.Name = "Discussion Team " + i.ToString();
                    }
                    
                    i++;
                    dt.Team = team;
                    dt.AssignmentID = Assignment.ID;
                    discussionTeams.Add(dt);
                }

                //Associating the list of discussion teams with the Critical Review Discussion assignment
                Assignment.DiscussionTeams = discussionTeams;
                db.SaveChanges();
            }

            ViewBag.criticalReviewDiscussionTeams = Assignment.DiscussionTeams;
            SetUpModeratorViewBag();
            return View(Assignment);
        }


        //For Critical Review Discussions, teams are never added. They are all built prior to the view.
        //Because of this assumption, we only have to watch for Moderators/TAs being added to an existing team
        //and a team's name changing.
        protected void ParseFormValues()
        {
            //collecting list of TeamMembers from the assignment that are Moderators or TAs
            List<TeamMember> ModeratorAndTATeamMembers = Assignment.DiscussionTeams.SelectMany(dt => dt.Team.TeamMembers)
                                                            .Where(tm => (tm.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA) 
                                                                    || (tm.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator)).ToList();
            foreach(TeamMember tm in ModeratorAndTATeamMembers)
            {
                db.TeamMembers.Remove(tm);
            }

            //Setting team names
            string[] teamKeys = Request.Form.AllKeys.Where(k => k.Contains("discussionTeamName_")).ToArray();
            foreach (string key in teamKeys)
            {
                string TeamName = Request.Form[key];
                int discTeamID = 0;

                if (!Int32.TryParse(key.Split('_')[1], out discTeamID))
                {
                    //Should never get here, but skip in case
                    continue;
                }

                DiscussionTeam currentDt = Assignment.DiscussionTeams.Where(dt => dt.ID == discTeamID).FirstOrDefault();
                if (currentDt == null)
                {
                    //Should never get here, but skip in case
                    continue;
                }

                //Assigning name
                currentDt.Team.Name = TeamName;
            }

            //grabbing all hidden moderator keys
            string[] moderatorKeys = Request.Form.AllKeys.Where(k => k.Contains("moderator_")).ToArray();
            foreach (string key in moderatorKeys)
            {

                int courseUserId = 0;
                if (!Int32.TryParse(key.Split('_')[1], out courseUserId)) 
                {
                    //if we fail to get a courseUserId, move on.
                    continue;
                }

                string[] discussionTeamIDList = Request.Form[key].Split(',');
                foreach (string discussionTeamId in discussionTeamIDList)
                {
                    int discTeamID = 0;
                    if (!Int32.TryParse(discussionTeamId, out discTeamID))
                    {
                        //Should never get here, but skip in case
                        continue;
                    }

                    Team TeamForNewTM = Assignment.DiscussionTeams.Where(dt => dt.ID == discTeamID).Select(dt=>dt.Team).FirstOrDefault();

                    //Can safely add the new TM to the team without fear of duplication because TA/moderators are removed from all teams initially
                    TeamMember newTM = new TeamMember()
                    {
                        Team = TeamForNewTM,
                        CourseUserID = courseUserId
                    };

                    TeamForNewTM.TeamMembers.Add(newTM);
                }
            }
            db.SaveChanges();
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = db.Assignments.Find(model.ID);

            ParseFormValues();

            return base.PostBack(Assignment);
        }
    }
}
