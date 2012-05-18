using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Models.HomePage;
using OSBLE.Models.ViewModels;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
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
            if (!ActiveCourse.AbstractRole.CanModify)
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
                           assignment.Category.CourseID == ActiveCourse.AbstractCourseID &&
                           assignment.IsWizardAssignment &&
                           assignment.ReleaseDate <= DateTime.Now
                           orderby assignment.DueDate
                           select assignment).ToList();

            // Future assignments are non-draft assignments whose start date has not yet happened. Appending to list now.
            Assignments.AddRange((from assignment in db.Assignments
                                  where !assignment.IsDraft &&
                                  assignment.Category.CourseID == ActiveCourse.AbstractCourseID &&
                                  assignment.IsWizardAssignment == true &&
                                  assignment.ReleaseDate > DateTime.Now
                                  orderby assignment.ReleaseDate
                                  select assignment).ToList());

            if (ActiveCourse.AbstractRole.CanModify || ActiveCourse.AbstractRole.Anonymized)
            {
                // Draft assignments (viewable by instructor only) are assignments that have not yet been published to students. Appending to list now.
                Assignments.AddRange((from assignment in db.Assignments
                                      where assignment.IsDraft &&
                                      assignment.IsWizardAssignment &&
                                      assignment.Category.CourseID == ActiveCourse.AbstractCourseID
                                      orderby assignment.ReleaseDate
                                      select assignment).ToList());
            }
            else if (ActiveCourse.AbstractRole.CanSubmit)
            {
                /*MG: gathering a list of assignments for that course that are non-draft. Then creating a dictionary<assignmentID, submissionTime>
                 * to be used in the view. This is only done for the students view.
                 */
                List<Assignment> assignmentList = (from assignment in db.Assignments
                                                   where !assignment.IsDraft &&
                                                   assignment.IsWizardAssignment &&
                                                   assignment.Category.CourseID == ActiveCourse.AbstractCourseID
                                                   select assignment).ToList();

                //This will hold the assignment ID, the date submitted in string format:, the grade in string format, and the team ID
                //submission time
                //score as string (or "No Grade" if there is not one)
                //assignmentTeam for that submission
                Dictionary<int, Tuple<string, string, AssignmentTeam>> submissionInfo = new Dictionary<int, Tuple<string, string, AssignmentTeam>>();
                foreach (Assignment a in assignmentList)
                {
                    //populating tuple to add to dictionary by collecting the information described in the above commentblock.
                    AssignmentTeam at = OSBLEController.GetAssignmentTeam(a, CurrentUser);
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

                //Gathering the Team Evaluations for the current user's teams.
                List<TeamEvaluation> teamEvaluations = (from t in db.TeamEvaluations
                                                        where t.EvaluatorID == ActiveCourse.ID
                                                        select t).ToList();

                ViewBag.TeamEvaluations = teamEvaluations;
                ViewBag.CourseUser = ActiveCourse;
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
                if (assignment.Category.CourseID == ActiveCourse.AbstractCourseID && assignment.AssignmentTeams.Contains(team))
                {
                    Session.Add("CurrentAssignmentID", assignmentID);
                    Session.Add("TeamID", teamID);

                    //if publish file exists then teacher can not save as draft
                    bool canSaveAsDraft = !(new FileInfo(FileSystem.GetTeamUserPeerReview(false, ActiveCourse.AbstractCourse as Course, assignmentID, teamID)).Exists);

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

                if (assignment.Category.CourseID == ActiveCourse.AbstractCourse.ID)
                {
                    foreach (TeamMember tm in at.Team.TeamMembers)
                    {
                        if (tm.CourseUser.UserProfileID == CurrentUser.ID)
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

        public ActionResult AssignmentDetails(int id)
        {
            Assignment assignment = db.Assignments.Find(id);
            List<Score> scores = assignment.Scores.ToList();
            List<CourseUser> cuList = null;
            List<AssignmentDetailsViewModel> AssignmentDetailsList = new List<AssignmentDetailsViewModel>();
            List<AssignmentTeam> teams = new List<AssignmentTeam>();

            if (ActiveCourse.AbstractRole.CanModify || ActiveCourse.AbstractRole.Anonymized) //Instructor || Observer setup 
            {
                if (assignment.Type == AssignmentTypes.TeamEvaluation)
                {
                    if (assignment.PreceedingAssignment.Type == AssignmentTypes.DiscussionAssignment) //preceding assignment is discussion, use discussion teams
                    {
                        //TODO: Handle getting teams for a discussion assignment as the previous assignment type. Note: You will have to get the discussion teams rather than assignment teams.
                    }
                    else
                    {
                        teams = assignment.PreceedingAssignment.AssignmentTeams.ToList();
                    }
                }
                else
                {
                    teams = assignment.AssignmentTeams.ToList();
                }
                List<DiscussionPost> allUserPosts = (from a in db.DiscussionPosts
                                                     where a.AssignmentID == assignment.ID
                                                     select a).ToList();

                if (ActiveCourse.AbstractRole.Anonymized)
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
                    if (ActiveCourse.AbstractRole.Anonymized)
                    {
                        ViewBag.DiscussionTeamList = discussionTeamList.OrderBy(o => o.TeamID).ToList();
                    }
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

                    int l_teamEvalsCompleted = 0;
                    int l_teamEvalsTotal = 0;
                    if (assignment.Type == AssignmentTypes.TeamEvaluation)
                    {
                        l_teamEvalsCompleted = 0;
                        foreach(TeamMember member in team.Team.TeamMembers)
                        {
                            int completedEvals = db.TeamEvaluations.Where(e => e.EvaluatorID == member.CourseUserID && e.TeamEvaluationAssignmentID == id).Count();
                            
                            if(completedEvals > 0)
                            {
                                l_teamEvalsCompleted++;
                            }
                        }
                        l_teamEvalsTotal = team.Team.TeamMembers.Count;
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
                            AssignmentDetailsList.Add(new AssignmentDetailsViewModel(assignment.ID, score, subTime, team.Team, postCount, replyCount) { teamEvalsCompleted = l_teamEvalsCompleted, teamEvalsTotal = l_teamEvalsTotal });
                            foundMatch = true;
                            break;
                        }
                    }
                    if (!foundMatch)
                    {
                        AssignmentDetailsList.Add(new AssignmentDetailsViewModel(assignment.ID, null, subTime, team.Team, postCount, replyCount) { teamEvalsCompleted = l_teamEvalsCompleted, teamEvalsTotal = l_teamEvalsTotal });
                    }
                }

                //Just giving the first or defaults teams ID, the instructor link to the rubric won't be specific.
                ViewBag.TeamID = 0;
                if (assignment.AssignmentTeams.Count > 0)
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
                if (assignment.HasTeams || (assignment.AssignmentTypeID == 4 && assignment.PreceedingAssignment.HasTeams))
                {
                    ViewBag.AssignmentDetailsVMList = AssignmentDetailsList.OrderBy(tn => tn.team.Name);
                }
                else
                {
                    ViewBag.AssignmentDetailsVMList = AssignmentDetailsList.OrderBy(l => l.team.TeamMembers.FirstOrDefault().CourseUser.UserProfile.LastName).ThenBy(f => f.team.TeamMembers.FirstOrDefault().CourseUser.UserProfile.FirstName);
                }

                if (ActiveCourse.AbstractRole.Anonymized)
                {
                    ViewBag.AssignmentDetailsVMList = AssignmentDetailsList.OrderBy(o => o.team.ID).ThenBy(c => c.team.TeamMembers.OrderBy(d => d.CourseUserID)).ToList();
                }
                ViewBag.RubricEvals = evaluations;
            }
            else if (ActiveCourse.AbstractRole.CanSubmit)//Student setup
            {
                AssignmentTeam at = GetAssignmentTeam(assignment, CurrentUser);
                ViewBag.TeamID = at.TeamID;
                ViewBag.CurrentUserID = ActiveCourse.ID;
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
                    foreach (TeamMember tm in at.Team.TeamMembers)
                    {
                        if (tm.CourseUser.ID != ActiveCourse.ID)
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
            ViewBag.AssignmentType = AssignmentTypeExtensions.Explode((AssignmentTypes)assignment.AssignmentTypeID);
            ViewBag.TeamMembers = cuList; //null if no team members

            //MG: getting a list of the deliverables to list for assignment details.
            List<string[]> fileTypes = new List<string[]>();
            foreach (Deliverable d in assignment.Deliverables)
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
                    PosterID = ActiveCourse.ID,
                    StartDate = assignment.ReleaseDate,
                    StartTime = assignment.ReleaseTime,
                    Title = assignment.AssignmentName
                };
                db.Events.Add(e);
                db.SaveChanges();
                assignment.AssociatedEventID = e.ID;
                db.SaveChanges();
            }
            string requestUrl = Request.UrlReferrer.ToString();
            Response.Redirect(requestUrl);

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
        /// This will take a TeamID (previous assignment teamID) and display the team evaluations to the teacher. 
        /// </summary>
        /// <param name="teamId"></param>
        [CanModifyCourse]
        public ActionResult TeacherTeamEvaluation(int teamId, int assignmentId)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);
            Team precTeam = db.Teams.Find(teamId);

            var cuIDs = (from tm in precTeam.TeamMembers
                         select tm.CourseUserID).ToList();

            List<TeamEvaluation> OurTeamEvals = db.TeamEvaluations.Where(te => cuIDs.Contains(te.RecipientID) && te.TeamEvaluationAssignmentID == assignment.ID).ToList();
            List<double> MultipliersInOrder = new List<double>();
            List<Score> ScoresInOrder = new List<Score>();
            List<CourseUser> CourseUsersInOrder = (from tm in precTeam.TeamMembers
                                                   orderby tm.CourseUser.UserProfile.LastName, tm.CourseUser.UserProfile.FirstName
                                                   select tm.CourseUser).ToList();
            double[,] table;

            table = new double[precTeam.TeamMembers.Count, precTeam.TeamMembers.Count+1];
            int i = 0;
            int j = 0;

            foreach (CourseUser cu in CourseUsersInOrder)
            {
                j = 0;
                List<TeamEvaluation> myEvals= (from te in OurTeamEvals
                                 where te.EvaluatorID == cu.ID
                                 select te).ToList().OrderBy(te => te.Recipient.UserProfile.LastName).ThenBy(te => te.Recipient.UserProfile.FirstName).ToList();

                Score myScore = (from s in assignment.PreceedingAssignment.Scores
                                 where s.CourseUserID == cu.ID
                                 select s).FirstOrDefault();
                ScoresInOrder.Add(myScore);

                //Adding Multiplier. Use Score's multiplier if its value is non-null. Otherwise, calculate the multiplier from the evals. If there are no
                //evaluations, then their multiplier is 1.
                if (myScore != null && myScore.Multiplier != null)
                {
                    MultipliersInOrder.Add((double)myScore.Multiplier);
                }
                else if (OurTeamEvals.Count > 0) //Only calculate multiplier if there are evaluations
                {
                    double myPoints = (from te in OurTeamEvals
                                       where te.RecipientID == cu.ID
                                       select te.Points).Sum();

                    double myMulti = myPoints / ((OurTeamEvals.Count / precTeam.TeamMembers.Count) * 100);
                    MultipliersInOrder.Add(myMulti);
                }
                else
                {
                    MultipliersInOrder.Add(1.0);
                }
                

                if (myEvals != null && myEvals.Count > 0)
                {
                    foreach (TeamEvaluation te in myEvals)
                    {
                        table[i,j] = te.Points;
                        j++;
                    }
                }
                else
                {
                    foreach (TeamMember tm2 in precTeam.TeamMembers)
                    {
                        table[i, j] = 0;
                        j++;
                    }
                }
                i++;
            }

            ViewBag.ScoresInOrder = ScoresInOrder;
            ViewBag.CourseUsersInOrder = CourseUsersInOrder;
            ViewBag.MultipliersInOrder = MultipliersInOrder;
            ViewBag.Table = table;
            ViewBag.Team = precTeam; //Note: ViewBag.Team is actually the preceding assignment's team. 
            ViewBag.Assignment = assignment;
            ViewBag.PrecedingAssignment = assignment.PreceedingAssignment;

            return View("_TeacherTeamEvaluationView");
        }

        /// <summary>
        /// This will take a Team and display the team evaluations to the Observer.
        /// </summary>
        /// <param name="teamId"></param>
        public ActionResult ObserverTeamEvaluation(int teamId, int assignmentId)
        {
            ViewBag.Team = db.Teams.Find(teamId);
            ViewBag.Time = DateTime.Now;
            Assignment a = db.Assignments.Find(assignmentId);

            Team team = db.Teams.Find(teamId);

            List<TeamEvaluation> teamEvaluations = new List<TeamEvaluation>();/* (from t in db.TeamEvaluations
                                                    where t.TeamID == teamId &&
                                                    t.AssignmentID == a.ID
                                                    select t).OrderBy(u => u.TeamID).ToList();*/
            List<TeamEvaluation> TeamEvaluations = new List<TeamEvaluation>(); /* (from t in db.TeamEvaluations
                                                                where t.TeamEvaluation.TeamID == teamId &&
                                                                t.TeamEvaluation.AssignmentID == a.ID
                                                                select t).OrderBy(u => u.RecipientID).ToList();
                                                                                * */
            AssignmentTeam at = GetAssignmentTeam(a.PreceedingAssignment, team.TeamMembers.FirstOrDefault().CourseUser.UserProfile);
            ViewBag.Team = at;

            ViewBag.TeamEvaluations = teamEvaluations;
            ViewBag.TeamEvaluations = TeamEvaluations;
            ViewBag.Assignment = a;

            return View("_TeacherTeamEvaluationView");
        }

        /// <summary>
        /// Generates a team evaluation view for the currentUser for assignmentId.
        /// </summary>
        /// <param name="teamId"></param
        [CanSubmitAssignments]
        public ActionResult StudentTeamEvaluation(int assignmentId)
        {
            Assignment a = db.Assignments.Find(assignmentId);
            AssignmentTeam pAt = GetAssignmentTeam(a.PreceedingAssignment, CurrentUser);
            AssignmentTeam at = GetAssignmentTeam(a, CurrentUser);
            if (at != null && a.Type == AssignmentTypes.TeamEvaluation)
            {
                ViewBag.AssignmentTeam = at;
                ViewBag.PreviousAssignmentTeam = pAt;
                
                List<TeamEvaluation> teamEvals = (from te in db.TeamEvaluations
                                           where
                                               te.TeamEvaluationAssignmentID == assignmentId &&
                                               te.EvaluatorID == ActiveCourse.ID
                                           orderby te.Recipient.UserProfile.LastName
                                           select te).ToList();
                //MG: evaluator (currentuser) must have completed at as many evaluations as team members from the previous assignment. 
                //Otherwise, use artificial team evals for view
                if (teamEvals.Count < pAt.Team.TeamMembers.Count)
                {
                    List<TeamEvaluation> artificialTeamEvals = new List<TeamEvaluation>();

                    foreach (TeamMember tm in pAt.Team.TeamMembers.OrderBy(tm2 => tm2.CourseUser.UserProfile.LastName))
                    {
                        TeamEvaluation te = new TeamEvaluation();
                        te.Points = 0;
                        te.Recipient = tm.CourseUser;
                        artificialTeamEvals.Add(te);
                    }
                    ViewBag.SubmitButtonValue = "Submit";
                    ViewBag.TeamEvaluations = artificialTeamEvals;
                    ViewBag.InitialPointsPossible = pAt.Team.TeamMembers.Count * 100;
                }
                else
                {
                    ViewBag.InitialPointsPossible = 0; //Must be 0 as we are reloading old TEs, and requirements for submitting initially are that points possible must be 0
                    ViewBag.Comment = teamEvals.FirstOrDefault().Comment;
                    ViewBag.SubmitButtonValue = "Resubmit";
                    ViewBag.TeamEvaluations = teamEvals;
                }

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
            AssignmentTeam at = GetAssignmentTeam(assignment, CurrentUser);
            AssignmentTeam pAt = GetAssignmentTeam(assignment.PreceedingAssignment, CurrentUser);
            List<TeamEvaluation> existingTeamEvaluations = (from te in db.TeamEvaluations
                                                            where te.TeamEvaluationAssignmentID == assignmentId &&
                                                            te.EvaluatorID == ActiveCourse.ID
                                                            select te).ToList();

            int existingCommentID = (from C in existingTeamEvaluations
                                     where C.CommentID != 0
                                     select C.CommentID).FirstOrDefault();
            TeamEvaluationComment tec;
            if (existingCommentID != 0) //Comment already existed. Modify that and use that.
            {

                tec = (from tc in db.TeamEvaluationComments
                       where tc.ID == existingCommentID
                       select tc).FirstOrDefault();


                tec.Comment = Convert.ToString(Request.Params["inBrowserText"]);
                db.SaveChanges();
            }
            else //using new comment
            {
                tec = new TeamEvaluationComment();
                tec.Comment = Convert.ToString(Request.Params["inBrowserText"]);
                db.TeamEvaluationComments.Add(tec);
                db.SaveChanges();
            }

            //Creating or editing TeamEvaluations for each team member from the previous assignment assignment team
            foreach (TeamMember tm in pAt.Team.TeamMembers)
            {
                TeamEvaluation te = (from eval in existingTeamEvaluations
                                     where eval.RecipientID == tm.CourseUserID
                                     select eval).FirstOrDefault();

                string param = "points-" + tm.CourseUserID;
                int paramPoints = Convert.ToInt32(Request.Params[param]);

                if (te == null) //No TE exists, create one
                {
                    TeamEvaluation newTE = new TeamEvaluation();
                    newTE.TeamEvaluationAssignmentID = assignmentId;
                    newTE.AssignmentUnderReviewID = (int)assignment.PrecededingAssignmentID;
                    newTE.EvaluatorID = ActiveCourse.ID;
                    newTE.RecipientID = tm.CourseUserID;
                    newTE.Points = paramPoints;
                    newTE.CommentID = tec.ID;

                    db.TeamEvaluations.Add(newTE);
                }
                else //TE exists, modify it
                {
                    te.CommentID = tec.ID;
                    te.Points = paramPoints;
                }
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        [CanModifyCourse]
        private void PublishTeamMultiplierHelper(int precedingAssignmentTeamID, int assignmentId)
        {
            //MG: publishTeamMultipliers - Handles scores for the assignment and the previous assignment.
            //The following steps need to be run for each user in the team
            //Step 0: Score for previous assignment. Pull it, if it does not exist, create it with points == -1. 
            //Step 1: Find multiplier. First try grabbing the scores multiplier, if its non-null use that. If it is null,
            //calculate the multiplier using team evaluations. If there are NO team evaluations for the whole team - use 1 as the multiplier. Save Multiplier into score and do a DB save
            //Step 2: Once the multiplier has been applied to the score - recalculate the grade using GBC.ModifyGrade(rawpoints,yada,yada). 
            //In order for ModifyGrade to use the multiplier, set Published to true for the score. Additionally, set PublishDate at this point.
            //Step 3: Now that the previous assignment grades are updated, scores must be created/updated for the Team Evaluation assignment
            //Start by pulling the score for the user. If it does not exist, create it. Users will get 0 points if they did 0 evals. Otherwise, full points.

            Team precTeam = db.Teams.Find(precedingAssignmentTeamID);
            Assignment assignment = db.Assignments.Find(assignmentId);

            //PEER REVIEW: During Peer review ask about better ways to do this. Seems like it would perform poorly as the TE table will be very large (as each user TE translates to TeamMember.Count amount of TEs in db)
            List<int> cuIDs = (from tm in precTeam.TeamMembers
                         select tm.CourseUserID).ToList();  
            List<TeamEvaluation> OurTeamEvals = db.TeamEvaluations.Where(te => cuIDs.Contains(te.RecipientID) && te.TeamEvaluationAssignmentID == assignment.ID).ToList();
            GradebookController GBC =  new GradebookController();

            foreach (TeamMember tm in precTeam.TeamMembers)
            {
                //Step 0
                Score prevAssignScore = (from s in assignment.PreceedingAssignment.Scores
                                         where s.CourseUserID == tm.CourseUserID
                                         select s).FirstOrDefault();

                if (prevAssignScore == null)
                {
                    prevAssignScore = new Score()
                    {
                        CourseUserID = tm.CourseUserID,
                        AssignmentID = assignment.PreceedingAssignment.ID,
                        Published = false,
                        AddedPoints = assignment.PreceedingAssignment.addedPoints,
                        Points = -1,
                        TeamID = precTeam.ID
                    };
                    db.Scores.Add(prevAssignScore);
                }

                //Step 1
                if (prevAssignScore.Multiplier == null)
                {
                    double multiplier = 1.0;
                    if (OurTeamEvals.Count > 0)
                    {
                        //Score.Multiplier wasn't set up, and there are more than 0 team evals, so calculate multiplier.
                        double myEvalPoints = (from e in OurTeamEvals
                                               where e.RecipientID == tm.CourseUserID
                                               select e.Points).Sum();

                        multiplier = myEvalPoints / ((OurTeamEvals.Count / precTeam.TeamMembers.Count) * 100);
                    }
                    prevAssignScore.Multiplier = multiplier;
                    db.SaveChanges();
                }

                //Step 2
                prevAssignScore.Published = true;
                prevAssignScore.PublishedDate = DateTime.Now;
                if(prevAssignScore.RawPoints > 0) //Only recalculating grade if they have raw points.
                {
                    GBC.ModifyGrade(prevAssignScore.RawPoints, tm.CourseUserID, (int)assignment.PrecededingAssignmentID);
                }
                
                //Step 3
                Score currentAssignScore = (from s in assignment.Scores
                                            where s.CourseUserID == tm.CourseUserID
                                            select s).FirstOrDefault();

                int myCompletedEvalsCount = (from e in OurTeamEvals
                                             where e.EvaluatorID == tm.CourseUserID
                                             select e).Count();

                double myPoints = 0.0;
                if(myCompletedEvalsCount > 0)
                {
                    myPoints = assignment.PointsPossible;
                }


                if (currentAssignScore != null)
                {
                    currentAssignScore.RawPoints = myPoints;
                }
                else
                {
                    int teamId = (from at in assignment.AssignmentTeams
                                  where at.Team.TeamMembers.FirstOrDefault().CourseUserID == tm.CourseUserID
                                  select at.TeamID).FirstOrDefault();

                    currentAssignScore = new Score()
                    {
                        AddedPoints = assignment.addedPoints,
                        AssignmentID = assignment.ID,
                        CourseUserID = tm.CourseUserID,
                        RawPoints = myPoints,
                        TeamID = teamId
                    };
                    db.Scores.Add(currentAssignScore);
                }
                db.SaveChanges();
                GBC.ModifyGrade(currentAssignScore.RawPoints, tm.CourseUserID, assignment.ID);
            }
        }

        /// <summary>
        /// This function publishes the Team Evaluation multipliers to the preceding assignment, as well as creates grades for the team evaluation assignment
        /// </summary>
        /// <param name="precedingAssignmentTeamID"> the teamId for the preceding assignment (NOT the Team Evaluation assignment) </param>
        /// <param name="assignmentId">the assignment Id for the Team Evaluation assignment. </param>
        /// <returns></returns>
        [CanModifyCourse]
        public ActionResult PublishTeamMultiplier(int precedingAssignmentTeamID, int assignmentId)
        {
            //MG: PublishTeamMultiplier
            //Step 0: publishTeamMultiplier
            //Step 1: Redirect to original location. 
            PublishTeamMultiplierHelper(precedingAssignmentTeamID, assignmentId);

            return RedirectToAction("AssignmentDetails", new { id = assignmentId });
        }

        [CanModifyCourse]
        [Obsolete]
        public void PublishAllMultipliers(int assignmentId)
        {
        }


        /// <summary>
        /// Returns true if it successfully modifies the user's score's value.
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="cuID"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        [CanModifyCourse]
        public bool ModifyMultiplier(int teamId, int cuID, double? value)
        {
            bool returnVal = false;
            if (teamId > 0 && cuID > 0 && (value > 0 || value == null))
            {
                Score usersScore = (from s in db.Scores
                                    where s.TeamID == teamId &&
                                    s.CourseUserID == cuID
                                    select s).FirstOrDefault();

                if (usersScore != null)
                {
                    usersScore.Multiplier = value;
                    db.SaveChanges();
                    GradebookController GBC = new GradebookController();
                    GBC.ModifyGrade(usersScore.RawPoints, cuID, usersScore.AssignmentID);
                    returnVal = true;
                }
            }
            return returnVal;
        }
    }
}
