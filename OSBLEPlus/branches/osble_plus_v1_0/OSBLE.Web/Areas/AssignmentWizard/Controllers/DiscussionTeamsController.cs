using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Utility;

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

        public override IWizardBaseController Prerequisite
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
            SetUpViewBag(Assignment.DiscussionTeams.Cast<IAssignmentTeam>().ToList());
            SetUpModeratorViewBag();
            return View(Assignment);
        }

        /// <summary>
        /// Updates discussion teams based off any name changes or team member changes. Creates new teams if needed and purges empty teams.
        /// </summary>
        protected void ParseFormValues()
        {
            //Wiping all DiscussionTeam.TeamMembers.
            foreach (DiscussionTeam dt in Assignment.DiscussionTeams)
            {
                dt.Team.TeamMembers.Clear();
            }
            db.SaveChanges();

            if (Request.Form.AllKeys.Contains("AutoGenFromPastButton"))
            {
                //we don't want to continue so force success to be false
                WasUpdateSuccessful = false;
                int assignmentId = Assignment.ID;
                Int32.TryParse(Request.Form["AutoGenFromPastSelect"].ToString(), out assignmentId);
                Assignment otherAssignment = db.Assignments.Find(assignmentId);
                List<IAssignmentTeam> otherTeams = null;
                switch (otherAssignment.Type)
                {
                    case AssignmentTypes.DiscussionAssignment:
                    case AssignmentTypes.CriticalReviewDiscussion:
                        otherTeams = RemoveWithdrawnMembers(otherAssignment.DiscussionTeams.Cast<IAssignmentTeam>().ToList(), "Discussion");
                        break;

                    default:
                        otherTeams = RemoveWithdrawnMembers(otherAssignment.AssignmentTeams.Cast<IAssignmentTeam>().ToList(), "Assignment");
                        break;
                }
                SetUpViewBag(otherTeams);
            }
            else
            {
                string[] studentKeys = Request.Form.AllKeys.Where(k => k.Contains("student_")).ToArray();
                List<string> TeamNames = new List<string>(); ; //This will be a list of the TeamNames from the form. These can be pre-existing teams or new teams.

                foreach (string studentKey in studentKeys)
                {
                    TeamNames.Add(Request.Form[studentKey]);
                }

                string[] PreExistingTeamKeys = Request.Form.AllKeys.Where(k => k.Contains("team_")).ToArray(); //These are keys for teams that  already existed
                foreach (string preExistingTeamKey in PreExistingTeamKeys) //Determining if each preexisting team should be kept or deleted
                {
                    string TeamName = Request.Form[preExistingTeamKey];
                    int TeamID;
                    Int32.TryParse(preExistingTeamKey.Split('_')[1], out TeamID);

                    Team team = Assignment.DiscussionTeams.Where(dt => dt.TeamID == TeamID).Select(dt => dt.Team).FirstOrDefault();

                    if (TeamNames.Contains(TeamName)) //The TeamName corrisponds to a team that a student is on.
                    {
                        //Save new name if team exists, in case of rename
                        if (team != null)
                        {
                            team.Name = TeamName;
                            db.SaveChanges();
                        }
                        //If the team has the same ID, then its safe to keep. If it does not, we must delete it, as it
                        //has been deleted by the user and recreated with the same name. Meaning a complete different team.

                        bool TeamHasSameID = Assignment.DiscussionTeams.Where(dt => dt.TeamID == TeamID).Count() > 0;
                        if (TeamHasSameID == false)
                        {
                            //Team has a new ID, but a pre-existing name. Team must have been delete/recreated. So we must 
                            //do the same
                            if (team != null)
                            {
                                db.Teams.Remove(team);
                                db.SaveChanges();
                            }
                        }
                    }
                    else //The TeamName does not corrispond with any Team a student is associated with. So delete it
                    {
                        if (team != null)
                        {
                            DiscussionTeam temp = (from dt in db.DiscussionTeams
                                                   where dt.TeamID == team.ID
                                                   select dt).FirstOrDefault();
                            db.DiscussionTeams.Remove(temp);
                            db.SaveChanges();


                            db.Teams.Remove(team);
                            db.SaveChanges();
                        }
                    }
                }



                //At this point, all old teams have been purged. So any team from the studentKeys values are either
                //new teams, or preexisting teams.
                foreach (string studentKey in studentKeys)
                {
                    Team team = Assignment.DiscussionTeams.Where(dt => dt.TeamName == Request.Form[studentKey]).Select(dt => dt.Team).FirstOrDefault();

                    if (team == null) //a new team. Create the team and discussion team before continuing.
                    {
                        team = new Team();
                        team.Name = Request.Form[studentKey];
                        db.Teams.Add(team);
                        db.SaveChanges();

                        DiscussionTeam dt = new DiscussionTeam();
                        dt.TeamID = team.ID;
                        dt.AssignmentID = Assignment.ID;
                        db.DiscussionTeams.Add(dt);
                        db.SaveChanges();
                    }

                    int courseUserId = 0;
                    Int32.TryParse(studentKey.Split('_')[1], out courseUserId);

                    // Copy any initial posts made on a different team
                    CopyInitialPosts(team, courseUserId);

                    TeamMember tm = new TeamMember();
                    tm.TeamID = team.ID;
                    tm.CourseUserID = courseUserId;
                    db.TeamMembers.Add(tm);
                    db.SaveChanges();
                }

            }

            //get all moderator form keys
            string[] modKeys = Request.Form.AllKeys.Where(k => k.Contains("moderator_")).ToArray();
            List<Team> teams = Assignment.DiscussionTeams.Select(dt => dt.Team).ToList();
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
                            db.SaveChanges(); //save new members onto team
                        }
                    }
                }
            }

            //Checking for empty teams one last time to be sure
            List<DiscussionTeam> dtsToRemove = Assignment.DiscussionTeams.Where(dt => dt.Team.TeamMembers.Count == 0).ToList();
            for (int i = dtsToRemove.Count - 1; i >= 0; i--)
            {
                db.DiscussionTeams.Remove(dtsToRemove[i]);
            }
            db.SaveChanges();

        }

        // CT: Work in progress
        private void CopyInitialPosts(Team team, int courseUserId)
        {
            using (var connection = DBHelper.GetNewConnection())
            {
                // Get the DiscussionTeamID from the team ID
                var discussionTeamID = DBHelper.GetDiscussionTeamIDFromTeamID(team.ID, connection);
                
                // Check to see if this user has made any posts as part of this team, if they have, then they
                // haven't been switched from a previous team, and we don't need to worry about copying their posts. 
                // This also helps to iliminate duplicating posts more than once.
                var existingPosts = DBHelper.GetDiscussionPosts(courseUserId, discussionTeamID, connection);
                if (existingPosts.Any())
                    return;
                
                // Get all initial posts made by this user for this assignment (note: initial posts have null ParentPostID)
                var initialPosts = DBHelper.GetInitialDiscussionPosts(courseUserId, Assignment.ID, connection);

                // Change the DiscussionTeamID to corespond to the new team
                foreach (DiscussionPost post in initialPosts)
                    post.DiscussionTeamID = discussionTeamID;

                // Copy all initial posts by re-inserting them with the new DiscussionTeamID
                DBHelper.InsertDiscussionPosts(initialPosts, connection);
            }
        }

        [HttpPost]
        public override ActionResult Index(Assignment model)
        {
            //reset our assignment
            Assignment = db.Assignments.Find(model.ID);
            ParseFormValues();

            return base.PostBack(Assignment);
        }
    }
}
