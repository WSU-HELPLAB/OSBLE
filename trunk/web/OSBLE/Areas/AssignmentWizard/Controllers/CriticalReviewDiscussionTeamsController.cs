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

            List<DiscussionTeam> discussionTeams = new List<DiscussionTeam>();
            Dictionary<int, Team> TeamDict = new Dictionary<int, Team>();

            List<int> authorTeamIDs = (from rt in Assignment.PreceedingAssignment.ReviewTeams
                                       select rt.AuthorTeamID).Distinct().ToList();

            int i = 0;
            //Right now, were not adding the authorteam into the discussion team created. Find them and insert them
            foreach (int authorTeamID in authorTeamIDs)
            {
                DiscussionTeam dt = new DiscussionTeam();
                Team team = new Team();

                List<Team> reviewTeams = (from rt in Assignment.PreceedingAssignment.ReviewTeams
                                       where rt.AuthorTeamID == authorTeamID
                                       select rt.ReviewingTeam).ToList();

                foreach (Team reviewerTeam in reviewTeams)
                {
                    foreach (TeamMember tm in reviewerTeam.TeamMembers)
                    {
                        TeamMember newTm = new TeamMember();
                        newTm.CourseUserID = tm.CourseUserID;
                        newTm.Team = team;
                        if (!team.TeamMembers.Contains(newTm)) //Only adding team members once
                        {
                            team.TeamMembers.Add(newTm);
                        }
                    }
                }

                team.Name = "Team " + i.ToString();
                i++;
                dt.Team = team;
                dt.AssignmentID = Assignment.ID;
                discussionTeams.Add(dt);
            }

            Assignment.DiscussionTeams = discussionTeams;
            db.SaveChanges();

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
