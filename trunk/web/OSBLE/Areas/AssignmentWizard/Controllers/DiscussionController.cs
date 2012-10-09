using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Controllers;
using OSBLE.Models.HomePage;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class DiscussionController : WizardBaseController
    {
        public override string ControllerName
        {
            get { return "Discussion"; }
        }

        public override string PrettyName
        {
            get
            {
                return "Discussion Settings";
            }
        }


        public override string ControllerDescription
        {
            get { return "This assignment has students discuss one or more topics."; }
        }

        public override WizardBaseController Prerequisite
        {
            get
            {
                return new TeamController();
            }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                return (new AssignmentTypes[] { AssignmentTypes.DiscussionAssignment, AssignmentTypes.CriticalReviewDiscussion }).ToList();
            }
        }

        /// <summary>
        /// The discussion component is required for discussion assignments
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
            if (Assignment.DiscussionSettings == null)
            {
                Assignment.DiscussionSettings = new DiscussionSetting();
                Assignment.DiscussionSettings.InitialPostDueDate = Assignment.ReleaseDate.Add(new TimeSpan(7, 0, 0, 0, 0));
                Assignment.DiscussionSettings.AssignmentID = Assignment.ID;
            }
            return View(Assignment.DiscussionSettings);
        }

        [HttpPost]
        public ActionResult Index(DiscussionSetting model)
        {
            Assignment = db.Assignments.Find(model.AssignmentID);
            if (ModelState.IsValid)
            {
                //delete preexisting settings to prevent an FK relation issue
                DiscussionSetting setting = db.DiscussionSettings.Find(model.AssignmentID);
                Event eventToUpdate = null;
                if (setting != null)
                {
                    eventToUpdate = setting.AssociatedEvent;
                    db.DiscussionSettings.Remove(setting);
                }
                db.SaveChanges();

                //...and then re-add it.
                Assignment.DiscussionSettings = model;
                db.SaveChanges();

                
                if (Assignment.IsDraft == false)
                {
                    //If the assignment is published, that means there was an associted event, but was deleted when DiscussionSettings was removed
                    //Re-adding event
                    EventController.UpdateDiscussionEvent(Assignment.DiscussionSettings, eventToUpdate, ActiveCourseUser.ID, db);
                }
                

                //If TAs are allowed to post to all discussion, 
                //remove them from any discussion teams they may have already been assigned to
                if (model.TAsCanPostToAllDiscussions == true)      
                {
                    foreach (DiscussionTeam dt in Assignment.DiscussionTeams)
                    {
                        List<TeamMember> tmsToRemove = new List<TeamMember>();
                        foreach (TeamMember tm in dt.Team.TeamMembers.Where(tm => tm.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA))
                        {
                            tmsToRemove.Add(tm);
                        }
                        foreach (TeamMember tm in tmsToRemove)
                        {
                            dt.Team.TeamMembers.Remove(tm);
                        }
                    }

                }
                db.SaveChanges();
                WasUpdateSuccessful = true;
            }
            else
            {
                WasUpdateSuccessful = false;
            }
            return base.PostBack(Assignment.DiscussionSettings);
        }
    }
}
