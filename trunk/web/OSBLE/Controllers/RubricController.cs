using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models;
using OSBLE.Models.ViewModels;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.Assignments;
using OSBLE.Models.Users;
using OSBLE.Models.Assignments.Activities;
using System.Web.Routing;

namespace OSBLE.Controllers
{ 
    public class RubricController : OSBLEController
    {
        public RubricController()
        {
            ViewBag.CritInputPrefix = "crit_amount";
            ViewBag.CritSliderPrefix = "crit_slider";
            ViewBag.CritCommentPrefix = "crit_comment";
            ViewBag.AbstractAssignmentActivityId = "abstractAssignmentActivityId";
            ViewBag.TeamUserId = "teamUserId";
            ViewBag.ActivitySelectId = "selected_activity";
            ViewBag.TeamSelectId = "selected_team";
            ViewBag.GlobalCommentId = ViewBag.CritCommentPrefix + "_global";
        }

        /// <summary>
        /// Builds a RubricViewModel that is based off of FORM values
        /// </summary>
        /// <returns></returns>
        private RubricViewModel BuildViewModelFromForm()
        {
            RubricViewModel viewModel = new RubricViewModel();
            int abstractId = 0;
            int teamId = 0;
            Int32.TryParse(Request.Form[ViewBag.AbstractAssignmentActivityId].ToString(), out abstractId);
            Int32.TryParse(Request.Form[ViewBag.TeamUserId].ToString(), out teamId);

            //The user has the possiblility of changing the currently selected activity and
            //team.  When this happens, we don't want to associate any current data
            //with this new evaluation.
            int otherActivityId = 0;
            int otherTeamId = 0;
            Int32.TryParse(Request.Form[ViewBag.ActivitySelectId].ToString(), out otherActivityId);
            Int32.TryParse(Request.Form[ViewBag.TeamSelectId].ToString(), out otherTeamId);

            if (abstractId != otherActivityId || teamId != otherTeamId)
            {
                viewModel = GetRubricViewModel(otherActivityId, otherTeamId);

                //return without filling in any other form data
                return viewModel;
            }

            //before we populate with FORM values, fill in the basic stuff
            if (abstractId > 0 && teamId > 0)
            {
                viewModel = GetRubricViewModel(abstractId, teamId);
            }

            //now, change the view model based on what we were passed through the form
            foreach (CriterionEvaluation critEval in viewModel.Evaluation.CriterionEvaluations)
            {
                string critScoreKey = String.Format("{0}_{1}", ViewBag.CritInputPrefix, critEval.Criterion.ID);
                string critCommentKey = String.Format("{0}_{1}", ViewBag.CritCommentPrefix, critEval.Criterion.ID);
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

            string globalCommentKey = ViewBag.GlobalCommentId;
            if (Request.Form.AllKeys.Contains(globalCommentKey))
            {
                viewModel.Evaluation.GlobalComment = Request.Form[globalCommentKey].ToString();
            }
            
            return viewModel;
        }

        /// <summary>
        /// Populates a generic RubricViewModel based on the supplied parameters
        /// </summary>
        /// <param name="abstractAssignmentActivityId"></param>
        /// <param name="teamUserId"></param>
        /// <returns></returns>
        private RubricViewModel GetRubricViewModel(int abstractAssignmentActivityId, int teamUserId)
        {
            RubricViewModel viewModel = new RubricViewModel();

            AbstractAssignmentActivity activity = (from aa in db.AbstractAssignmentActivities
                                                   where aa.ID == abstractAssignmentActivityId
                                                   select aa).FirstOrDefault();
            TeamUserMember teamUser = (from tu in db.TeamUsers
                                       where tu.ID == teamUserId
                                       select tu).FirstOrDefault();
            if (activity == null || teamUser == null)
            {
                return viewModel;
            }

            //assigns the rubric to our view model
            Rubric rubric = activity.AbstractAssignment.Rubric;
            viewModel.Rubric = rubric;
            viewModel.SelectedAssignmentActivity = activity;
            viewModel.SelectedTeam = teamUser;

            //pull a prior evaluation if it exists
            RubricEvaluation eval = (from e in db.RubricEvaluations
                                     where e.RecipientID == teamUserId
                                     &&
                                     e.EvaluatorID == currentUser.ID
                                     select e).FirstOrDefault();
            if (eval != null)
            {
                viewModel.Evaluation = eval;
            }
            else
            {
                //if nothing exists, we need to build a dummy eval for the view to process
                viewModel.Evaluation.Recipient = teamUser;
                foreach (Criterion crit in rubric.Criteria)
                {
                    CriterionEvaluation critEval = new CriterionEvaluation();
                    critEval.Criterion = crit;
                    critEval.Score = 0;
                    critEval.Comment = "";
                    viewModel.Evaluation.CriterionEvaluations.Add(critEval);
                }
            }

            //assignments are storied within categories, which are found within
            //the active course.  
            List<AbstractAssignmentActivity> activities = new List<AbstractAssignmentActivity>();
            foreach (Category cat in (activeCourse.Course as Course).Categories)
            {
                foreach (AbstractAssignment assignment in cat.Assignments)
                {
                    List<SubmissionActivity> submissions = (from aa in assignment.AssignmentActivities
                                                            where aa is SubmissionActivity
                                                            select aa as SubmissionActivity).ToList();
                    foreach (SubmissionActivity submission in submissions)
                    {
                        activities.Add(submission);
                    }
                }
            }
            viewModel.AssignmentActivities = activities;
            viewModel.TeamUsers = activity.TeamUsers.OrderBy(t => t.Name).ToList();
            return viewModel;
        }

        [CanGradeCourse]
        [HttpPost]
        public ActionResult Index(RubricViewModel viewModel)
        {
            RubricViewModel vm = BuildViewModelFromForm();
            return View(vm);
        }

        [CanGradeCourse]
        public ActionResult Index(int abstractAssignmentActivityId, int teamUserId)
        {
            RubricViewModel viewModel = GetRubricViewModel(abstractAssignmentActivityId, teamUserId);

            //make sure that we found a rubric and that we have an activity to grade
            if (viewModel.Rubric == null || viewModel.AssignmentActivities.Count == 0)
            {
                return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
            }

            //Make sure that the current activity is attached to the active course
            if (viewModel.SelectedAssignmentActivity.AbstractAssignment.Category.CourseID != activeCourse.CourseID)
            {
                return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
            }

            return View(viewModel);
        }
    }
}