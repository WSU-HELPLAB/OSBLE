using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Configuration;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Models.HomePage;
using System.Data.Entity.Validation;
using System.Diagnostics;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.DiscussionAssignment;
using System.Text.RegularExpressions;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class AssignmentController : OSBLEController
    {
        public AssignmentController()
        {
            ViewBag.CurrentTab = "Assignments";
        }

        [CanModifyCourse]
        public ActionResult Delete(int id)
        {
            //verify that the user attempting a delete owns this course and that the id is valid
            if (!activeCourse.AbstractRole.CanModify)
            {
                return RedirectToAction("Index");
            }

            Assignment assignment = db.Assignments.Find(id);
            if (assignment == null)
            {
                return RedirectToAction("Index");
            }

            db.Assignments.Remove(assignment);
            db.SaveChanges();
            return Index(id);
        }

        //
        // GET: /Assignment/

        public ActionResult Index(int? id)
        {
            //did the user just submit something?  If so, set up view to notify user
            if (Session["SubmissionReceived"] != null && Convert.ToBoolean(Session["SubmissionReceived"]) == true)
            {
                ViewBag.SubmissionReceived = true;
                Session["SubmissionReceived"] = null;
            }
            else
            {
                ViewBag.SubmissionReceived = false;
                Session["SubmissionReceived"] = null;
            }

            List<Assignment> Assignments = new List<Assignment>();
            //Getting the assginment list, initially without future or draft assignments.
            Assignments = (from assignment in db.Assignments
                                                where !assignment.IsDraft &&
                                                assignment.Category.CourseID == activeCourse.AbstractCourseID &&
                                                assignment.IsWizardAssignment &&
                                                assignment.ReleaseDate <= DateTime.Now
                                                orderby assignment.DueDate
                                                select assignment).ToList();

            // Future assignments are non-draft assignments whose start date has not yet happened. Appending to list now.
            Assignments.AddRange((from assignment in db.Assignments
                                                  where !assignment.IsDraft &&
                                                  assignment.Category.CourseID == activeCourse.AbstractCourseID &&
                                                  assignment.IsWizardAssignment == true &&
                                                  assignment.ReleaseDate > DateTime.Now
                                                  orderby assignment.ReleaseDate
                                                  select assignment).ToList());

            if (ActiveCourse.AbstractRole.CanModify || ActiveCourse.AbstractRole.Anonymized)
            {
                // Draft assignments (viewable by instructor only) are assignments that have not yet been published to students. Appending to list now.
                Assignments.AddRange( (from assignment in db.Assignments
                                    where assignment.IsDraft &&
                                    assignment.IsWizardAssignment &&
                                    assignment.Category.CourseID == activeCourse.AbstractCourseID
                                    orderby assignment.ReleaseDate
                                    select assignment).ToList());
            }
            else if(ActiveCourse.AbstractRole.CanSubmit)
            {
                /*MG: gathering a list of assignments for that course that are non-draft. Then creating a dictionary<assignmentID, submissionTime> 
                 * to be used in the view. This is only done for the students view. 
                 */
                List<Assignment> assignmentList = (from assignment in db.Assignments
                                                   where !assignment.IsDraft &&
                                                   assignment.IsWizardAssignment &&
                                                   assignment.Category.CourseID == activeCourse.AbstractCourseID
                                                   select assignment).ToList();

                //This will hold the assignment ID, the date submitted in string format, the grade in string format, and the team ID
                Dictionary<int, Tuple<string, string, AssignmentTeam>> submissionInfo = new Dictionary<int, Tuple<string, string, AssignmentTeam>>();
                foreach (Assignment a in assignmentList)
                {
                    //populating tuple to add to dictionary by collecting the information described in the above commentblock.
                    AssignmentTeam at = OSBLEController.GetAssignmentTeam(a, currentUser);
                    DateTime? subTime = GetSubmissionTime(a.Category.Course, a, at);
                    string submissionTime = "No Submission";
                    string scoreString = "No Grade";
                    if (subTime != null) //found a submission time, Reassign submissionTime
                    {
                        submissionTime = subTime.Value.ToString();
                    }

                    //Finding score match based off UserPrfileID rather than courseUserID to avoid grabbing another team members grade (as they are potentially different)
                    var score = (from assScore in a.Scores
                                 where assScore.CourseUser.UserProfileID == CurrentUser.ID
                                 select assScore).FirstOrDefault();
                    if (score != null) //found matching score. Reassign scoreString
                    {
                        scoreString = (score as Score).getGradeAsPercent(a.PointsPossible);
                    }
                    submissionInfo.Add(a.ID, new Tuple<string, string, AssignmentTeam>(submissionTime, scoreString, at));

                }

                //Gathering the Team Evaluations for the current users teams.
                List<TeamEvaluation> teamEvaluations = (from t in db.TeamMemberEvaluations
                                                        where t.EvaluatorID == activeCourse.ID
                                                        select t.TeamEvaluation).ToList();

                ViewBag.TeamEvaluations = teamEvaluations;

                ViewBag.SubmissionInfoDictionary = submissionInfo;
            }

            ViewBag.PastCount = (from a in Assignments
                                    where a.DueDate < DateTime.Now &&
                                    !a.IsDraft &&
                                    a.IsWizardAssignment
                                    select a).Count();
            ViewBag.PresentCount = (from a in Assignments
                                    where a.ReleaseDate < DateTime.Now &&
                                    a.DueDate > DateTime.Now &&
                                    !a.IsDraft &&
                                    a.IsWizardAssignment
                                    select a).Count();
            ViewBag.FutureCount = (from a in Assignments
                                    where a.DueDate >= DateTime.Now &&
                                    a.ReleaseDate >= DateTime.Now &&
                                    !a.IsDraft &&
                                    a.IsWizardAssignment
                                    select a).Count();
            ViewBag.DraftCount = (from a in Assignments
                                  where a.IsDraft &&
                                  a.IsWizardAssignment
                                    select a).Count();
            ViewBag.Assignments = Assignments;
            ViewBag.CurrentDate = DateTime.Now;
            ViewBag.Submitted = false;
            return View();
        }

        [CanGradeCourse]
        public ActionResult InlineReview(int assignmentID, int teamID)
        {
            try
            {
                Assignment assignment = db.Assignments.Find(assignmentID);
                AssignmentTeam team = (from a in assignment.AssignmentTeams
                                       where a.TeamID == teamID
                                       select a).FirstOrDefault();
                if (assignment.Category.CourseID == activeCourse.AbstractCourseID && assignment.AssignmentTeams.Contains(team))
                {
                    Session.Add("CurrentAssignmentID", assignmentID);
                    Session.Add("TeamID", teamID);

                    //if publish file exists then teacher can not save as draft
                    bool canSaveAsDraft = !(new FileInfo(FileSystem.GetTeamUserPeerReview(false, activeCourse.AbstractCourse as Course, assignmentID, teamID)).Exists);

                    ViewBag.Assignment = assignment;
                    ViewBag.AssignmentTeam = team;

                    return View(new InlineReviewViewModel() { ReviewInterface = createEditInlineReviewSilverlightObject(canSaveAsDraft) });
                }
            }
            catch
            { }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult ViewInlineReview(int assignmentID, int teamID)
        {
            try
            {
                //AbstractAssignmentActivity activity = db.AbstractAssignmentActivities.Find(abstractAssignmentActivityId);
                //TeamUserMember teamUser = db.TeamUsers.Find(teamUserId);
                Assignment assignment = db.Assignments.Find(assignmentID);
                AssignmentTeam at = db.AssignmentTeams.Find(assignmentID, teamID);

                ViewBag.assignment = assignment;
                ViewBag.assignmentTeam = at;
                //ViewBag.activity = activity;
                //ViewBag.TeamUser = teamUser;

                if (assignment.Category.CourseID == activeCourse.AbstractCourse.ID)
                {
                    foreach (TeamMember tm in at.Team.TeamMembers)
                    {
                        if (tm.CourseUser.UserProfileID == currentUser.ID)
                        {
                           // Session.Add("CurrentActivityID", activity.ID);
                            Session.Add("CurrentAssignmentID", assignment.ID);
                            //Session.Add("TeamUserID", teamUser.ID);
                            Session.Add("TeamID", at.TeamID);
                            return View("InlineReview", new InlineReviewViewModel() { ReviewInterface = ViewInlineReviewSilverlightObject() });
                        }
                    }
                }
            }
            catch
            { }

            return RedirectToAction("Index", "Home");
        }

        private SilverlightObject ViewInlineReviewSilverlightObject()
        {
            return new SilverlightObject
            {
                CSSId = "inline_review_silverlight",
                XapName = "ViewPeerReview",
                Width = "99%",
                Height = "99%",
                OnLoaded = "SLObjectLoaded",
                Parameters = new Dictionary<string, string>()
                {
                }
            };
        }

        private SilverlightObject createEditInlineReviewSilverlightObject(bool canSaveAsDraft)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            parameters.Add("CanSaveAsDraft", canSaveAsDraft.ToString());

            return new SilverlightObject
            {
                CSSId = "inline_review_silverlight",
                XapName = "EditPeerReview",
                Width = "99%",
                Height = "99%",
                OnLoaded = "SLObjectLoaded",
                Parameters = parameters
            };
        }
        
        public ActionResult AssignmentDetails (int id)
        {
            Assignment assignment = db.Assignments.Find(id);
            List<Score> scores = assignment.Scores.ToList();
            List<CourseUser> cuList = null;
            List<AssignmentDetailsViewModel> AssignmentDetailsList = new List<AssignmentDetailsViewModel>();

            if (activeCourse.AbstractRole.CanModify || activeCourse.AbstractRole.Anonymized) //Instructor setup || Observer
            {
                List<AssignmentTeam> teams = assignment.AssignmentTeams.ToList();
                List<DiscussionPost> allUserPosts = (from a in db.DiscussionPosts
                                                     where a.AssignmentID == assignment.ID
                                                     select a).ToList();

                if (activeCourse.AbstractRole.Anonymized)
                {
                    List<CourseUser> cus = db.CourseUsers.Where(c => c.AbstractCourseID == assignment.Category.Course.ID).ToList();
                    ViewBag.ObserverCU = cus.OrderBy(o => o.ID).ToList();
                }
                
                //Setting up Viewbag for Discussion team assignments
                if (assignment.AssignmentTypeID == 3 && assignment.HasDiscussionTeams)
                {
                    List<DiscussionTeam> discussionTeamList = assignment.DiscussionTeams.ToList();
                    discussionTeamList.Sort((x, y) => string.Compare(x.Team.Name, y.Team.Name));
                    ViewBag.DiscussionTeamList = discussionTeamList;
                    if (activeCourse.AbstractRole.Anonymized)
                    {
                        ViewBag.DiscussionTeamList = discussionTeamList.OrderBy(o => o.TeamID).ToList();
                    }     
                }

                //Sorting teams by team name (also last name if teams are of 1 because team name is the users name
                if (assignment.HasTeams)
                {
                    teams.Sort((x, y) => string.Compare(x.Team.Name, y.Team.Name));
                }

                /*MG Going through each team in the assignment and for each team going through the scores until a match is found
                 * the match will then be used for display information. 
                 *Conflict: There can multiple scores with the same team ID as each individual gets a Score in the DB. So this will only pick up first score found
                 */
                foreach (AssignmentTeam team in teams)
                {
                    int postCount = 0;
                    int replyCount = 0;
                    if (assignment.AssignmentTypeID == 3)
                    {
                        foreach (TeamMember tm in team.Team.TeamMembers)
                        {
                            postCount = (from a in allUserPosts
                                         where a.CourseUserID == tm.CourseUserID &&
                                         !a.IsReply
                                         select a).Count();
                            replyCount = (from a in allUserPosts
                                          where a.CourseUserID == tm.CourseUserID && 
                                          a.IsReply
                                          select a).Count();
                        }
                    }

                    //Grabbing the submission time for the assignment team
                    DateTime? subTime = new DateTime?();
                    if (assignment.HasDeliverables)
                    {
                        subTime = GetSubmissionTime(team.Assignment.Category.Course, team.Assignment, team);
                    }

                    //Checking for matched score, if there is none - add a null entry
                    bool foundMatch = false;
                    foreach (Score score in scores)
                    {
                        if (score.TeamID == team.TeamID)
                        {
                            AssignmentDetailsList.Add(new AssignmentDetailsViewModel(score, subTime, team.Team, postCount, replyCount));
                            foundMatch = true;
                            break;
                        }
                    }
                    if (!foundMatch)
                    {
                        AssignmentDetailsList.Add(new AssignmentDetailsViewModel(null, subTime, team.Team, postCount, replyCount));
                    }
                }

                //Just giving the first or defaults teams ID, the instructor link to the rubric won't be specific.
                ViewBag.TeamID = 0;
                if(assignment.AssignmentTeams.Count > 0)
                {
                    ViewBag.TeamID = assignment.AssignmentTeams.FirstOrDefault().TeamID;

                    //MG: Due to rubric controller change 
                    ViewBag.CurrentUserID = assignment.AssignmentTeams.FirstOrDefault().Team.TeamMembers.FirstOrDefault().CourseUser.ID;
                }

                //Setting up a list of evaluations
                List<RubricEvaluation> evaluations = (from e in db.RubricEvaluations
                                                      where e.AssignmentID == assignment.ID &&
                                                      e.IsPublished == false
                                                      select e).ToList();

                ViewBag.AssignmentDetailsVMList = AssignmentDetailsList;
                if (activeCourse.AbstractRole.Anonymized) 
                {
                    ViewBag.AssignmentDetailsVMList = AssignmentDetailsList.OrderBy(o => o.team.ID).ThenBy(c => c.team.TeamMembers.OrderBy(d => d.CourseUserID)).ToList();
                }
                ViewBag.RubricEvals = evaluations;
            }
            else if (activeCourse.AbstractRole.CanSubmit)//Student setup
            {
                AssignmentTeam at = GetAssignmentTeam(assignment, currentUser);
                ViewBag.TeamID = at.TeamID;
                ViewBag.CurrentUserID = activeCourse.ID;
                ViewBag.TeamName = at.Team.Name;
                DateTime? subTime = GetSubmissionTime(assignment.Category.Course, assignment, at);
                var score = (from assScore in assignment.Scores
                             where assScore.CourseUser.UserProfileID == CurrentUser.ID
                             select assScore).FirstOrDefault();
                ViewBag.Grade = "No Grade";
                if (score != null) //found matching score. Reassign scoreString
                {
                    ViewBag.Grade = (score as Score).getGradeAsPercent(assignment.PointsPossible);
                }
                if (subTime.HasValue)
                {
                    ViewBag.SubmissionTime = subTime.Value.ToString();
                }
                else
                {
                    ViewBag.SubmissionTime = "No Submission";
                }
                
                if (assignment.HasTeams)
                {
                    
                    foreach(TeamMember tm in at.Team.TeamMembers)
                    {
                        if (tm.CourseUser.ID != activeCourse.ID)
                        {
                            if (cuList == null) //init only when needed, otherwise leave null for view check.
                            {
                                cuList = new List<CourseUser>();
                            }
                            cuList.Add(tm.CourseUser);
                        }
                    }
                    
                }
            }
            ViewBag.TeamMembers = cuList; //null if no team members

            //MG: getting a list of the deliverables to list for assignment details. 
            List<string[]> fileTypes = new List<string[]>();
            foreach(Deliverable d in assignment.Deliverables)
            {
                fileTypes.Add(GetFileExtensions((DeliverableType)d.Type));
            }
            ViewBag.filetypeList = fileTypes;
            return View(assignment);
        }

        /// <summary>
        /// Toggles an assignment between draft and regular assignment. Draft assignments are not shown to students, and not
        /// used to calculate grades. 
        /// </summary>
        /// <param name="assignmentID"></param>
        [CanModifyCourse]
        public void ToggleDraft(int assignmentID)
        {
            //MG: Pulling the assignment from the DB, toggling its IsDraft parameter. and saving it back to the DB. 
            Assignment assignment = db.Assignments.Find(assignmentID);
            assignment.IsDraft = !assignment.IsDraft;
            db.SaveChanges();

            if (assignment.IsDraft)
            {
                if (assignment.AssociatedEvent != null)
                {
                    Event e = db.Events.Find(assignment.AssociatedEventID);
                    db.Events.Remove(e);
                    db.SaveChanges();
                }
            }
            else
            {
                Event e = new Event()
                {
                    Description = assignment.AssignmentDescription,
                    EndDate = assignment.DueDate,
                    EndTime = assignment.DueTime,
                    Approved = true,
                    PosterID = activeCourse.ID,
                    StartDate = assignment.ReleaseDate,
                    StartTime = assignment.ReleaseTime,
                    Title = assignment.AssignmentName
                };
                db.Events.Add(e);
                db.SaveChanges();
                assignment.AssociatedEventID = e.ID;
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Takes any grade that is currently saved as a draft for the specified assignment and 
        /// publishes the grade to the students.
        /// </summary>
        /// <param name="assignmentId"></param>
        [CanModifyCourse]
        public void PublishAllGrades(int assignmentId)
        {
            if (assignmentId > 0)
            {
                Assignment assignment = db.Assignments.Find(assignmentId);

                //Getting the list of evaluations that have been saved as draft
                List<RubricEvaluation> evaluations = (from e in db.RubricEvaluations
                                                      where e.AssignmentID == assignment.ID &&
                                                      e.IsPublished == false
                                                      select e).ToList();


                foreach (RubricEvaluation re in evaluations)
                {
                    re.IsPublished = true;

                    (new NotificationController()).SendRubricEvaluationCompletedNotification(assignment, re.Recipient);
                    GradebookController gradebook = new GradebookController();

                    //figure out the normalized final score.
                    double maxLevelScore = (from c in assignment.Rubric.Levels
                                            select c.RangeEnd).Sum();
                    double totalRubricPoints = (from c in assignment.Rubric.Criteria
                                                select c.Weight).Sum();
                    double studentScore = 0.0;

                    foreach (CriterionEvaluation critEval in re.CriterionEvaluations)
                    {
                        studentScore += (double)critEval.Score / maxLevelScore * (critEval.Criterion.Weight / totalRubricPoints);
                    }
                    
                    //normalize the score with the abstract assignment score
                    studentScore *= re.Assignment.PointsPossible;

                    gradebook.ModifyTeamGrade(studentScore, assignment.ID, re.Recipient.ID);
                }
                db.SaveChanges();
            }
        }


        /// <summary>
        /// Modifies a students custom late penalty. If scoreId == 0, we are assuming there is no score
        /// for the student.
        /// </summary>
        /// <param name="assignmentId"></param>
        [CanModifyCourse]
        public void ModifyLatePenalty(int scoreId, int courseUserId, double latePenalty, int assignmentId)
        {
            if (scoreId > 0)
            {
                Score score = db.Scores.Find(scoreId);
                score.CustomLatePenaltyPercent = latePenalty;
                db.SaveChanges();
                new GradebookController().ModifyGrade(score.RawPoints, courseUserId, score.AssignmentID);
            }
            else if (scoreId == 0)
            {
                new GradebookController().ModifyGrade(-1, courseUserId, assignmentId);
                Score score = (from s in db.Scores
                               where s.CourseUser.ID == courseUserId &&
                               s.AssignmentID == assignmentId
                               select s).FirstOrDefault();

                if (score != null)
                {
                    score.CustomLatePenaltyPercent = latePenalty;
                    db.SaveChanges();
                }
            }
            else
            {
                //If we got here there was a mistake, don't do anything
            }
        }

        /// <summary>
        /// This will take a Team and display the team evaluations to the teacher.
        /// </summary>
        /// <param name="teamId"></param>
        [CanModifyCourse]
        public ActionResult TeacherTeamEvaluation(int teamId, int assignmentId)
        {
            ViewBag.Team = db.Teams.Find(teamId);
            Team team = db.Teams.Find(teamId);
            ViewBag.Time = DateTime.Now;
            Assignment a = db.Assignments.Find(assignmentId);
            List<TeamEvaluation> teamEvaluations = (from t in db.TeamEvaluations
                                                    where t.TeamID == teamId &&
                                                    t.AssignmentID == a.ID
                                                    select t).ToList();
            List<TeamMemberEvaluation> teamMemberEvaluations = (from t in db.TeamMemberEvaluations
                                                                where t.TeamEvaluation.TeamID == teamId &&
                                                                t.TeamEvaluation.AssignmentID == a.ID
                                                                select t).ToList();
            if (teamEvaluations.Count > 0)
            {
                ViewBag.TeamEvaluations = teamEvaluations;
                ViewBag.TeamMemberEvaluations = teamMemberEvaluations;
                return View("_TeacherTeamEvaluationView");
            }
            else
            {
                return View("_TeacherAssignmentDetails");
            }            
        }

        /// <summary>
        /// This will take a Team and display the team evaluations to the teacher.
        /// </summary>
        /// <param name="teamId"></param
        [CanSubmitAssignments]
        public ActionResult StudentTeamEvaluation(int assignmentId)
        {
            Assignment a = db.Assignments.Find(assignmentId);
            AssignmentTeam pAt = GetAssignmentTeam(a.PreceedingAssignment, currentUser);
            AssignmentTeam at = GetAssignmentTeam(a, currentUser);
            if (at != null)
            {
                ViewBag.AssignmentTeam = at;
                ViewBag.PreviousAssignmentTeam = pAt;
                ViewBag.TeamMemberEvaluations = (from tme in db.TeamMemberEvaluations
                                                 where tme.EvaluatorID == activeCourse.ID &&
                                                 tme.TeamEvaluation.AssignmentID == a.ID
                                                 select tme).ToList();

                return View("_StudentTeamEvaluationView");
            }
            else
            {
                return View("Index");
            }
        }

        
        [HttpPost]
        public ActionResult SubmitTeamEvaluation(int assignmentId)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);

            //Get the assignment team for the current user
            AssignmentTeam at = GetAssignmentTeam(assignment, currentUser);
            AssignmentTeam pAt = GetAssignmentTeam(assignment.PreceedingAssignment, currentUser);
            int tmPoints;

            List<TeamMemberEvaluation> teamMemberEvaluation = (from t in db.TeamMemberEvaluations
                                                               where t.Evaluator.UserProfileID == currentUser.ID &&
                                                               t.TeamEvaluation.AssignmentID == assignment.ID
                                                               select t).ToList();

            string comments = Request.Params["inBrowserText"];
            if (teamMemberEvaluation.Count() > 0)
            {
                teamMemberEvaluation.FirstOrDefault().TeamEvaluation.Comments = comments;
                foreach (TeamMember tm in pAt.Team.TeamMembers)
                {
                    foreach (TeamMemberEvaluation tme in teamMemberEvaluation)
                    {
                        if (tme.RecipientID == tm.CourseUserID)
                        {
                            string param = "points-" + tm.CourseUserID;
                            tmPoints = Convert.ToInt32(Request.Params[param]);

                            tme.Points = tmPoints;

                        }
                    }
                }
                db.SaveChanges();
            }
            else
            {
                //Create a new team evaluation
                TeamEvaluation te = new TeamEvaluation()
                {
                    AssignmentID = assignment.ID,
                    TeamID = at.TeamID,
                    Comments = comments
                };
                db.TeamEvaluations.Add(te);
                db.SaveChanges();

                //Loop through the different team members on the team
                foreach (TeamMember tm in pAt.Team.TeamMembers)
                {
                    //Get the points from the view assigned to the specific recipient
                    string param = "points-" + tm.CourseUserID;
                    tmPoints = Convert.ToInt32(Request.Params[param]);

                    //Get the Team Member of the current user
                    TeamMember currentTm = GetTeamUser(assignment, currentUser);

                    //Create a team member evaluation
                    TeamMemberEvaluation tme = new TeamMemberEvaluation()
                    {
                        Points = tmPoints,
                        RecipientID = tm.CourseUserID,
                        EvaluatorID = currentTm.CourseUserID,
                        TeamEvaluationID = te.ID
                    };
                    db.TeamMemberEvaluations.Add(tme);
                }
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [CanModifyCourse]
        public void SubmitMultiplier(int assignmentId)
        {
            int currentCourseId = ActiveCourse.AbstractCourseID;
            Assignment assignment = db.Assignments.Find(assignmentId);

            List<CourseUser> studentList = (from cu in db.CourseUsers
                                            where cu.AbstractCourseID == currentCourseId && 
                                            cu.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                            orderby cu.UserProfile.LastName, cu.UserProfile.FirstName
                                            select cu).ToList();
                                            
            List<TeamMemberEvaluation> teamEvaluations = (from te in db.TeamMemberEvaluations
                                                          where te.TeamEvaluation.AssignmentID == assignmentId
                                                          select te).ToList();

            foreach (CourseUser user in studentList)
            {
                double multiplier = (from m in teamEvaluations
                                     where m.RecipientID == user.ID
                                     select m.Points).Sum();

                assignment.PreceedingAssignment.Scores.Where(s => s.CourseUserID == user.ID).FirstOrDefault().Multiplier = multiplier;
                db.SaveChanges();
            }

        }
    }
}

