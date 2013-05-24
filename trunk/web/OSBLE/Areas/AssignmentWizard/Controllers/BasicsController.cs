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

                //account for local client time, reset to UTC
                int utcOffset = 0;
                try
                {
                    Int32.TryParse(Request.Form["utc-offset"].ToString(), out utcOffset);
                }
                catch (Exception)
                {
                }
                Assignment.ReleaseDate = Assignment.ReleaseDate.AddMinutes(utcOffset);
                Assignment.DueDate = Assignment.DueDate.AddMinutes(utcOffset);

                //update our DB
                if (Assignment.ID == 0)
                {
                    //Add default assignment teams
                    SetUpDefaultAssignmentTeams();
                    db.Assignments.Add(Assignment);
                }
                else //editing preexisting assingment
                {
                    if (Assignment.AssociatedEventID.HasValue)
                    {
                        //If the assignment is being edited, update it's associated event.
                        Event assignmentsEvent = db.Events.Find(Assignment.AssociatedEventID.Value);
                        EventController.UpdateAssignmentEvent(Assignment, assignmentsEvent, ActiveCourseUser.ID, db);
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
        /// 
        /// This will set the teams to the default individual assignment teams.
        /// If teams already exist, it does nothing.
        /// </summary>
        void SetUpDefaultAssignmentTeams()
        {

            bool assignmentTeamsExist = (from at in db.AssignmentTeams
                                                     where at.AssignmentID == Assignment.ID
                                                     select at).Count() > 0;
            //Only set up default teams if teams don't already exist
            if (Assignment.ID == 0 || !assignmentTeamsExist)
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
                    TeamMember tm = new TeamMember()
                    {
                        CourseUserID = cu.ID
                    };
                    team.TeamMembers.Add(tm);

                    //Creating the assignment team and adding it to the assignment
                    AssignmentTeam at = new AssignmentTeam()
                    {
                        Team = team,
                        Assignment = Assignment
                    };
                    Assignment.AssignmentTeams.Add(at);
                }
            }
        }
    }
}