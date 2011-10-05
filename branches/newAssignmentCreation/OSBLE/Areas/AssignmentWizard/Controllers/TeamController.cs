﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class TeamController : WizardBaseController
    {

        public override string ControllerName
        {
            get { return "Team"; }
        }

        public override string ControllerDescription
        {
            get { return "The assignment is team-based"; }
        }

        public override ICollection<WizardBaseController> Prerequisites
        {
            get 
            {
                List<WizardBaseController> prereqs = new List<WizardBaseController>();
                prereqs.Add(new BasicsController());
                return prereqs;
            }
        }

        private void SetUpViewBag()
        {
            //Guaranteed to pull all people enrolled in the course that can submit files
            //(probably students).
            List<CourseUsers> users = (from cu in db.CourseUsers
                                       where cu.AbstractCourseID == activeCourse.AbstractCourseID
                                       && cu.AbstractRole.CanSubmit
                                       select cu).ToList();
            List<CourseUsers> allUsers = users.ToList();

            //We'll need to cross the current teams with the list of course users
            List<AssignmentTeam> teams = Assignment.AssignmentTeams.ToList();

            //remove students currently on the team list from our complete user list
            foreach (AssignmentTeam team in teams)
            {
                foreach (TeamMember member in team.Team.TeamMembers)
                {
                    //If we're in a postback condition, our list of teams will include little more than the CourseUserId
                    //As such, we can't access member.CourseUser
                    CourseUsers user = users.Find(u => u.ID == member.CourseUserID);
                    users.Remove(user);

                    //add the more detailed CourseUsers info to the member
                    member.CourseUser = user;
                }
            }

            //place items into the viewbag
            ViewBag.AllUsers = allUsers;
            ViewBag.UnassignedUsers = users;
            ViewBag.Teams = teams;
        }

        public override ActionResult Index() 
        {
            base.Index();
            SetUpViewBag();
            return View(Assignment);
        }

        [HttpPost]
        public ActionResult Index(Assignment model)
        {
            //reset our assignment
            Assignment = db.Assignments.Find(model.ID);
            ParseFormValues();
            db.SaveChanges();

            //We need to force the update as our model validation fails by default because
            //we're not guaranteeing that the Assignment will be fully represented in our view.
            WasUpdateSuccessful = true;
            SetUpViewBag();
            return base.Index(Assignment);
        }

        private void ParseFormValues()
        {
            //clear any previous team configuration as we'll be creating a new one
            Assignment.AssignmentTeams.Clear();

            //our new list of teams
            List<Team> teams = new List<Team>();

            //get all relevant form keys
            string[] keys = Request.Form.AllKeys.Where(k => k.Contains("student_")).ToArray();
            foreach (string key in keys)
            {
                string TeamName = Request.Form[key];
                int courseUserId = 0;
                if(!Int32.TryParse(key.Split('_')[1], out courseUserId))
                {
                    continue;
                }
                if (teams.Count(t => t.Name.CompareTo(TeamName) == 0) == 0)
                {
                    Team newTeam = new Team()
                    {
                        Name = TeamName
                    };
                    teams.Add(newTeam);
                }
                Team team = teams.Find(t => t.Name.CompareTo(TeamName) == 0);
                TeamMember tm = new TeamMember() 
                { 
                    CourseUserID = courseUserId,
                    Team = team
                };
                team.TeamMembers.Add(tm);
            }

            //attach the new teams to the assignment
            foreach (Team team in teams)
            {
                Assignment.AssignmentTeams.Add(new AssignmentTeam() { Assignment = Assignment, Team = team });
            }
        }
    }
}
