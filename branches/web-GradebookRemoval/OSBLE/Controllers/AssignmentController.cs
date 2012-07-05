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
            if (Cache["SubmissionReceived"] != null && Convert.ToBoolean(Cache["SubmissionReceived"]) == true)
            {
                ViewBag.SubmissionReceived = true;
                ViewBag.SubmissionReceivedAssignmentID = Cache["SubmissionReceivedAssignmentID"];
                Cache["SubmissionReceived"] = false;
            }
            else
            {
                ViewBag.SubmissionReceived = false;
                Cache["SubmissionReceived"] = false;
            }

            List<Assignment> Assignments = new List<Assignment>();
            //Getting the assginment list, without draft assignments.
            Assignments = (from assignment in db.Assignments
                           where !assignment.IsDraft &&
                           assignment.Category.CourseID == ActiveCourseUser.AbstractCourseID &&
                           assignment.IsWizardAssignment
                           orderby assignment.DueDate
                           select assignment).ToList();

            if (ActiveCourseUser.AbstractRole.CanGrade || ActiveCourseUser.AbstractRole.Anonymized)
            {
                // Draft assignments (viewable by instructor only) are assignments that have not yet been published to students. Appending to list here.
                Assignments.AddRange((from assignment in db.Assignments
                                      where assignment.IsDraft &&
                                      assignment.IsWizardAssignment &&
                                      assignment.Category.CourseID == ActiveCourseUser.AbstractCourseID
                                      orderby assignment.ReleaseDate
                                      select assignment).ToList());
            }
            else if (ActiveCourseUser.AbstractRole.CanSubmit)
            {
                //MG: creating a dictionary<assignmentID, submissionTime>
                // to be used in the view. This is only done for the students view.

                //This will hold the assignment ID, the date submitted in string format:, the grade in string format, and the team ID
                //submission time
                //score as string (or "No Grade" if there is not one)
                //assignmentTeam for that submission
                Dictionary<int, Tuple<string, string, AssignmentTeam>> submissionInfo = new Dictionary<int, Tuple<string, string, AssignmentTeam>>();
                foreach (Assignment a in Assignments)
                {
                    //populating tuple to add to dictionary by collecting the information described in the above commentblock.
                    AssignmentTeam at = GetAssignmentTeam(a, ActiveCourseUser);
                    DateTime? subTime = FileSystem.GetSubmissionTime(at);
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
                                                        where t.EvaluatorID == ActiveCourseUser.ID
                                                        select t).ToList();

                ViewBag.TeamEvaluations = teamEvaluations;
                ViewBag.CourseUser = ActiveCourseUser;
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
            return View("Index");
        }

        /// <summary>
        /// Toggles an assignment between draft and regular assignment. Draft assignments are not shown to students, and not
        /// used to calculate grades.
        /// </summary>
        /// <param name="assignmentID"></param>
        [CanModifyCourse]
        public ActionResult ToggleDraft(int assignmentID)
        {
            Assignment assignment = db.Assignments.Find(assignmentID);

            //Confirm assignment belongs to the current users course before proceeding
            if (assignment.Category.CourseID == ActiveCourseUser.AbstractCourse.ID) 
            {
                Assignment.ToggleDraft(assignmentID, ActiveCourseUser.ID);
            }
            return RedirectToRoute(new { action = "Index" });
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
        /// This will display the team evaluations to the teacher. 
        /// </summary>
        /// <param name="precedingTeamId">The teamId from the TeamEvaluation's preceding assignment.</param>
        /// <param name="TeamEvaluationAssignmentId">The assignment ID of the TeamEvaluation assignment</param>
        /// <returns></returns>
        [CanModifyCourse]
        public ActionResult TeacherTeamEvaluation(int precedingTeamId, int TeamEvaluationAssignmentId)
        {
            Assignment assignment = db.Assignments.Find(TeamEvaluationAssignmentId);
            Team precTeam = db.Teams.Find(precedingTeamId);

            var cuIDs = (from tm in precTeam.TeamMembers
                         select tm.CourseUserID).ToList();
            var query = db.TeamEvaluations.Where(te => cuIDs.Contains(te.RecipientID) && te.TeamEvaluationAssignmentID == assignment.ID);
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
        /// Generates a team evaluation view for the currentUser for assignmentId.
        /// </summary>
        /// <param name="teamId"></param
        [CanSubmitAssignments]
        public ActionResult StudentTeamEvaluation(int assignmentId)
        {
            Assignment a = db.Assignments.Find(assignmentId);
            AssignmentTeam pAt = GetAssignmentTeam(a.PreceedingAssignment, ActiveCourseUser);
            AssignmentTeam at = GetAssignmentTeam(a, ActiveCourseUser);
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
             AssignmentTeam at = GetAssignmentTeam(assignment, ActiveCourseUser);
             AssignmentTeam pAt = GetAssignmentTeam(assignment.PreceedingAssignment, ActiveCourseUser);
            List<TeamEvaluation> existingTeamEvaluations = (from te in db.TeamEvaluations
                                                            where te.TeamEvaluationAssignmentID == assignmentId &&
                                                            te.EvaluatorID == ActiveCourseUser.ID
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

            List<int> cuIDs = (from tm in precTeam.TeamMembers
                         select tm.CourseUserID).ToList();  
            List<TeamEvaluation> OurTeamEvals = db.TeamEvaluations.Where(te => cuIDs.Contains(te.RecipientID) 
                && te.TeamEvaluationAssignmentID == assignment.ID).ToList();
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
        /// This function will publish all the critical reviews for a critical review assignment. Allowing students to download their evaluated 
        /// documents.
        /// </summary>
        /// <param name="assignmentID">critical review assignment ID</param>
        /// <returns></returns>
        [CanModifyCourse]
        public ActionResult PublishAllCriticalReviews(int assignmentID)
        {
            Assignment assignment = db.Assignments.Find(assignmentID);
            assignment.CriticalReviewPublishDate = DateTime.Now;
            db.SaveChanges();
            return RedirectToAction("Index", "Home", new { area = "AssignmentDetails", assignmentId = assignmentID });
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
