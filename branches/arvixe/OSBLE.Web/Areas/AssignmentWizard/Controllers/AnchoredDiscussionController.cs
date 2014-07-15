using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Controllers;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Utility;
using System;
using OSBLE.Models.HomePage;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Attributes;
using OSBLE.Models.FileSystem;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class AnchoredDiscussionController : WizardBaseController
    {
        public override string PrettyName
        {
            get { return "Anchored Discussion Settings"; }
        }

        public override string ControllerName
        {
            get { return "AnchoredDiscussion"; }
        }

        public override string ControllerDescription
        {
            get
            {
                return "This assignment includes one or more documents students can collaboratively annoate";
            }
        }

        public override IWizardBaseController Prerequisite
        {
            get
            {
                //nothing comes before the Anchored Discussion Controller
                //return new BasicsController();
                return new PreviousAssignmentController();
                
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                return (new AssignmentTypes[] { AssignmentTypes.AnchoredDiscussion }).ToList();
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
            //ModelState.Clear();
            //return View(Assignment);
            
            //SETTING UP
            SetUpViewBag();
            if (Assignment.CriticalReviewSettings == null)
            {
                Assignment.CriticalReviewSettings = new CriticalReviewSettings();
                Assignment.CriticalReviewSettings.AssignmentID = Assignment.ID;
                Assignment.CriticalReviewSettings.Assignment = Assignment;
            }
            return View(Assignment.CriticalReviewSettings);
        }

        [HttpPost]
        public virtual ActionResult Index(CriticalReviewSettings model)
        {
            Assignment = db.Assignments.Find(model.AssignmentID);

            //delete preexisting settings to prevent an FK relation issue
            CriticalReviewSettings setting = db.CriticalReviewSettings.Find(model.AssignmentID);
            if (setting != null)
            {
                db.CriticalReviewSettings.Remove(setting);
            }
            db.SaveChanges();

            //...and then re-add it.
            Assignment.CriticalReviewSettings = model;
            db.SaveChanges();

            //Blow out all previous review teams and readd the ones we get from the view.
            //AC Note: This may have to be changed depending on how we implement the new 
            //HTML-based review interface.
            List<ReviewTeam> oldTeams = db.ReviewTeams.Where(rt => rt.AssignmentID == Assignment.ID).ToList();
            foreach (ReviewTeam oldTeam in oldTeams)
            {
                db.ReviewTeams.Remove(oldTeam);
            }
            db.SaveChanges();

            //add new values back in, save again.
            Assignment.ReviewTeams = ParseReviewTeams();
            db.SaveChanges();
            WasUpdateSuccessful = true;

            //This could have been an edit of the CR, and as a reuslt any CRDs associated with the assignment should be updated.
            UpdateCriticalReviewDiscussions();

            SetUpViewBag();



            return base.PostBack(Assignment);
        }

        private void UpdateCriticalReviewDiscussions()
        {
            //Getting a list of CRDs that used this Critical Review
            List<Assignment> relatedCriticalReviewDiscussions = (from a in db.Assignments
                                                                 where a.AssignmentTypeID == (int)AssignmentTypes.CriticalReviewDiscussion &&
                                                                 a.PrecededingAssignmentID == Assignment.ID
                                                                 select a).ToList();

            foreach (Assignment CRDassignment in relatedCriticalReviewDiscussions)
            {
                //The discussion teams from the CRD are potentially orphans, at the end of this iteration, any teams that are still orphans will be deleted.
                List<DiscussionTeam> orphanTeams = CRDassignment.DiscussionTeams.ToList();

                List<int> authorTeamIds = Assignment.ReviewTeams.Select(rt => rt.AuthorTeamID).Distinct().ToList();

                //There will be at most 1 DiscussionTeam for each AuthorTeam. So, looking for existing
                //discussion teams to edit.
                int i = 1;
                foreach (int authorTeamId in authorTeamIds)
                {
                    DiscussionTeam existingDiscussionTeam = orphanTeams.Where(dt => dt.AuthorTeamID == authorTeamId).FirstOrDefault();
                    if (existingDiscussionTeam == null) //There was no team. This could occur if Team X had no reviewers before the edit, and now has reviewers.
                    {
                        //Create new DiscussionTeam and Team for existingDiscussionTeam
                        existingDiscussionTeam = new DiscussionTeam();
                        existingDiscussionTeam.AuthorTeamID = authorTeamId;
                        existingDiscussionTeam.AssignmentID = CRDassignment.ID;

                        Team newTeam = new Team();
                        //Keep trying to generate unique name
                        do
                        {
                            newTeam.Name = "Discussion Team 0" + i;
                            i++;
                        } while (CRDassignment.DiscussionTeams.Where(dt => dt.TeamName == newTeam.Name).Count() > 0);

                        existingDiscussionTeam.Team = newTeam;
                        db.DiscussionTeams.Add(existingDiscussionTeam);
                        db.SaveChanges();
                    }
                    else //Team still exists, remove from orphan list.
                    {
                        orphanTeams.Remove(existingDiscussionTeam);
                    }

                    //ExistingDiscussionTeam.Team.TeamMembers need to be wiped and refreshed with those who exist in the review team
                    existingDiscussionTeam.Team.TeamMembers.Clear();

                    //Add each reviewer to existingDiscussionTeam.Team, only once.
                    //MG: Note, the reason the team Ids are collected and then a db query for the teams is because assignment's aren't properly doing a virtual call
                    //to collect their review teams, as all review teams are null.
                    List<int> reviewTeamIds = Assignment.ReviewTeams.Where(rt => rt.AuthorTeamID == authorTeamId).Select(rt => rt.ReviewTeamID).ToList();
                    List<Team> reviewTeams = (from team in db.Teams
                                              where reviewTeamIds.Contains(team.ID)
                                              select team).ToList();
                    foreach (Team reviewTeam in reviewTeams)
                    {
                        foreach (TeamMember reviewer in reviewTeam.TeamMembers)
                        {
                            //Checking if reviewer is already on team
                            bool alreadyOnTeam = (from tm in existingDiscussionTeam.Team.TeamMembers
                                                  where tm.CourseUserID == reviewer.CourseUserID
                                                  select tm).Count() > 0;

                            //If not on team, Create a new team member for them, and add to existingDiscussionTeam.Team
                            if (alreadyOnTeam == false)
                            {
                                TeamMember newMember = new TeamMember();
                                newMember.CourseUserID = reviewer.CourseUserID;
                                newMember.TeamID = existingDiscussionTeam.TeamID;
                                existingDiscussionTeam.Team.TeamMembers.Add(newMember);
                            }
                        }
                    }
                }

                //Remove any remainig orphanTeams
                foreach (DiscussionTeam orphan in orphanTeams)
                {
                    db.DiscussionTeams.Remove(orphan);
                }
                db.SaveChanges();
            }
        }

        private List<ReviewTeam> ParseReviewTeams()
        {
            List<ReviewTeam> reviewTeams = new List<ReviewTeam>();
            string[] reviewTeamKeys = Request.Form.AllKeys.Where(k => k.Contains("reviewTeam_")).ToArray();
            foreach (string reviewTeamKey in reviewTeamKeys)
            {

                int reviewerId = 0;
                Int32.TryParse(reviewTeamKey.Split('_')[1], out reviewerId);

                //skip bad apples
                if (reviewerId == 0)
                {
                    continue;
                }

                //Loop through each review item.  Review items are contained within the form value separated
                //by underscores (_)
                string reviewItems = Request.Form[reviewTeamKey];
                string[] itemPieces = reviewItems.Split('_');
                foreach (string item in itemPieces)
                {
                    int authorId = 0;
                    Int32.TryParse(item, out authorId);
                    if (authorId > 0)
                    {
                        ReviewTeam activeTeam = new ReviewTeam();
                        activeTeam.ReviewTeamID = reviewerId;
                        activeTeam.AuthorTeamID = authorId;
                        reviewTeams.Add(activeTeam);
                    }
                }
            }
            return reviewTeams;
        }


        private void SetUpViewBag()
        {
            //pull previous team configurations
            List<Assignment> previousTeamAssignments = (from assignment in db.Assignments
                                                        where assignment.Course.ID == ActiveCourseUser.AbstractCourseID
                                                        where assignment.AssignmentTeams.Count > 0
                                                        select assignment).ToList();

            //place items into the viewbag
            ViewBag.Teams = Assignment.AssignmentTeams.Select(t => t.Team).OrderBy(t => t.Name).ToList();
            ViewBag.PreviousTeamAssignments = previousTeamAssignments;
            ViewBag.testing = "BOOYA";
        }

    }
}
