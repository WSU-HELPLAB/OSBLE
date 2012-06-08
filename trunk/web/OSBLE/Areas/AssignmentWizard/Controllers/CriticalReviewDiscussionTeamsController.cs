using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;

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

        public override ActionResult Index()
        {
            base.Index();

            //Only creating the discussion teams for the assignment if DiscussionTeams does not exist. 
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
                            if (!alreadyInTeam)
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

            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            Assignment = db.Assignments.Find(model.ID);
            return base.PostBack(Assignment);
        }
    }
}
