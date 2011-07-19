using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Newtonsoft.Json;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.AbstractCourses;
//using OSBLE.Models.AbstractCourses.Course;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Utility;
using OSBLE.Models.AbstractCourses.Course;

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

        [HttpPost]
        [CanModifyCourse]
        public ActionResult PastTeamsChange()
        {
            return View("_TeamSilverlightObject");
        }

        //
        // GET: /Assignment/Create

        [CanModifyCourse]
        public ActionResult Create()
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

            var assignmentActivites = from c in db.AbstractAssignmentActivities
                                      where c.AbstractAssignment.Category.CourseID == activeCourse.AbstractCourseID
                                      select c;

            //set up past team assignments
            List<object> pastTeamAssignments = new List<object>();
            foreach (var activity in assignmentActivites.ToList())
            {
                if (activity.isTeam)
                {
                    string json = serializeTeamMemers(getTeamMembers(activity));
                    pastTeamAssignments.Add(new { ID = json, Name = activity.Name });
                }
            }
            ViewBag.PastTeamAssignments = new SelectList(pastTeamAssignments, "ID", "Name", null);

            viewModel.SerializedTeamMembersJSON = viewModel.TeamCreation.Parameters["teamMembers"] = serializeTeamMemers(getTeamMembers());

            setupViewBagForCreate();

            // line by line review configurations
            List<CommentCategoryConfiguration> configs = (from cc in db.CommentCategoryConfigurations
                                                          where cc.ID != null
                                                          select cc).ToList();
            ViewBag.CommentConfigurations = configs;

            return View(viewModel);
        }

        //
        // POST: /Assignment/Create

        private void setupViewBagForCreate()
        {
            List<Rubric> rubrics = (from cr in db.CourseRubrics
                                    join r in db.Rubrics on cr.RubricID equals r.ID
                                    where cr.AbstractCourseID == activeCourse.AbstractCourseID
                                    select r).ToList();
            rubrics.Insert(0, new Rubric() { ID = 0, Description = "" });
            ViewBag.Rubrics = rubrics.ToList();

            var cat = from c in (activeCourse.AbstractCourse as Course).Categories
                      where c.Name != Constants.UnGradableCatagory
                      select c;
            ViewBag.Categories = new SelectList(cat, "ID", "Name");
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");
            ViewBag.AllowedFileNames = from c in FileSystem.GetCourseDocumentsFileList(activeCourse.AbstractCourse, includeParentLink: false).Files select c.Name;
        }

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
                        CommentCategoryConfiguration config = BuildCommentCategories();
                        if (config.Categories.Count > 0)
                        {
                            db.CommentCategoryConfigurations.Add(config);
                            db.SaveChanges();
                            basic.Assignment.CommentCategoryConfigurationID = config.ID;
                        }
                    }
                    else if (Request.Form["line_review_options"].ToString().CompareTo("AutoConfig") == 0)
                    {
                        basic.Assignment.CommentCategoryConfigurationID = Convert.ToInt32(Request.Params["comment_category_selection"]); 
                    }

                }

                if (basic.UseRubric)
                {
                    int rubricId = 0;
                    if (Int32.TryParse(Request.Form["RubricToUse"].ToString(), out rubricId))
                    {
                        basic.Assignment.RubricID = rubricId;
                    }
                }

                db.StudioAssignments.Add(basic.Assignment);

                db.SaveChanges();

                // Causes a duplicate entry into the database
                //db.AbstractAssignmentActivity.Add(submission);
                //db.AbstractAssignmentActivity.Add(stop);
                //db.SaveChanges();

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

                return RedirectToAction("Index", "Assignment");
            }

            basic.TeamCreation = createTeamCreationSilverlightObject();
            basic.RubricCreation = createRubricCreationSilverlightObject();
            setupViewBagForCreate();
            //ViewBag.Categories = new SelectList(db.Categories, "ID", "Name", basic.Assignment.CategoryID);
            //ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");

            basic.SerializedTeamMembersJSON = basic.TeamCreation.Parameters["teamMembers"] = serializeTeamMemers(getTeamMembers());

            return View(basic);
        }

        private string serializeTeamMemers(List<SerializableTeamMember> members)
        {
            return Uri.EscapeDataString(JsonConvert.SerializeObject(members));
        }

        private List<SerializableTeamMember> getTeamMembers(AbstractAssignmentActivity activity)
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

        private List<SerializableTeamMember> getTeamMembers()
        {
            List<SerializableTeamMember> teamMembmers = new List<SerializableTeamMember>();

            var couresesUsers = (from c in db.CoursesUsers
                                 where c.AbstractCourseID == activeCourse.AbstractCourseID
                                 && (c.AbstractRole.ID == (int)CourseRole.CourseRoles.Student)
                                 select c).ToList();

            int i = 0;

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

        //
        // GET: /Assignment/Edit/5

        [CanModifyCourse]
        public ActionResult Edit(int id)
        {
            List<Deliverable> Deliverables = new List<Deliverable>();
            BasicAssignmentViewModel viewModel = new BasicAssignmentViewModel();
            StudioAssignment assignment = db.AbstractAssignments.Find(id) as StudioAssignment;

            if (assignment != null && assignment.Category.Course == activeCourse.AbstractCourse)
            {
                SubmissionActivity submission = (from c in assignment.AssignmentActivities where c is SubmissionActivity select c as SubmissionActivity).FirstOrDefault();
                StopActivity stop = (from c in assignment.AssignmentActivities where c is StopActivity select c as StopActivity).FirstOrDefault();

                if (submission != null && stop != null)
                {
                    // Copy default Late Policy settings
                    Course active = activeCourse.AbstractCourse as Course;

                    viewModel.Assignment = assignment;

                    viewModel.Submission.HoursLatePerPercentPenalty = submission.HoursLatePerPercentPenalty;
                    viewModel.Submission.HoursLateUntilZero = submission.HoursLateUntilZero;
                    viewModel.Submission.PercentPenalty = submission.PercentPenalty;
                    viewModel.Submission.MinutesLateWithNoPenalty = submission.MinutesLateWithNoPenalty;

                    viewModel.TeamCreation = createTeamCreationSilverlightObject();
                    viewModel.RubricCreation = createRubricCreationSilverlightObject();

                    //TO DO: need to load existing teams not make new ones
                    viewModel.SerializedTeamMembersJSON = viewModel.TeamCreation.Parameters["teamMembers"] = serializeTeamMemers(getTeamMembers());

                    //TO DO: need to load existing categories not make new ones
                    var cat = from c in db.Categories
                              where c.CourseID == active.ID
                              select c;
                    ViewBag.Categories = new SelectList(cat, "ID", "Name");
                    ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");
                    ViewBag.Deliverables = Deliverables;
                    return View(viewModel);
                }
            }

            return RedirectToAction("Index", "Assignment");
        }

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Edit(BasicAssignmentViewModel basic)
        {
            StudioAssignment assignment = db.StudioAssignments.Find(basic.Assignment.ID);

            // Make sure assignment to update belongs to this course.
            if (assignment.Category.CourseID != ActiveCourse.AbstractCourseID)
            {
                return RedirectToAction("Index", "Home");
            }

            // If category updated, ensure it belongs to the course as well.
            if (basic.Assignment.CategoryID != assignment.CategoryID)
            {
                Category c = db.Categories.Find(basic.Assignment.CategoryID);
                if (c.CourseID != ActiveCourse.AbstractCourseID)
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

        // ajax handling code for comments
        //   kept in case ajax wanted to be reimplemented
        /*
        [HttpPost]
        [CanModifyCourse]
        public string SaveCommentCollection(string name, string data)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            string[][] dataArray = ser.Deserialize<string[][]>(HttpUtility.UrlDecode(data));

            string output = "[" + HttpUtility.UrlDecode(name);

            CommentCollection newCC = new CommentCollection(HttpUtility.UrlDecode(name));

            newCC.Name = HttpUtility.UrlDecode(name);

            for (int i = 0; i < dataArray.Length; i++)
            {
                CommentCategory cc = new CommentCategory(dataArray[i][0]);
                output += "," + dataArray[i][0];

                for (int j = 1; j < dataArray[i].Length; j++)
                {
                    output += "," + dataArray[i][j];
                    cc.CommentCategoryTags.Add( new CommentCategoryTag(dataArray[i][j]) );
                }

                newCC.CommentCategories.Add( cc );
            }

            // validate

            //db.CommentCollections.Add(newCC);

            //db.SaveChanges();

            return output + "]";
        }

        [HttpPost]
        [CanModifyCourse]
        public string GetCollectionContents(int inputID)
        {
            List<CommentCollection> q1 = (from c in db.CommentCollections
                                    where c.ID == inputID
                                    select c).ToList();

            string output = "[";
            foreach (CommentCollection cc in q1)
            {
                output += "." + cc.Name + ".";
                foreach (CommentCategory c in cc.CommentCategories)
                {
                    output += "[" + c.Name;

                    foreach (CommentCategoryTag t in c.CommentCategoryTags)
                    {
                        output += "," + t.value;
                    }

                    output += "],";
                }
            }
            // removes trailing comma and adds ending bracket
            //output = output.Substring(0, output.Length - 1) + "]";

            string output = "[" + inputID.ToString() + "]";
            return HttpUtility.UrlEncode(output);
        }

        [HttpPost]
        [CanModifyCourse]
        public string GetCollections()
        {
            // add logic for current course here
            List<CommentCollection> q = (from c in db.CommentCollections where c.Name != null select c).ToList();

            string output = "[";
            foreach (CommentCollection c in q)
            {
                output += "[\"" + c.ID + "\",\"" + c.Name + "\"],";
            }
            output = output.Substring(0, output.Length - 1) + "]";

            string output = "[[\"1\",\"Abc\"],[\"2\",\"Def\"]]";
            return output;
        }
        */

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
