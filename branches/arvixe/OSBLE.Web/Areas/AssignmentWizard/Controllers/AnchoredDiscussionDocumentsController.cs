using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Areas.AssignmentWizard.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using System.IO;
using OSBLE.Controllers;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class AnchoredDiscussionDocumentsController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "AnchoredDiscussionDocuments"; }
        }

        public override string PrettyName
        {
            get
            {
                return "Select Review Documents";
            }
        }

        public override string ControllerDescription
        {
            get { return "This assignment requires that instructors submit one or more files for an Anchored Discussion."; }
        }

        public override IWizardBaseController Prerequisite
        {
            get
            {
                //
                return new BasicsController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                return (new AssignmentTypes[] { AssignmentTypes.AnchoredDiscussion }).ToList();
            }
        }

        /// <summary>
        /// The previous assignment component is required for all assignments listed in ValidAssignmentTypes
        /// </summary>
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
            ModelState.Clear();   
            //Setup new group assignment team and review team.
            Assignment = SetupTeams(Assignment);
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model, IEnumerable<HttpPostedFileBase> files)
        {
            Assignment = db.Assignments.Find(model.ID);

            bool newUpload = false;
            foreach(var file in files)
            {
                if (file != null && file.ContentLength > 0)
                {
                    newUpload = true;
                    break;
                }                    
            }

            if (files != null && newUpload)
            {
                //clear review teams first
                Assignment.ReviewTeams.Clear();
                db.Entry(Assignment).State = System.Data.EntityState.Modified;
                db.SaveChanges();
                //submit file to the system
                SubmissionController submission = new SubmissionController();                
                //removed: ActiveCourseUser.AbstractCourse.ID
                submission.Create(model.ID, files, null);
            }

            WasUpdateSuccessful = true;
            return base.PostBack(model);

        }

        public Assignment SetupTeams(Assignment assignment)
        {
            List<CourseUser> courseUsers = db.CourseUsers.Where(cu => cu.AbstractCourseID == ActiveCourseUser.AbstractCourseID).ToList();
            
            //get old teams for checking membership
            List<AssignmentTeam> oldAssignmentTeams = assignment.AssignmentTeams.ToList();
            //clear old teams
            assignment.AssignmentTeams.Clear();
            //db.Entry(assignment).State = System.Data.EntityState.Modified;
            //db.SaveChanges();

            //make new teams for user listing          
            foreach (CourseUser cu in courseUsers)
            {
                //add if they are not on a review team already
                bool inReviewTeam = false;
                //foreach(ReviewTeam rTeam in assignment.ReviewTeams)
                //{
                //    foreach(AssignmentTeam aTeam in oldAssignmentTeams)
                //    {
                //        foreach(TeamMember tmember in aTeam.Team.TeamMembers)
                //        {
                //            if (tmember.TeamID == rTeam.ReviewTeamID)
                //            {
                //                inReviewTeam = true;                                
                //            }                                
                //        }
                //    }
                //}

                if(!inReviewTeam)
                {
                    Team newTeam = new Team();

                    newTeam.Name = cu.UserProfile.LastAndFirst();

                    newTeam.TeamMembers.Add(new TeamMember
                    {
                        CourseUser = cu,
                        CourseUserID = cu.ID,
                        Team = newTeam,
                        TeamID = newTeam.ID
                    });

                    assignment.AssignmentTeams.Add(new AssignmentTeam
                    {
                        Assignment = assignment,
                        AssignmentID = assignment.ID,
                        Team = newTeam,
                        TeamID = ActiveCourseUser.AbstractCourseID
                    });
                }
                
            }

            db.Entry(assignment).State = System.Data.EntityState.Modified;
            db.SaveChanges();

            return assignment;
        }
    }
}
