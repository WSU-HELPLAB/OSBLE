﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Assignments.Activities.Scores;
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
            ViewBag.AbstractAssignmentActivityId = "abstractAssignmentActivityId";
            ViewBag.TeamUserId = "teamUserId";
            ViewBag.ActivitySelectId = "selected_activity";
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
            int abstractId = 0;
            int teamId = 0;
            Int32.TryParse(Request.Form[ViewBag.AbstractAssignmentActivityId].ToString(), out abstractId);
            Int32.TryParse(Request.Form[ViewBag.TeamUserId].ToString(), out teamId);

            //before we populate with FORM values, fill in the basic stuff
            if (abstractId > 0 && teamId > 0)
            {
                viewModel = GetRubricViewModel(abstractId, teamId);
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
            if (viewModel.Rubric == null || viewModel.AssignmentActivities == null || viewModel.AssignmentActivities.Count == 0)
            {
                return false;
            }

            //Make sure that the current activity is attached to the active course
            if (viewModel.SelectedAssignmentActivity.AbstractAssignment.Category.CourseID != activeCourse.AbstractCourseID)
            {
                return false;
            }
            return true;
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
                                     select e).FirstOrDefault();
            if (eval != null)
            {
                viewModel.Evaluation = eval;
            }
            else
            {
                //if nothing exists, we need to build a dummy eval for the view to process
                viewModel.Evaluation.Recipient = teamUser;
                viewModel.Evaluation.AbstractAssignmentActivityID = activity.ID;
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
            List<AbstractAssignmentActivity> activities = new List<AbstractAssignmentActivity>();
            foreach (Category cat in (activeCourse.AbstractCourse as Course).Categories)
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
                    PublishGrade(vm.Evaluation.AbstractAssignmentActivityID, vm.Evaluation.Recipient.ID);
                }
            }
            return View(vm);
        }

       

        [CanGradeCourse]
        public ActionResult Index(int abstractAssignmentActivityId, int teamUserId)
        {
            RubricViewModel viewModel = GetRubricViewModel(abstractAssignmentActivityId, teamUserId);

            if (!HasValidViewModel(viewModel))
            {
                return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
            }

            ViewBag.isEditable = true;
            return View(viewModel);
        }

        public ActionResult View(int abstractAssignmentActivityId, int teamUserId)
        {
            RubricViewModel viewModel = GetRubricViewModel(abstractAssignmentActivityId, teamUserId);

            if (HasValidViewModel(viewModel))
            {
                bool isOwnAssignment = false;

                if (activeCourse.AbstractRole.CanSubmit)
                {
                    TeamUserMember teamUser = db.TeamUsers.Find(teamUserId);

                    if (teamUser.Contains(currentUser))
                    {
                        isOwnAssignment = true;
                    }
                }

                if (activeCourse.AbstractRole.CanGrade || isOwnAssignment)
                {
                    AbstractAssignmentActivity activity = db.AbstractAssignmentActivities.Find(abstractAssignmentActivityId);

                    ViewBag.PossiblePoints = activity.PointsPossible;
                    ViewBag.Score = (from c in activity.Scores where c.TeamUserMemberID == teamUserId select c).FirstOrDefault();
                    ViewBag.isEditable = false;

                    return View(viewModel);
                }
            }
            return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
        }
    }
}
