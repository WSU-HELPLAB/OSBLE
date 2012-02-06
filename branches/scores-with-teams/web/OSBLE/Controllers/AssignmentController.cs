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
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses.Rubrics;

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
            
            // Past assignments are non-draft assignments whose final stop date has already passed.
            List<Assignment> pastAssignments = (from assignment in db.Assignments
                                                where !assignment.IsDraft &&
                                                assignment.Category.CourseID == activeCourse.AbstractCourseID &&
                                                assignment.IsWizardAssignment &&
                                                assignment.DueDate <= DateTime.Now
                                                orderby assignment.DueDate
                                                select assignment).ToList();

            // Present assignments are any (non-draft) for which we are between the first start date and last end date.
            List<Assignment> presentAssignments = (from assignment in db.Assignments
                                                   where !assignment.IsDraft &&
                                                   assignment.Category.CourseID == activeCourse.AbstractCourseID &&
                                                   assignment.IsWizardAssignment &&
                                                   assignment.DueDate > DateTime.Now &&
                                                   assignment.ReleaseDate <= DateTime.Now
                                                   orderby assignment.DueDate
                                                   select assignment).ToList();

            // Future assignments are non-draft assignments whose start date has not yet happened.
            List<Assignment> futureAssignments = (from assignment in db.Assignments
                                                  where !assignment.IsDraft &&
                                                  assignment.Category.CourseID == activeCourse.AbstractCourseID &&
                                                  assignment.IsWizardAssignment == true &&
                                                  assignment.ReleaseDate > DateTime.Now
                                                  orderby assignment.ReleaseDate
                                                  select assignment).ToList();

            List<Assignment> draftAssignments = new List<Assignment>();
            if (ActiveCourse.AbstractRole.CanModify)
            {
                // Draft assignments (viewable by instructor only) are assignments that have not yet been published to students
                draftAssignments = (from assignment in db.Assignments
                                    where assignment.IsDraft &&
                                    assignment.IsWizardAssignment &&
                                    assignment.Category.CourseID == activeCourse.AbstractCourseID
                                    orderby assignment.ReleaseDate
                                    select assignment).ToList();
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
                Dictionary<int, Tuple<string, string, int>> submissionInfo = new Dictionary<int, Tuple<string, string, int>>();
                foreach (Assignment a in assignmentList)
                {
                    AssignmentTeam at = OSBLEController.GetAssignmentTeam(a, currentUser);
                    DateTime? subTime = GetSubmissionTime(a.Category.Course, a, at);
                    string submissionTime = "No Submission";
                    string scoreString = "No Grade";
                    if (subTime != null) //found a submission time, Reassign submissionTime
                    {
                        submissionTime = subTime.Value.ToString();
                    }


                    //Finding score match based of UserPrfileID to avoid grabbing another team members grade (as they are potentially different)
                    var score = (from assScore in a.Scores
                                 where assScore.CourseUser.UserProfileID == CurrentUser.ID
                                 select assScore).FirstOrDefault();
                    if (score != null) //found matching score. Reassign scoreString
                    {
                        scoreString = (score as Score).getGradeAsPercent(a.PointsPossible);
                    }
                    submissionInfo.Add(a.ID, new Tuple<string, string, int>(submissionTime, scoreString, at.Team.TeamMembers.FirstOrDefault().CourseUserID));
                }
                ViewBag.SubmissionInfoDictionary = submissionInfo; 
            }
            
            ViewBag.PastAssignments = pastAssignments;
            ViewBag.PresentAssignments = presentAssignments;
            ViewBag.FutureAssignments = futureAssignments;
            ViewBag.DraftAssignments = draftAssignments;
            ViewBag.DeliverableTypes = GetListOfDeliverableTypes();
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

            if (activeCourse.AbstractRole.CanModify) //Instructor setup
            {
                List<Tuple<Score, Team, string>> scoreAndTeam = new List<Tuple<Score, Team, string>>();
                List<AssignmentTeam> teams = assignment.AssignmentTeams.ToList();

                //Sorting teams by team name or by last name if non-team assignment
                if (assignment.HasTeams)
                {
                    teams.Sort((x, y) => string.Compare(x.Team.Name, y.Team.Name));
                }
                else
                {
                    teams.Sort((x, y) => string.Compare(x.Team.Name, y.Team.Name));
                }

                string submissionTime;
                /*MG Going through each team in the assignment and for each team going through the scores until a match is found
                 * the match will then be used for display information. 
                 *Conflict: There can multiple scores with the same team ID as each individual gets a Score in the DB. So this will only pick up first score found
                 */
                foreach (AssignmentTeam team in teams)
                {
                    //Grabbing the submission time for the assignment
                    DateTime? subTime = GetSubmissionTime(team.Assignment.Category.Course, team.Assignment, team);
                    if (subTime != null)
                    {
                        submissionTime = subTime.Value.ToString();
                    }
                    else
                    {
                        submissionTime = "No Submission";
                    }

                    //Checking for matched score, if there is none - add a null entry
                    bool foundMatch = false;
                    foreach (Score score in scores)
                    {
                        if (score.TeamID == team.TeamID)
                        {
                            scoreAndTeam.Add(new Tuple<Score, Team, string>(score, team.Team, submissionTime));
                            foundMatch = true;
                            break;
                        }
                    }
                    if (!foundMatch)
                    {
                        Score junkScore = new Score() { Assignment = team.Assignment, AssignmentID = team.AssignmentID };
                        scoreAndTeam.Add(new Tuple<Score, Team, string>(junkScore, team.Team, submissionTime));
                    }
                }
                ViewBag.ScoresAndTeams = scoreAndTeam;

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

                ViewBag.RubricEvals = evaluations;
            }

            else if (activeCourse.AbstractRole.CanSubmit)//STudent setup
            {
                AssignmentTeam at = GetAssignmentTeam(assignment, currentUser);
                ViewBag.TeamID = at.TeamID;
                ViewBag.CurrentUserID = activeCourse.ID;
                ViewBag.TeamName = at.Team.Name;

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
        public void ModifyLatePenalty(int scoreId, string userIdentification, double latePenalty, int assignmentId)
        {
            if (scoreId > 0)
            {
                Score score = db.Scores.Find(scoreId);
                score.CustomLatePenaltyPercent = latePenalty;
                db.SaveChanges();
                new GradebookController().ModifyGrade(score.RawPoints, userIdentification, score.AssignmentID);
            }
            else if (scoreId == 0)
            {
                new GradebookController().ModifyGrade(-1, userIdentification, assignmentId);
                Score score = (from s in db.Scores
                               where s.CourseUser.UserProfile.Identification == userIdentification &&
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
    }
}

