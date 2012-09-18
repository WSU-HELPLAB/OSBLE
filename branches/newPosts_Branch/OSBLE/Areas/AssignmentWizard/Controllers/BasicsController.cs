using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Controllers;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Utility;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class BasicsController : WizardBaseController
    {
        public override string PrettyName
        {
            get { return "Basic Settings"; }
        }

        public override string ControllerName
        {
            get { return "Basics"; }
        }

        public override string ControllerDescription
        {
            get
            {
                return "Basic assignment information";
            }
        }

        public override WizardBaseController Prerequisite
        {
            get
            {
                //nothing comes before the Basics Controller
                return null;
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

        public override ActionResult Index()
        {
            base.Index();
            ModelState.Clear();
            Assignment.Type = manager.ActiveAssignmentType;

            //If the assignment is new and has default zero values, overwrite them with Course default late penalty values
            if (Assignment.HoursLateWindow == 0 && Assignment.DeductionPerUnit == 0 && Assignment.HoursPerDeduction == 0)
            {
                Assignment.HoursPerDeduction = (ActiveCourseUser.AbstractCourse as Course).HoursLatePerPercentPenalty;
                Assignment.DeductionPerUnit = (ActiveCourseUser.AbstractCourse as Course).PercentPenalty;
                Assignment.HoursLateWindow = (ActiveCourseUser.AbstractCourse as Course).HoursLateUntilZero;
            }
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            
            Assignment = model;
            if (ModelState.IsValid)
            {
                WasUpdateSuccessful = true;

                Assignment.CourseID = ActiveCourseUser.AbstractCourseID;

                //update our DB
                if (Assignment.ID == 0)
                {
                    //Add default assignment teams
                    SetUpDefaultAssignmentTeams(Assignment);

                    db.Assignments.Add(Assignment);
                }
                else //editing preexisting assingment
                {

                    if (Assignment.AssociatedEventID.HasValue)
                    {
                        //If the assignment is being edited, update it's associated event.
                        OSBLE.Models.HomePage.Event assignmentsEvent = db.Events.Find(Assignment.AssociatedEventID);
                        if (assignmentsEvent != null)
                        {
                            assignmentsEvent.Description = Assignment.AssignmentDescription;
                            assignmentsEvent.EndDate = Assignment.DueDate;
                            assignmentsEvent.EndTime = Assignment.DueTime;
                            assignmentsEvent.StartDate = Assignment.ReleaseDate;
                            assignmentsEvent.StartTime = Assignment.ReleaseTime;
                            assignmentsEvent.Title = Assignment.AssignmentName;
                            db.Entry(assignmentsEvent).State = System.Data.EntityState.Modified;
                        }

                        bool assignmentTeamsExist = (from at in db.AssignmentTeams
                                                     where at.AssignmentID == Assignment.ID
                                                     select at).Count() > 0;
                        if (!assignmentTeamsExist)
                        {
                            //No teams, so add default assignment teams
                            SetUpDefaultAssignmentTeams(Assignment);
                        }
                    }
                    db.Entry(Assignment).State = System.Data.EntityState.Modified;
                }
                try
                {
                    db.SaveChanges();
                }
                catch (DbEntityValidationException dbEx)
                {
                    foreach (var validationErrors in dbEx.EntityValidationErrors)
                    {
                        foreach (var validationError in validationErrors.ValidationErrors)
                        {
                            Trace.TraceInformation("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                        }
                    }
                }
            }
            else
            {
                WasUpdateSuccessful = false;
            }
            return base.PostBack(Assignment);
        }

        /// <summary>
        /// By design, all students must be part of their own individual assignment team. 
        /// But, some assignments (I.e. Discussion type assignments, or team evaluations) should not be taken the TeamController to set up the asisngment teams. 
        /// This will set the teams to the default individual assignment teams.
        /// </summary>
        void SetUpDefaultAssignmentTeams(Assignment assignment)
        {
            List<CourseUser> users = (from cu in db.CourseUsers
                            where cu.AbstractCourseID == ActiveCourseUser.AbstractCourseID
                            && cu.AbstractRole.CanSubmit
                            orderby cu.UserProfile.LastName, cu.UserProfile.FirstName
                            select cu).ToList();

            //Creates an assignment team for each CourseUser who can submit documents (students)
            //The team name will be "FirstName LastName"
            foreach (CourseUser cu in users)
            {
                //Creating team
                Team team = new Team();
                team.Name = cu.UserProfile.FirstName + " " + cu.UserProfile.LastName;

                //Creating Tm and adding them to team
                TeamMember tm = new TeamMember(){
                    CourseUserID = cu.ID
                };
                team.TeamMembers.Add(tm);

                //Creating the assignment team and adding it to the assignment
                AssignmentTeam at = new AssignmentTeam()
                {
                    Team = team,
                    Assignment = assignment
                };
                Assignment.AssignmentTeams.Add(at);
            }
        }
    }
}