using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class CriticalReviewController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "CriticalReview"; }
        }

        public override string PrettyName
        {
            get
            {
                return "Critical Review";
            }
        }

        public override string ControllerDescription
        {
            get { return "Assign students or teams to review documents"; }
        }

        public override WizardBaseController Prerequisite
        {
            get
            {
                return new CriticalReviewSettingsController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                return (new AssignmentTypes[] { AssignmentTypes.CriticalReview }).ToList();
            }
        }

        public override bool IsRequired
        {
            get
            {
                return true;
            }
        }

        private new void SetUpViewBag()
        {
            //pull previous team configurations
            List<Assignment> previousTeamAssignments = (from assignment in db.Assignments
                                                        where assignment.Course.ID == ActiveCourseUser.AbstractCourseID
                                                        where assignment.AssignmentTeams.Count > 0
                                                        select assignment).ToList();

            //place items into the viewbag
            ViewBag.Teams = Assignment.AssignmentTeams.Select(t => t.Team).OrderBy(t => t.Name).ToList();
            ViewBag.PreviousTeamAssignments = previousTeamAssignments;
        }

        public override ActionResult Index()
        {
            base.Index();
            SetUpViewBag();
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
            foreach(ReviewTeam oldTeam in oldTeams)
            {
                db.ReviewTeams.Remove(oldTeam);
            }
            db.SaveChanges();

            //add new values back in, save again.
            Assignment.ReviewTeams = ParseReviewTeams();
            db.SaveChanges();
            WasUpdateSuccessful = true;
            SetUpViewBag();
            return base.PostBack(Assignment);
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
    }
}
