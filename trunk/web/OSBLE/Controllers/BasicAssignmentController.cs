using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Newtonsoft.Json;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class BasicAssignmentController : OSBLEController
    {
        public BasicAssignmentController()
            : base()
        {
            ViewBag.CurrentTab = "Assignments";
        }

        //
        // GET: /Assignment/

        /*
        public ViewResult Index()
        {
            //This is what it was but no longer works
            var abstractgradables = db.AbstractGradables.ToList();
            return View(abstractgradables);
            //return View();
        }*/

        //
        // GET: /Assignment/Details/5

        /*
        public ViewResult Details(int id)
        {
            SubmissionActivity assignment = db.AbstractGradables.Find(id) as SubmissionActivity;
            return View(assignment);
        }
        */
        //
        // GET: /Assignment/Create

        [CanModifyCourse]
        public ActionResult Create()
        {
            //we create a basic assignment that is a StudioAssignment with a submission and a stop
            List<Deliverable> Deliverables = new List<Deliverable>();
            BasicAssignmentViewModel viewModel = new BasicAssignmentViewModel();

            // Copy default Late Policy settings
            Course active = activeCourse.Course as Course;
            viewModel.Submission.HoursLatePerPercentPenalty = active.HoursLatePerPercentPenalty;
            viewModel.Submission.HoursLateUntilZero = active.HoursLateUntilZero;
            viewModel.Submission.PercentPenalty = active.PercentPenalty;
            viewModel.Submission.MinutesLateWithNoPenalty = active.MinutesLateWithNoPenalty;

            viewModel.TeamCreation = createTeamCreationSilverlightObject();

            List<SerializableTeamMember> teamMembmers = new List<SerializableTeamMember>();

            var couresesUsers = (from c in db.CoursesUsers
                                 where c.CourseID == activeCourse.CourseID
                                 && (c.CourseRole.ID == (int)CourseRole.OSBLERoles.Student)
                                 select c).ToList();

            int i = 0;

            if (couresesUsers.Count >= 2)
            {
                foreach (CoursesUsers cu in couresesUsers)
                {
                    SerializableTeamMember teamMember = new SerializableTeamMember();
                    teamMember.IsModerator = cu.CourseRole.ID == (int)CourseRole.OSBLERoles.Moderator;
                    teamMember.Name = cu.UserProfile.FirstName + " " + cu.UserProfile.LastName;
                    teamMember.Section = cu.Section;
                    teamMember.UserID = cu.UserProfileID;
                    teamMember.isUser = true;

                    ///////////////////////TEST/////////////////////////
                    if (i % 2 == 0)
                    {
                        teamMember.Subbmitted = true;
                    }
                    i++;
                    ///////////////////////////////////////////////////

                    teamMembmers.Add(teamMember);
                }

                viewModel.SerializedTeamMembersJSON = viewModel.TeamCreation.Parameters["teamMembers"] = Uri.EscapeDataString(JsonConvert.SerializeObject(teamMembmers));
            }
            else
            {
                viewModel.SerializedTeamMembersJSON = viewModel.TeamCreation.Parameters["teamMembers"] = null;
            }

            ViewBag.Categories = new SelectList(db.Categories, "ID", "Name");
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");
            ViewBag.Deliverables = Deliverables;
            return View(viewModel);
        }

        //
        // POST: /Assignment/Create

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Create(BasicAssignmentViewModel basic)
        {
            string serializedTeams = null;
            try
            {
                serializedTeams = Uri.UnescapeDataString(Request.Params["newTeams"]);
            }
            catch
            {
                serializedTeams = null;
            }

            if (basic.Submission.isTeam)
            {
                if (serializedTeams == null || serializedTeams == "")
                {
                    ModelState.AddModelError("Team", "Using teams was selected but no teams were created, please create the teams");
                }
            }

            if (basic.Submission.ReleaseDate >= basic.Stop.ReleaseDate)
            {
                ModelState.AddModelError("time", "The due date must come after the release date");
            }

            if (ModelState.IsValid)
            {
                SubmissionActivity submission = new SubmissionActivity();
                StopActivity stop = new StopActivity();

                submission.ReleaseDate = basic.Submission.ReleaseDate;
                submission.Name = basic.Submission.Name;

                submission.PointsPossible = basic.Submission.PointsPossible;

                submission.HoursLatePerPercentPenalty = basic.Submission.HoursLatePerPercentPenalty;
                submission.HoursLateUntilZero = basic.Submission.HoursLateUntilZero;
                submission.PercentPenalty = basic.Submission.PercentPenalty;
                submission.MinutesLateWithNoPenalty = basic.Submission.MinutesLateWithNoPenalty;

                submission.isTeam = basic.Submission.isTeam;

                stop.Name = basic.Stop.Name;
                stop.ReleaseDate = basic.Stop.ReleaseDate;

                basic.Assignment.AssignmentActivities.Add(submission);
                basic.Assignment.AssignmentActivities.Add(stop);

                db.BasicAssignments.Add(basic.Assignment);

                db.SaveChanges();

                db.AssignmentActivities.Add(submission);
                db.AssignmentActivities.Add(stop);

                db.SaveChanges();

                if (basic.Submission.isTeam)
                {
                    List<SerializableTeamMember> teamMembers = JsonConvert.DeserializeObject<List<SerializableTeamMember>>(serializedTeams);

                    //(section, teams) : where teams is string and a list of members
                    Dictionary<int, Dictionary<string, List<SerializableTeamMember>>> membersByTeamBySection = new Dictionary<int, Dictionary<string, List<SerializableTeamMember>>>();

                    var teamMembersBySections = from c in teamMembers group c by c.Section;

                    foreach (var section in teamMembersBySections)
                    {
                        Dictionary<string, List<SerializableTeamMember>> membersByTeams = new Dictionary<string, List<SerializableTeamMember>>();
                        var teams = from c in section group c by c.InTeamName;

                        foreach (var team in teams)
                        {
                            membersByTeams.Add(team.Key, team.ToList());
                        }
                        membersByTeamBySection.Add(section.Key, membersByTeams);
                    }

                    //for every section add every team
                    foreach (var membersBySection in membersByTeamBySection)
                    {
                        //for every team create a new team and set the Team Name
                        foreach (var team in membersBySection.Value)
                        {
                            Team team_db = new Team();
                            team_db.Name = team.Key;

                            //for every member of that team make a new TeamMember
                            foreach (SerializableTeamMember serializeableMember in team.Value)
                            {
                                TeamMember teamMember_db = new TeamMember();
                                if (serializeableMember.isUser)
                                {
                                    teamMember_db.TeamUser = TeamsOrUsers.User;
                                    teamMember_db.UserProfileID = serializeableMember.UserID;
                                    teamMember_db.TeamID = null;
                                }
                                else
                                {
                                    teamMember_db.TeamUser = TeamsOrUsers.Team;
                                    teamMember_db.TeamID = serializeableMember.TeamID;
                                    teamMember_db.UserProfileID = null;
                                }
                                team_db.Members.Add(teamMember_db);
                            }

                            submission.Teams.Add(team_db);

                            db.Teams.Add(team_db);
                        }
                    }
                    db.SaveChanges();
                }
                else
                {
                    submission.Teams = null;
                    db.SaveChanges();
                }

                return RedirectToAction("Index", "Assignment");
            }

            basic.TeamCreation = createTeamCreationSilverlightObject();

            ViewBag.Categories = new SelectList(db.Categories, "ID", "Name", basic.Assignment.CategoryID);
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");

            return View(basic);

            return RedirectToAction("Index", "Assignment");
        }

        private SilverlightObject createTeamCreationSilverlightObject()
        {
            return new SilverlightObject
            {
                CSSId = "team_creation_silverlight",
                XapName = "TeamCreation",
                Width = "800",
                Height = "580",
                OnLoaded = "SLObjectLoaded",
                Parameters = new Dictionary<string, string>()
                {
                }
            };
        }

        //
        // GET: /Assignment/Edit/5

        [CanModifyCourse]
        public ActionResult Edit(int id)
        {
            BasicAssignmentViewModel basicViewModel = new BasicAssignmentViewModel();
            BasicAssignment assignment = db.BasicAssignments.Find(id);

            if (assignment.Category.CourseID != ActiveCourse.CourseID)
            {
                return RedirectToAction("Index", "Home");
            }

            basicViewModel.Assignment = assignment;
            basicViewModel.Submission = assignment.AssignmentActivities.Where(aa => aa is SubmissionActivity).First() as SubmissionActivity;
            basicViewModel.Stop = assignment.AssignmentActivities.Where(aa => aa is StopActivity).FirstOrDefault() as StopActivity;

            ViewBag.Categories = new SelectList(db.Categories, "ID", "Name", assignment.CategoryID);
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");

            return View(basicViewModel);
        }

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Edit(BasicAssignmentViewModel basic)
        {
            BasicAssignment assignment = db.BasicAssignments.Find(basic.Assignment.ID);

            // Make sure assignment to update belongs to this course.
            if (assignment.Category.CourseID != ActiveCourse.CourseID)
            {
                return RedirectToAction("Index", "Home");
            }

            // If category updated, ensure it belongs to the course as well.
            if (basic.Assignment.CategoryID != assignment.CategoryID)
            {
                Category c = db.Categories.Find(basic.Assignment.CategoryID);
                if (c.CourseID != ActiveCourse.CourseID)
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            if (basic.Submission.ReleaseDate >= basic.Stop.ReleaseDate)
            {
                ModelState.AddModelError("time", "The due date must come after the release date");
            }
            if (ModelState.IsValid)
            {
                // Find current submission and stop activities from basic assignment
                SubmissionActivity submission = assignment.AssignmentActivities.Where(aa => aa is SubmissionActivity).First() as SubmissionActivity;
                StopActivity stop = assignment.AssignmentActivities.Where(aa => aa is StopActivity).FirstOrDefault() as StopActivity;

                assignment.Deliverables.Clear();

                // Update Basic Assignment fields
                assignment.CategoryID = basic.Assignment.CategoryID;
                assignment.Name = basic.Assignment.Name;
                assignment.Description = basic.Assignment.Description;
                assignment.PointsPossible = basic.Assignment.PointsPossible;
                assignment.Deliverables = basic.Assignment.Deliverables;

                // Update Submission Activity fields

                submission.ReleaseDate = basic.Submission.ReleaseDate;
                submission.Name = basic.Submission.Name;
                submission.PointsPossible = basic.Submission.PointsPossible;

                submission.HoursLatePerPercentPenalty = basic.Submission.HoursLatePerPercentPenalty;
                submission.HoursLateUntilZero = basic.Submission.HoursLateUntilZero;
                submission.PercentPenalty = basic.Submission.PercentPenalty;
                submission.MinutesLateWithNoPenalty = basic.Submission.MinutesLateWithNoPenalty;

                // Update Stop Activity fields

                stop.ReleaseDate = basic.Stop.ReleaseDate;

                // Flag models as modified and save to DB

                db.Entry(assignment).State = EntityState.Modified;
                db.Entry(submission).State = EntityState.Modified;
                db.Entry(stop).State = EntityState.Modified;

                db.SaveChanges();
            }

            ViewBag.Categories = new SelectList(db.Categories, "ID", "Name", basic.Assignment.CategoryID);
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");

            return View(basic);
        }

        //
        // GET: /Assignment/Delete/5

        /*
        public ActionResult Delete(int id)
        {
            SubmissionActivity assignment = db.AbstractGradables.Find(id) as SubmissionActivity;
            return View(assignment);
        }
        */

        //
        // POST: /Assignment/Delete/5

        /*
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            SubmissionActivity assignment = db.AbstractGradables.Find(id) as SubmissionActivity;
            db.AbstractGradables.Remove(assignment);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
         */

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }


    }
}
