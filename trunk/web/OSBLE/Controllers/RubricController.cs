using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;

using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    public class RubricController : OSBLEController
    {
        public RubricController()
        {
            ViewBag.CritInputPrefix = "crit_amount";
            ViewBag.CritSliderPrefix = "crit_slider";
            ViewBag.CritCommentPrefix = "crit_comment";
            ViewBag.AssignmentId = "AssignmentId";
            ViewBag.CourseUserId = "CourseUserId";
            ViewBag.TeamId = "TeamId";
            ViewBag.AssignmentSelectId = "selected_assignment";
            ViewBag.TeamSelectId = "selected_team";
            ViewBag.GlobalCommentId = ViewBag.CritCommentPrefix + "_global";
            ViewBag.DraftButtonId = "save_as_draft";
            ViewBag.PublishButtonId = "publish_to_student";
        }

        /// <summary>
        /// Builds a RubricViewModel that is based off of FORM values
        /// </summary>
        /// <returns></returns>
        private RubricViewModel BuildViewModelFromForm()
        {
            RubricViewModel viewModel = new RubricViewModel();
            int assignmentId = 0;
            int courseUserId = 0;
            int teamId = 0;
            Int32.TryParse(Request.Form[ViewBag.AssignmentId].ToString(), out assignmentId);
            Int32.TryParse(Request.Form[ViewBag.CourseUserId].ToString(), out courseUserId);
            Int32.TryParse(Request.Form[ViewBag.TeamId].ToString(), out teamId);

            //before we populate with FORM values, fill in the basic stuff
            if (assignmentId > 0 && courseUserId > 0 && teamId > 0)
            {
                viewModel = GetRubricViewModel(assignmentId, courseUserId);
            }

            //now, change the view model based on what we were passed through the form
            viewModel.Evaluation.RecipientID = teamId;
            foreach (CriterionEvaluation critEval in viewModel.Evaluation.CriterionEvaluations)
            {
                string critScoreKey = String.Format("{0}_{1}", ViewBag.CritInputPrefix, critEval.CriterionID);
                string critCommentKey = String.Format("{0}_{1}", ViewBag.CritCommentPrefix, critEval.CriterionID);
                if (Request.Form.AllKeys.Contains(critScoreKey))
                {
                    int score = 0;
                    Int32.TryParse(Request.Form[critScoreKey], out score);
                    critEval.Score = score;
                }
                if (Request.Form.AllKeys.Contains(critCommentKey))
                {
                    critEval.Comment = Request.Form[critCommentKey].ToString();
                }
            }

            //Set the published status based on what button the user selected.  Note
            //that once published, a review cannot be unpublished.  In this case,
            //the "save as draft" and "publish to student" buttons do the same thing.
            //In the future, it might be cool to have the "draft" button grayed out or
            //something - DONE. 
            string publishKey = ViewBag.PublishButtonId;
            if (Request.Form.AllKeys.Contains(publishKey))
            {
                viewModel.Evaluation.IsPublished = true;
                viewModel.Evaluation.DatePublished = DateTime.Now;
            }
            else 
            {
                //Even if the user has selected to save as draft, 
                //it should store the DatePublished to be displayed when the draft was last saved. 
                viewModel.Evaluation.IsPublished = false;
                viewModel.Evaluation.DatePublished = DateTime.Now;
            }


            string globalCommentKey = ViewBag.GlobalCommentId;
            if (Request.Form.AllKeys.Contains(globalCommentKey))
            {
                viewModel.Evaluation.GlobalComment = Request.Form[globalCommentKey].ToString();
            }

            return viewModel;
        }

        private bool HasValidViewModel(RubricViewModel viewModel)
        {
            //make sure that we found a rubric and that we have an activity to grade
            if (viewModel.Rubric == null || viewModel.AssignmentList == null || viewModel.AssignmentList.Count == 0)
            {
                return false;
            }

            //Make sure that the current activity is attached to the active course
            if (viewModel.SelectedAssignment.Category.CourseID != activeCourse.AbstractCourseID)
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Populates a RubricViewModel that is for an uneditable Rubric
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <returns></returns>
        private RubricViewModel GetUneditableRubricViewModel(int assignmentId)
        {
            RubricViewModel viewModel = new RubricViewModel();
            Assignment assignment = db.Assignments.Find(assignmentId);

            if (assignment == null)
            {
                return viewModel;
            }

            //assigns the rubric to our view model
            Rubric rubric = assignment.Rubric;
            viewModel.Rubric = rubric;
            viewModel.SelectedAssignment = assignment;

            //if nothing exists, we need to build a dummy eval for the view to process
            viewModel.Evaluation.AssignmentID = assignment.ID;
            viewModel.Evaluation.EvaluatorID = CurrentUser.ID;
            foreach (Criterion crit in rubric.Criteria)
            {
                CriterionEvaluation critEval = new CriterionEvaluation();
                critEval.Criterion = crit;
                critEval.CriterionID = crit.ID;
                critEval.Score = 0;
                critEval.Comment = "";
                viewModel.Evaluation.CriterionEvaluations.Add(critEval);
            }
            return viewModel;
        }

        /// <summary>
       /// Populates a generic RubricViewModel based on the supplied parameters
       /// </summary>
       /// <param name="abstractAssignmentActivityId"></param>
       /// <param name="teamUserId"></param>
       /// <returns></returns>
        private RubricViewModel GetRubricViewModel(int assignmentId, int cuId)
        {
            CourseUser cu = db.CourseUsers.Find(cuId);
            RubricViewModel viewModel = new RubricViewModel();

            Assignment assignment = db.Assignments.Find(assignmentId);

            AssignmentTeam assignmentTeam = GetAssignmentTeam(assignment, cu.UserProfile);

            if (assignment == null || assignmentTeam == null)
            {
                return viewModel;
            }

            //assigns the rubric to our view model
            Rubric rubric = assignment.Rubric;
            viewModel.Rubric = rubric;
            viewModel.SelectedAssignment = assignment;
            viewModel.SelectedTeam = assignmentTeam;

            //pull a prior evaluation if it exists
            RubricEvaluation eval = (from e in db.RubricEvaluations
                                     where e.RecipientID == assignmentTeam.TeamID
                                     select e).FirstOrDefault();
            if (eval != null)
            {
                viewModel.Evaluation = eval;
            }
            else
            {
                //if nothing exists, we need to build a dummy eval for the view to process
                viewModel.Evaluation.Recipient = assignmentTeam.Team;
                viewModel.Evaluation.AssignmentID = assignment.ID;
                viewModel.Evaluation.EvaluatorID = CurrentUser.ID;
                foreach (Criterion crit in rubric.Criteria)
                {
                    CriterionEvaluation critEval = new CriterionEvaluation();
                    critEval.Criterion = crit;
                    critEval.CriterionID = crit.ID;
                    critEval.Score = 0;
                    critEval.Comment = "";
                    viewModel.Evaluation.CriterionEvaluations.Add(critEval);
                }
            }

            //assignments are storied within categories, which are found within
            //the active course.
            List<Assignment> rubricAssignmentList = new List<Assignment>();
            foreach (Category cat in (activeCourse.AbstractCourse as Course).Categories)
            {
                foreach (Assignment a in cat.Assignments)
                {
                    if (a.HasRubric)
                    {
                        rubricAssignmentList.Add(a);
                    }
                }
            }

            viewModel.AssignmentList = rubricAssignmentList;
            viewModel.TeamList = assignment.AssignmentTeams.OrderBy(t => t.Team.Name).ToList();
            return viewModel;
        }

        [CanGradeCourse]
        [HttpPost]
        public ActionResult Index(RubricViewModel viewModel)
        {
            double latePenalty = 0.0;
            RubricViewModel vm = BuildViewModelFromForm();

            ViewBag.isEditable = true;

            if (!HasValidViewModel(vm))
            {
                return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
            }

            //if we've gotten this far, then it's probably okay to save to the DB
            if (ModelState.IsValid)
            {
                //save to the rubric evaluations table
                if (vm.Evaluation.ID != 0)
                {
                    db.Entry(vm.Evaluation).State = EntityState.Modified;
                }
                else
                {
                    db.RubricEvaluations.Add(vm.Evaluation);
                }
                db.SaveChanges();

                //if the evaluation has been published, update the scores in the gradebook
                if (vm.Evaluation.IsPublished)
                {
                    (new NotificationController()).SendRubricEvaluationCompletedNotification(vm.Evaluation.Assignment, vm.Evaluation.Recipient);
                    GradebookController gradebook = new GradebookController();

                    //figure out the normalized final score.
                    double maxLevelScore = (from c in vm.Rubric.Levels
                                            select c.RangeEnd).Sum();
                    double totalRubricPoints = (from c in vm.Rubric.Criteria
                                                select c.Weight).Sum();
                    double studentScore = 0.0;

                    foreach (CriterionEvaluation critEval in vm.Evaluation.CriterionEvaluations)
                    {
                        studentScore += (double)critEval.Score / maxLevelScore * (critEval.Criterion.Weight / totalRubricPoints);
                    }

                    //normalize the score with the assignment score
                    studentScore *= vm.Evaluation.Assignment.PointsPossible;

                    gradebook.ModifyTeamGrade(studentScore, vm.SelectedAssignment.ID, vm.Evaluation.Recipient.ID);


                }
            }
            return View(vm);
        }

        [CanGradeCourse]
        public ActionResult Index(int assignmentId, int cuId)
        {
            RubricViewModel viewModel = GetRubricViewModel(assignmentId, cuId);

            if (!HasValidViewModel(viewModel))
            {
                return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
            }

            CourseUser cu = db.CourseUsers.Find(cuId);
            if (cu.AbstractRole.Anonymized)
            {
                ViewBag.ObserverCU = db.CourseUsers.Where(c => c.AbstractCourseID == cu.AbstractCourseID).ToList();
                ViewBag.isEditable = false;
            }
            else
            {
                ViewBag.isEditable = true;
            }
            return View(viewModel);
        }


        public ActionResult View(int assignmentId, int cuId)
        {
            RubricViewModel viewModel = GetRubricViewModel(assignmentId, cuId);

            if (HasValidViewModel(viewModel))
            {
                bool isOwnAssignment = false;

                if (activeCourse.AbstractRole.CanSubmit)
                {
                    
                    Assignment assignment = db.Assignments.Find(assignmentId);
                    AssignmentTeam team = GetAssignmentTeam(assignment, currentUser);
                    if (team != null)
                    { 
                        isOwnAssignment = true;
                    }
                }

                if (activeCourse.AbstractRole.CanGrade || isOwnAssignment || activeCourse.AbstractRole.Anonymized)
                {
                    Assignment assignment = db.Assignments.Find(assignmentId);
                    CourseUser cu = db.CourseUsers.Find(cuId);
                    AssignmentTeam at = GetAssignmentTeam(assignment, cu.UserProfile);
                    ViewBag.AssignmentName = assignment.AssignmentName;
                    ViewBag.PossiblePoints = assignment.PointsPossible;
                    ViewBag.Score = (from c in assignment.Scores where c.CourseUserID == cuId select c).FirstOrDefault();
                    if (ViewBag.Score != null && ViewBag.Score.Points == -1) //If the score is currently a NG, dont display score (doing this by giving score a null value, handled by view)
                        ViewBag.Score = null;
                    ViewBag.isEditable = false;

                    return View(viewModel);
                }
            }
            return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
        }


        public ActionResult ViewAsUneditable(int assignmentId)
        {
            RubricViewModel viewModel = GetUneditableRubricViewModel(assignmentId);
            if (!(viewModel.Rubric == null || viewModel.SelectedAssignment.Category.CourseID != activeCourse.AbstractCourseID))
            {
                ViewBag.Score = null;
                ViewBag.isEditable = false;
                return View("View", viewModel);
            }
            return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
        }
    }
}
