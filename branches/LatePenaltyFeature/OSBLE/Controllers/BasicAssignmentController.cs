using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.AbstractCourses;
using OSBLE.Models.AbstractCourses.Course;

using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.HomePage;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Utility;
using System.Data.Entity.Validation;
using System.Diagnostics;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities.Scores;

//AC: Please try to keep method names in alphabetical order
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

        private CommentCategoryConfiguration BuildCommentCategories()
        {
            CommentCategoryConfiguration config = new CommentCategoryConfiguration();

            //all keys that we care about start with "category_"
            List<string> keys = (from key in Request.Form.AllKeys
                                 where key.Contains("category_")
                                 select key).ToList();

            //we know this one for sure so no need to loop
            config.Name = Request.Form["category_config_name"].ToString();

            //but the rest are variable, so we need to loop
            foreach (string key in keys)
            {
                //All category keys go something like "category_BLAH1_BLAH2_...".  Based on how
                //many underscores the current key has, we can determine what data it is
                //providing to us
                string[] pieces = key.Split('_');

                //length of 2 is a category name
                if (pieces.Length == 2)
                {
                    int catId = 0;
                    Int32.TryParse(pieces[1], out catId);

                    //does the comment category already exist?
                    CommentCategory category = GetOrCreateCategory(config, catId);
                    category.Name = Request.Form[key].ToString();
                }
                //length of 4 is a category option
                else if (pieces.Length == 4)
                {
                    int catId = 0;
                    int order = 0;
                    Int32.TryParse(pieces[2], out catId);
                    Int32.TryParse(pieces[3], out order);
                    CommentCategory category = GetOrCreateCategory(config, catId);
                    CommentCategoryOption option = new CommentCategoryOption();
                    option.Name = Request.Form[key].ToString();
                    category.Options.Insert(order, option);
                }
            }

            //when we're all done, zero out the category IDs to ensure that the items get
            //added to the DB correctly
            foreach (CommentCategory c in config.Categories)
            {
                c.ID = 0;
            }

            return config;
        }

        [CanModifyCourse]
        public ActionResult Create()
        {
            BasicAssignmentViewModel viewModel = SetUpViewModel();
            SetUpViewBag();
            return View(viewModel);
        }


        [HttpPost]
        [CanModifyCourse]
        public ActionResult Create(BasicAssignmentViewModel basic)
        {

            //The validation method call should trigger an invalid model state if something
            //isn't right.
            ValidateSubmission(basic);

            //inject any lingering form data into our VM
            PopulateModelWithFormData(basic);

            if (ModelState.IsValid)
            {
                int currentCategoryId = basic.Assignment.CategoryID;

                //Get the next column order
                int colOrder = (from assignment in db.AbstractAssignmentActivities
                                where assignment.AbstractAssignment.CategoryID == currentCategoryId
                                orderby assignment.ColumnOrder descending
                                select assignment.ColumnOrder).FirstOrDefault();

                StopActivity stop = new StopActivity();

                basic.Submission.ColumnOrder = colOrder++;

                stop.Name = basic.Stop.Name;
                stop.ReleaseDate = basic.Stop.ReleaseDate;

                basic.Assignment.AssignmentActivities.Add(basic.Submission);
                basic.Assignment.AssignmentActivities.Add(stop);
                db.StudioAssignments.Add(basic.Assignment);

                db.SaveChanges();

                //getting current assignment
                AbstractAssignmentActivity currentActivity = (from a in db.AbstractAssignmentActivities
                                                               where a.AbstractAssignment.ID == basic.Assignment.ID
                                                               select a).First();

                //Getting all the team users for the current assignment
                ICollection<TeamUserMember> currentTeamUsers = (from a in db.AbstractAssignmentActivities
                                                     where a.ID == currentActivity.ID
                                                     select a.TeamUsers).First();

                //creating a list called newscores and populating it with only 1 unique score per team or individual
                List<Score> newScores = new List<Score>();
                foreach(TeamUserMember tu in currentTeamUsers)
                {
                    bool alreadyInList = false;
                    foreach(Score s in newScores) //looking through scores to see if there is already a teamUserMember with a score in the list
                    {
                        if (tu.ID == s.TeamUserMemberID)
                        {
                            alreadyInList = true;
                            break; //Break out once weve found a match
                        }
                    }
                    if (alreadyInList == false) //Only adding scores that are not added
                    {
                        Score newScore = new Score()
                        {
                            TeamUserMember = tu,
                            Points = -1,
                            AssignmentActivityID = currentActivity.ID,
                            PublishedDate = DateTime.Now,
                            isDropped = false,
                            StudentPoints = -1
                        };
                        newScores.Add(newScore);
                    }
                }

                //Now all the scores are compiled into a list. Add them into the db and save
                foreach (Score s in newScores)
                {
                    db.Scores.Add(s);
                }
                db.SaveChanges();

                //send out Events
                Event e = new Event();
                e.Approved = true;
                e.CourseID = activeCourse.AbstractCourse.ID;
                e.HideDelete = true;
                e.Title = basic.Submission.Name + " Due";
                e.StartDate = stop.ReleaseDate;
                e.StartTime = stop.ReleaseDate;
                e.PosterID = currentUser.ID;
                e.Description = "https://osble.org/Assignment?id=" + basic.Assignment.ID;
                db.Events.Add(e);
                db.SaveChanges();
                return RedirectToAction("Index", "Assignment");
            }

            basic.TeamCreation = CreateTeamCreationSilverlightObject();
            basic.RubricCreation = CreateRubricCreationSilverlightObject();
            SetUpViewBag();
            basic.SerializedTeamMembersJSON = basic.TeamCreation.Parameters["teamMembers"] = SerializeTeamMemers(GetTeamMembers());

            return View(basic);
        }

        private SilverlightObject CreateRubricCreationSilverlightObject()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("courseId", activeCourse.AbstractCourseID.ToString());

            return new SilverlightObject
            {
                CSSId = "rubric_silverlight",
                XapName = "OsbleRubric",
                Width = "800",
                Height = "600",
                OnLoaded = "SLObjectLoaded",
                Parameters = parameters
            };
        }

        private SilverlightObject CreateTeamCreationSilverlightObject()
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

        [CanModifyCourse]
        public ActionResult Edit(int id)
        {
            BasicAssignmentViewModel viewModel = SetUpViewModel(id);
            SetUpViewBag(viewModel);
            return View("Create", viewModel);
        }

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Edit(BasicAssignmentViewModel basic)
        {

            //The validation method call should trigger an invalid model state if something
            //isn't right.
            ValidateSubmission(basic);

            //inject any lingering form data into our VM
            PopulateModelWithFormData(basic);

            if (ModelState.IsValid)
            {
                //SubmissionActivity submission = new SubmissionActivity();
                StopActivity stop = (from activity in db.AbstractAssignmentActivities
                                     where activity is StopActivity
                                     &&
                                     activity.AbstractAssignmentID == basic.Assignment.ID
                                     select activity).FirstOrDefault() as StopActivity;
                if (stop == null)
                {
                    stop = new StopActivity();
                    basic.Assignment.AssignmentActivities.Add(stop);
                }
                stop.Name = basic.Stop.Name;
                stop.ReleaseDate = basic.Stop.ReleaseDate;

                //I'm getting a DB reference error when trying to save submission activity
                //changes.  As a quick hack, I decided to just pull the most recent copy
                //from the DB, make the changes, then submit that copy back.
                BasicAssignmentViewModel fakeVm = SetUpViewModel(basic.Assignment.ID);

                fakeVm.Submission.addedPoints = basic.Submission.addedPoints;
                fakeVm.Submission.ColumnOrder = basic.Submission.ColumnOrder;
                fakeVm.Submission.HoursLatePerPercentPenalty = basic.Submission.HoursLatePerPercentPenalty;
                fakeVm.Submission.HoursLateUntilZero = basic.Submission.HoursLateUntilZero;
                fakeVm.Submission.InstructorCanReview = basic.Submission.InstructorCanReview;
                fakeVm.Submission.isTeam = basic.Submission.isTeam;
                fakeVm.Submission.MinutesLateWithNoPenalty = basic.Submission.MinutesLateWithNoPenalty;
                fakeVm.Submission.Name = basic.Submission.Name;
                fakeVm.Submission.PercentPenalty = basic.Submission.PercentPenalty;
                fakeVm.Submission.PointsPossible = basic.Submission.PointsPossible;
                fakeVm.Submission.ReleaseDate = basic.Submission.ReleaseDate;
                fakeVm.Submission.TeamUsers.Clear();
                fakeVm.Submission.TeamUsers = basic.Submission.TeamUsers;
                
                //Do the same thing for basic.Assignment as I did for submissions
                fakeVm.Assignment.CategoryID = basic.Assignment.CategoryID;
                fakeVm.Assignment.Deliverables.Clear();
                fakeVm.Assignment.Deliverables = basic.Assignment.Deliverables;
                fakeVm.Assignment.Description = basic.Assignment.Description;
                fakeVm.Assignment.IsDraft = basic.Assignment.IsDraft;
                fakeVm.Assignment.Name = basic.Assignment.Name;
                fakeVm.Assignment.PointsPossible = basic.Assignment.PointsPossible;
                fakeVm.Assignment.RubricID = basic.Assignment.RubricID;
                fakeVm.Assignment.CommentCategoryConfiguration = basic.Assignment.CommentCategoryConfiguration;
                if (fakeVm.Assignment.CommentCategoryConfiguration != null && fakeVm.Assignment.CommentCategoryConfiguration.ID == 0)
                {
                    fakeVm.Assignment.CommentCategoryConfiguration = null;
                }
                try
                {
                    db.Entry(fakeVm.Submission).State = System.Data.EntityState.Modified;
                    db.Entry(fakeVm.Assignment).State = System.Data.EntityState.Modified;
                    db.Entry(stop).State = System.Data.EntityState.Modified;
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
                return RedirectToAction("Index", "Assignment");
            }

            basic.TeamCreation = CreateTeamCreationSilverlightObject();
            basic.RubricCreation = CreateRubricCreationSilverlightObject();
            SetUpViewBag();
            basic.SerializedTeamMembersJSON = basic.TeamCreation.Parameters["teamMembers"] = SerializeTeamMemers(GetTeamMembers());

            return View("Create", basic);
        }

        private CommentCategory GetOrCreateCategory(CommentCategoryConfiguration config, int categoryId)
        {
            //does the comment category already exist?
            CommentCategory category = (from c in config.Categories
                                        where c.ID == categoryId
                                        select c).FirstOrDefault();
            if (category == null)
            {
                category = new CommentCategory();
                category.ID = categoryId;
                config.Categories.Add(category);
            }
            return category;
        }

        /// <summary>
        /// Builds a new list of rubrics after receiving a close event from the
        /// silverlight rubric creation tool.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [CanModifyCourse]
        public ActionResult GetRubrics()
        {
            List<Rubric> rubrics = (from cr in db.CourseRubrics
                                    join r in db.Rubrics on cr.RubricID equals r.ID
                                    where cr.AbstractCourseID == activeCourse.AbstractCourseID
                                    select r).ToList();
            rubrics.Insert(0, new Rubric() { ID = 0, Description = "" });
            ViewBag.Rubrics = rubrics.ToList();

            if (Request.Form.AllKeys.Contains("selectedRubric"))
            {
                ViewBag.SelectedRubric = Convert.ToInt32(Request.Form["selectedRubric"]);
            }
            else
            {
                ViewBag.SelectedRubric = 0;
            }

            return View("_RubricSelect");
        }

        /// <summary>
        /// Returns a list of students as team members.  The students returned are associated
        /// with the active course
        /// </summary>
        /// <returns></returns>
        private List<SerializableTeamMember> GetTeamMembers()
        {
            List<SerializableTeamMember> teamMembmers = new List<SerializableTeamMember>();

            var couresesUsers = (from c in db.CoursesUsers
                                 where c.AbstractCourseID == activeCourse.AbstractCourseID
                                 && (c.AbstractRole.ID == (int)CourseRole.CourseRoles.Student)
                                 select c).ToList();

            if (couresesUsers.Count > 0)
            {
                foreach (CoursesUsers cu in couresesUsers)
                {
                    SerializableTeamMember teamMember = new SerializableTeamMember();
                    teamMember.IsModerator = cu.AbstractRole.ID == (int)CourseRole.CourseRoles.Moderator;
                    teamMember.Name = cu.UserProfile.FirstName + " " + cu.UserProfile.LastName;
                    teamMember.Section = cu.Section;
                    teamMember.UserID = cu.UserProfileID;
                    teamMember.isUser = true;

                    //Need to find if they submitted the previous activity
                    teamMembmers.Add(teamMember);
                }
            }
            return teamMembmers;
        }

        /// <summary>
        /// Returns a list of team members for the given activity
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        private List<SerializableTeamMember> GetTeamMembers(AbstractAssignmentActivity activity)
        {
            List<SerializableTeamMember> teamMembmers = new List<SerializableTeamMember>();

            //Because an assignment might have been created before some students were added
            //to the course, we cannot look at just the current activity's team users.
            //We also need to pull orphaned (not around at assignment creation) students
            //into the team creation tool.
            //
            //Start with the assumption that all students in the course are orphans.  When we
            //find a student in a team, we can remove him or her from the list of orphans.
            List<CoursesUsers> orphans = db.CoursesUsers
                                            .Where(cu => cu.AbstractCourseID == activeCourse.AbstractCourseID)
                                            .Where(cu => cu.AbstractRole.CanSubmit == true)
                                            .ToList();

            foreach (TeamUserMember member in activity.TeamUsers)
            {
                SerializableTeamMember serializedMember = new SerializableTeamMember();
                serializedMember.Name = member.Name;

                if (member is UserMember)
                {
                    UserMember uMember = member as UserMember;

                    CoursesUsers cu = (from c in db.CoursesUsers
                                       where
                                          c.AbstractCourseID == activeCourse.AbstractCourseID
                                          &&
                                          c.UserProfileID == uMember.UserProfileID
                                       select c).FirstOrDefault();

                    if (cu == null)
                    {
                        continue;
                    }

                    //remove the user member from the list of orphans
                    orphans.Remove(cu);

                    serializedMember.UserID = uMember.UserProfileID;
                    serializedMember.isUser = true;
                    serializedMember.IsModerator = cu.AbstractRole.ID == (int)CourseRole.CourseRoles.Moderator;
                    serializedMember.Section = cu.Section;
                    teamMembmers.Add(serializedMember);
                }
                else if (member is TeamMember)
                {
                    TeamMember tMember = member as TeamMember;
                    foreach (TeamUserMember tum in tMember.Team.Members)
                    {
                        if (tum is UserMember)
                        {
                            serializedMember = new SerializableTeamMember();
                            serializedMember.Name = tum.Name;

                            UserMember uMember = tum as UserMember;

                            CoursesUsers cu = (from c in db.CoursesUsers
                                               where
                                                  c.AbstractCourseID == activeCourse.AbstractCourseID
                                                  &&
                                                  c.UserProfileID == uMember.UserProfileID
                                               select c).FirstOrDefault();

                            if (cu == null)
                            {
                                continue;
                            }

                            //remove the user member from the list of orphans
                            orphans.Remove(cu);

                            serializedMember.InTeamName = tMember.Name;
                            serializedMember.UserID = uMember.UserProfileID;
                            serializedMember.isUser = true;
                            serializedMember.IsModerator = cu.AbstractRole.ID == (int)CourseRole.CourseRoles.Moderator;
                            serializedMember.Section = cu.Section;
                            teamMembmers.Add(serializedMember);
                        }
                    }
                }
            }

            //when we're all done with the activity's teams, we must now add the orphans back to the list
            foreach (CoursesUsers user in orphans)
            {
                SerializableTeamMember serializedMember = new SerializableTeamMember();
                serializedMember.Name = user.UserProfile.LastAndFirst();
                serializedMember.UserID = user.UserProfileID;
                serializedMember.isUser = true;
                serializedMember.IsModerator = user.AbstractRole.ID == (int)CourseRole.CourseRoles.Moderator;
                serializedMember.Section = user.Section;
                teamMembmers.Add(serializedMember);
            }

            return teamMembmers;
        }

        /// <summary>
        /// EF handles most of the ViewModel binding, but some things still need to be modified.
        /// This function handles these final modifications.
        /// </summary>
        /// <param name="viewModel"></param>
        private void PopulateModelWithFormData(BasicAssignmentViewModel viewModel)
        {
            if (Request.Params["isGradable"].ToString() == "false")
            {
                viewModel.Assignment.Category = (from c in (activeCourse.AbstractCourse as Course).Categories
                                             where c.Name == Constants.UnGradableCatagory
                                             select c).FirstOrDefault();
            }

            if (viewModel.Submission.InstructorCanReview)
            {
                if (Request.Form["line_review_options"].ToString().CompareTo("ManualConfig") == 0)
                {
                    viewModel.Assignment.CommentCategoryConfiguration = BuildCommentCategories();
                    db.CommentCategoryConfigurations.Add(viewModel.Assignment.CommentCategoryConfiguration);
                    db.SaveChanges();
                    viewModel.Assignment.CommentCategoryConfigurationID = viewModel.Assignment.CommentCategoryConfiguration.ID;
                }
                else if (Request.Form["line_review_options"].ToString().CompareTo("AutoConfig") == 0)
                {
                    viewModel.Assignment.CommentCategoryConfigurationID = Convert.ToInt32(Request.Params["comment_category_selection"]);
                    viewModel.Assignment.CommentCategoryConfiguration = (from ccc in db.CommentCategoryConfigurations
                                                                         where ccc.ID == viewModel.Assignment.CommentCategoryConfigurationID
                                                                         select ccc).FirstOrDefault();
                }
            }

            if (viewModel.Submission.isTeam)
            {
                viewModel.Submission.TeamUsers.Clear();
                List<SerializableTeamMember> teamMembers = JsonConvert.DeserializeObject<List<SerializableTeamMember>>(viewModel.SerializedTeams);

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
                            TeamUserMember teamUser_db;
                            if (serializeableMember.isUser)
                            {
                                UserMember userMember = new UserMember();
                                userMember.UserProfile = (from c in db.UserProfiles
                                                          where c.ID == serializeableMember.UserID
                                                          select c).FirstOrDefault();
                                userMember.UserProfileID = userMember.UserProfile.ID;
                                teamUser_db = userMember;
                            }
                            else
                            {
                                TeamMember teamMember = new TeamMember();
                                teamMember.Team = (from c in db.Teams
                                                   where c.ID == serializeableMember.TeamID
                                                   select c).FirstOrDefault();
                                teamMember.TeamID = teamMember.Team.ID;

                                teamUser_db = teamMember;
                            }
                            team_db.Members.Add(teamUser_db);
                        }

                        TeamMember tm = new TeamMember();
                        tm.Team = team_db;
                        viewModel.Submission.TeamUsers.Add(tm);
                        db.Teams.Add(team_db);
                    }
                }
                //db.SaveChanges();
            }
            else
            {
                viewModel.Submission.TeamUsers.Clear();
                var users = from c in db.CoursesUsers
                            where c.AbstractCourseID == activeCourse.AbstractCourseID
                            && c.AbstractRole.CanSubmit
                            select c.UserProfile;

                foreach (UserProfile user in users)
                {
                    UserMember um = new UserMember();
                    um.UserProfile = user;

                    viewModel.Submission.TeamUsers.Add(um);
                }
                //db.SaveChanges();
            }
        }

        /// <summary>
        /// Is a one line function necessary?
        /// </summary>
        /// <param name="members"></param>
        /// <returns></returns>
        private string SerializeTeamMemers(List<SerializableTeamMember> members)
        {
            return Uri.EscapeDataString(JsonConvert.SerializeObject(members));
        }

        private List<object> SetUpPastTeamAssignments()
        {
            var assignmentActivites = from c in db.AbstractAssignmentActivities
                                      where c.AbstractAssignment.Category.CourseID == activeCourse.AbstractCourseID
                                      select c;
            List<object> pastTeamAssignments = new List<object>();
            foreach (var activity in assignmentActivites.ToList())
            {
                if (activity.isTeam)
                {
                    string json = SerializeTeamMemers(GetTeamMembers(activity));
                    pastTeamAssignments.Add(new { ID = json, Name = activity.Name });
                }
            }
            return pastTeamAssignments;
        }

        /// <summary>
        /// Place ALL viewbag-related content in this function
        /// </summary>
        private void SetUpViewBag()
        {
            // changes the Page title and button to "Create Basic Assignment"
            ViewBag.AssignmentLabel = "Create Basic Assignment";

            //RUBRICS
            List<Rubric> rubrics = (from cr in db.CourseRubrics
                                    join r in db.Rubrics on cr.RubricID equals r.ID
                                    where cr.AbstractCourseID == activeCourse.AbstractCourseID
                                    select r).ToList();
            if (rubrics.Count() < 1)
            {
                rubrics.Insert(0, new Rubric() { ID = 0, Description = "This course has no rubrics" });
            }
            else
            {
                rubrics.Insert(0, new Rubric() { ID = 0, Description = "" });
            }
            ViewBag.Rubrics = rubrics.ToList();

            //SUBMISSION CATEGORIES
            var cat = from c in (activeCourse.AbstractCourse as Course).Categories
                      where c.Name != Constants.UnGradableCatagory
                      select c;
            ViewBag.Categories = new SelectList(cat, "ID", "Name");

            //DELIVERABLES
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");
            ViewBag.AllowedFileNames = from c in FileSystem.GetCourseDocumentsFileList(activeCourse.AbstractCourse, includeParentLink: false).Files select c.Name;

            //TEAM ASSIGNMENTS
            ViewBag.NewTeams = "";
            if (Request.Form.AllKeys.Contains("newTeams"))
            {
                ViewBag.NewTeams = Request.Form["newTeams"];
            }
            ViewBag.PastTeamAssignments = new SelectList(SetUpPastTeamAssignments(), "ID", "Name", null);

            //LINE-BY-LINE
            List<CommentCategoryConfiguration> configs = (from course in db.Courses
                                                          join category in db.Categories on course.ID equals category.CourseID
                                                          join assignment in db.AbstractAssignments on category.ID equals assignment.CategoryID
                                                          where
                                                            assignment.CommentCategoryConfigurationID != null
                                                            &&
                                                            course.ID == activeCourse.AbstractCourseID
                                                          select assignment.CommentCategoryConfiguration).ToList();
            ViewBag.CommentConfigurations = configs;

            //a new assignment can't have existing submissions
            ViewBag.HasSubmissions = false;
        }

        /// <summary>
        /// Edit-specific viewbag information
        /// </summary>
        private void SetUpViewBag(BasicAssignmentViewModel viewModel)
        {
            //Make life easier by calling the default setup function.  I'm sure that most of the
            //important stuff will get overwritten by the code below, but it's always good to
            //cover your bases.
            SetUpViewBag();

            // changes the Page title and button to "Modify Assignment"
            ViewBag.AssignmentLabel = "Modify Assignment";

            //Unlike when doing a CREATE, EDIT teams come from the viewmodel and not a postback
            string json = SerializeTeamMemers(GetTeamMembers(viewModel.Assignment.AssignmentActivities.Where(a => a.TeamUsers.Count > 0).FirstOrDefault()));
            ViewBag.NewTeams = json;

            //similarly, the rubric's id doesn't come from a postback
            ViewBag.SelectedRubric = viewModel.Assignment.RubricID;

            //does this assignment have any submissions?
            bool submissionFound = false;
            foreach (TeamUserMember teamUser in viewModel.Submission.TeamUsers)
            {
                DateTime? submissionTime = GetSubmissionTime(activeCourse.AbstractCourse as Course, viewModel.Submission, teamUser);
                if (submissionTime != null)
                {
                    submissionFound = true;
                    break;
                }
            }

            //if we found something, then we need to inform the view
            if (submissionFound)
            {
                ViewBag.HasSubmissions = true;
            }

        }

        private BasicAssignmentViewModel SetUpViewModel()
        {
            //we create a basic assignment that is a StudioAssignment with a submission and a stop
            BasicAssignmentViewModel viewModel = new BasicAssignmentViewModel();

            // Copy default Late Policy settings
            Course active = activeCourse.AbstractCourse as Course;
            viewModel.Submission.HoursLatePerPercentPenalty = active.HoursLatePerPercentPenalty;
            viewModel.Submission.HoursLateUntilZero = active.HoursLateUntilZero;
            viewModel.Submission.PercentPenalty = active.PercentPenalty;
            viewModel.Submission.MinutesLateWithNoPenalty = active.MinutesLateWithNoPenalty;

            viewModel.TeamCreation = CreateTeamCreationSilverlightObject();
            viewModel.RubricCreation = CreateRubricCreationSilverlightObject();

            viewModel.TeamCreation.Parameters["teamMembers"] = SerializeTeamMemers(GetTeamMembers());
            viewModel.SerializedTeamMembersJSON = viewModel.TeamCreation.Parameters["teamMembers"];

            //comment categories are null by default.  This doesn't work for us
            viewModel.Assignment.CommentCategoryConfiguration = new CommentCategoryConfiguration();

            return viewModel;
        }

        private BasicAssignmentViewModel SetUpViewModel(int assignmentId)
        {
            BasicAssignmentViewModel viewModel = new BasicAssignmentViewModel();

            //base assignment data
            viewModel.Assignment = (from a in db.StudioAssignments
                                    where a.ID == assignmentId
                                    select a).FirstOrDefault();

            //get the submission activity
            viewModel.Submission = (from sa in viewModel.Assignment.AssignmentActivities
                                    where sa is SubmissionActivity
                                    select sa).FirstOrDefault() as SubmissionActivity;
            viewModel.Stop = (from sa in viewModel.Assignment.AssignmentActivities
                              where sa is StopActivity
                              select sa).FirstOrDefault() as StopActivity;

            viewModel.TeamCreation = CreateTeamCreationSilverlightObject();
            viewModel.RubricCreation = CreateRubricCreationSilverlightObject();
            
            //Check for null comment categories.  
            if (viewModel.Assignment.CommentCategoryConfiguration == null)
            {
                viewModel.Assignment.CommentCategoryConfiguration = new CommentCategoryConfiguration();
            }

            //was a rubric specified?
            if (viewModel.Assignment.RubricID != 0 && viewModel.Assignment.Rubric != null)
            {
                viewModel.UseRubric = true;
            }

            return viewModel;
        }

        /// <summary>
        /// Performs model validation
        /// </summary>
        /// <param name="viewModel"></param>
        private void ValidateSubmission(BasicAssignmentViewModel viewModel)
        {
            //Team validation.  Note the model change.
            viewModel.SerializedTeams = null;
            try
            {
                viewModel.SerializedTeams = Uri.UnescapeDataString(Request.Params["newTeams"]);
            }
            catch
            {
                viewModel.SerializedTeams = null;
            }
            if (viewModel.Submission.isTeam)
            {
                if (viewModel.SerializedTeams == null || viewModel.SerializedTeams == "")
                {
                    ModelState.AddModelError("Team", "Using teams was selected but no teams were created, please create the teams");
                }
            }

            //release date validation
            if (viewModel.Submission.ReleaseDate >= viewModel.Stop.ReleaseDate)
            {
                ModelState.AddModelError("time", "The due date must come after the release date");
            }

            //rubric.  Note that we're also modifying the ViewModel
            if (viewModel.UseRubric)
            {
                int rubricId = 0;
                if (Int32.TryParse(Request.Form["RubricToUse"].ToString(), out rubricId) && rubricId != 0)
                {
                    viewModel.Assignment.RubricID = rubricId;
                    ViewBag.SelectedRubric = rubricId;
                }
                else
                {
                    ModelState.AddModelError("rubric", "The use of a rubric was indicated, but no rubric has been selected.");
                }
            }

        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}