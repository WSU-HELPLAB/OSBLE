using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.AbstractCourses;
using OSBLE.Models.AbstractCourses.Course;

//using OSBLE.Models.AbstractCourses.Course;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.HomePage;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Utility;

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

        [CanModifyCourse]
        public ActionResult Create()
        {
            BasicAssignmentViewModel viewModel = SetUpViewModel();
            SetUpViewBag();
            return View(viewModel);
        }

        [CanModifyCourse]
        public ActionResult Edit(int id)
        {
            BasicAssignmentViewModel viewModel = SetUpViewModel(id);
            SetUpViewBag();
            return View("Create", viewModel);
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

            return teamMembmers;
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

            viewModel.TeamCreation = createTeamCreationSilverlightObject();
            viewModel.RubricCreation = createRubricCreationSilverlightObject();

            viewModel.TeamCreation.Parameters["teamMembers"] = SerializeTeamMemers(GetTeamMembers());
            viewModel.SerializedTeamMembersJSON = viewModel.TeamCreation.Parameters["teamMembers"];

            return viewModel;
        }

        private BasicAssignmentViewModel SetUpViewModel(int courseId)
        {
            //TODO: get this working
            BasicAssignmentViewModel viewModel = new BasicAssignmentViewModel();

            //base assignment data
            viewModel.Assignment = (from a in db.StudioAssignments
                                    where a.ID == courseId
                                    select a).FirstOrDefault();

            //get the submission activity
            viewModel.Submission = (from sa in db.SubmissionActivities
                                    where sa.AbstractAssignmentID == courseId
                                    select sa).FirstOrDefault();


            viewModel.TeamCreation = createTeamCreationSilverlightObject();
            viewModel.RubricCreation = createRubricCreationSilverlightObject();

            return viewModel;
        }

        //*********
        ///Everything below this line MAY need to be redone.  Place things that are "okay"
        //above this line.
        //*********


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

            if (Request.Params["isGradable"].ToString() == "false")
            {
                basic.Assignment.Category = (from c in (activeCourse.AbstractCourse as Course).Categories
                                             where c.Name == Constants.UnGradableCatagory
                                             select c).FirstOrDefault();
            }

            if (basic.UseRubric)
            {
                int rubricId = 0;
                if ( Int32.TryParse(Request.Form["RubricToUse"].ToString(), out rubricId) && rubricId != 0 )
                {
                    basic.Assignment.RubricID = rubricId;
                    ViewBag.SelectedRubric = rubricId;
                }
                else
                {
                    ModelState.AddModelError("rubric", "The use of a rubric was indicated, but no rubric has been selected.");
                }
            }

            if (Request.Form["line_review_options"].ToString().CompareTo("ManualConfig") == 0)
            {
                basic.CommentCategoryConfiguration = BuildCommentCategories();
            }
            else if (Request.Form["line_review_options"].ToString().CompareTo("AutoConfig") == 0)
            {
                basic.Assignment.CommentCategoryConfigurationID = Convert.ToInt32(Request.Params["comment_category_selection"]);
                basic.CommentCategoryConfiguration.ID = (int)basic.Assignment.CommentCategoryConfigurationID;
            }

            if (ModelState.IsValid)
            {
                int currentCategoryId = basic.Assignment.CategoryID;

                //Get the next column order
                int colOrder = (from assignment in db.AbstractAssignmentActivities
                                where assignment.AbstractAssignment.CategoryID == currentCategoryId
                                orderby assignment.ColumnOrder descending
                                select assignment.ColumnOrder).FirstOrDefault();

                SubmissionActivity submission = new SubmissionActivity();
                StopActivity stop = new StopActivity();

                submission.PointsPossible = 100; //it actually doesn't matter
                submission.ReleaseDate = basic.Submission.ReleaseDate;
                submission.Name = basic.Submission.Name;

                submission.PointsPossible = basic.Submission.PointsPossible;

                submission.HoursLatePerPercentPenalty = basic.Submission.HoursLatePerPercentPenalty;
                submission.HoursLateUntilZero = basic.Submission.HoursLateUntilZero;
                submission.PercentPenalty = basic.Submission.PercentPenalty;
                submission.MinutesLateWithNoPenalty = basic.Submission.MinutesLateWithNoPenalty;

                submission.isTeam = basic.Submission.isTeam;
                submission.InstructorCanReview = basic.Submission.InstructorCanReview;

                submission.ColumnOrder = colOrder++;

                stop.Name = basic.Stop.Name;
                stop.ReleaseDate = basic.Stop.ReleaseDate;

                basic.Assignment.AssignmentActivities.Add(submission);
                basic.Assignment.AssignmentActivities.Add(stop);

                if (basic.Submission.InstructorCanReview)
                {
                    if (Request.Form["line_review_options"].ToString().CompareTo("ManualConfig") == 0)
                    {
                        db.CommentCategoryConfigurations.Add(basic.CommentCategoryConfiguration);
                        db.SaveChanges();
                        basic.Assignment.CommentCategoryConfigurationID = basic.CommentCategoryConfiguration.ID;
                    }
                    else if (Request.Form["line_review_options"].ToString().CompareTo("AutoConfig") == 0)
                    {
                        basic.Assignment.CommentCategoryConfigurationID = Convert.ToInt32(Request.Params["comment_category_selection"]);
                    }
                }

                db.StudioAssignments.Add(basic.Assignment);

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

                            submission.TeamUsers.Add(tm);

                            db.Teams.Add(team_db);
                        }
                    }
                    db.SaveChanges();
                }
                else
                {
                    var users = from c in db.CoursesUsers
                                where c.AbstractCourseID == activeCourse.AbstractCourseID
                                && c.AbstractRole.CanSubmit
                                select c.UserProfile;

                    foreach (UserProfile user in users)
                    {
                        UserMember um = new UserMember();
                        um.UserProfile = user;

                        submission.TeamUsers.Add(um);
                    }
                    db.SaveChanges();
                }

                //send out Events
                Event e = new Event();
                e.Approved = true;
                e.CourseID = activeCourse.AbstractCourse.ID;
                e.HideDelete = true;
                e.Title = submission.Name + " Due";
                e.StartDate = stop.ReleaseDate;
                e.StartTime = stop.ReleaseDate;
                e.PosterID = currentUser.ID;
                e.Description = "https://osble.org/Assignment?id=" + basic.Assignment.ID;
                db.Events.Add(e);
                db.SaveChanges();
                return RedirectToAction("Index", "Assignment");
            }

            basic.TeamCreation = createTeamCreationSilverlightObject();
            basic.RubricCreation = createRubricCreationSilverlightObject();
            SetUpViewBag();
            //ViewBag.Categories = new SelectList(db.Categories, "ID", "Name", basic.Assignment.CategoryID);
            //ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");

            basic.SerializedTeamMembersJSON = basic.TeamCreation.Parameters["teamMembers"] = SerializeTeamMemers(GetTeamMembers());

            return View(basic);
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

        private SilverlightObject createRubricCreationSilverlightObject()
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
                    CommentCategory category = GetCategoryOrCreateNew(config, catId);
                    category.Name = Request.Form[key].ToString();
                }
                //length of 4 is a category option
                else if (pieces.Length == 4)
                {
                    int catId = 0;
                    int order = 0;
                    Int32.TryParse(pieces[2], out catId);
                    Int32.TryParse(pieces[3], out order);
                    CommentCategory category = GetCategoryOrCreateNew(config, catId);
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

        private CommentCategory GetCategoryOrCreateNew(CommentCategoryConfiguration config, int categoryId)
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

        // POST: /BasicAssignment/Edit/5
        [HttpPost]
        [CanModifyCourse]
        public ActionResult Edit(BasicAssignmentViewModel basic)
        {
            return View("Create");
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}