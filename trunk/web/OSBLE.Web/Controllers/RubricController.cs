using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;

using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.ViewModels;
using System.Web;
using System.IO;
using OSBLE.Models.Users;

namespace OSBLE.Controllers
{
    [OsbleAuthorize]
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
            ViewBag.StudentStatusName = "StudentStatus";
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

            bool student;
            bool.TryParse(Request.Form[ViewBag.StudentStatusName].ToString(), out student);
            viewModel.Student = student;

            //before we populate with FORM values, fill in the basic stuff
            if (assignmentId > 0 && courseUserId > 0 && teamId > 0)
            {
                if (student)
                {
                    viewModel = GetStudentRubricViewModel(assignmentId, courseUserId, ActiveCourseUser.ID);
                }
                else
                {
                    viewModel = GetRubricViewModel(assignmentId, courseUserId);
                    //now, change the view model based on what we were passed through the form
                    viewModel.Evaluation.RecipientID = teamId;
                }
            }

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
                viewModel.Evaluation.DatePublished = DateTime.UtcNow;
            }
            else
            {
                //Even if the user has selected to save as draft,
                //it should store the DatePublished to be displayed when the draft was last saved.
                viewModel.Evaluation.IsPublished = false;
                viewModel.Evaluation.DatePublished = DateTime.UtcNow;
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
            if (viewModel.Student)
            {
                return true;
            }
            //make sure that we found a rubric and that we have an activity to grade
            if (viewModel.Rubric == null || viewModel.AssignmentList == null || viewModel.AssignmentList.Count == 0)
            {
                return false;
            }

            //Make sure that the current activity is attached to the active course
            if (viewModel.SelectedAssignment.CourseID != ActiveCourseUser.AbstractCourseID)
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
        private RubricViewModel GetUneditableRubricViewModel(int assignmentId, bool student = false)
        {
            RubricViewModel viewModel = new RubricViewModel();
            Assignment assignment = db.Assignments.Find(assignmentId);

            if (assignment == null)
            {
                return viewModel;
            }
            Rubric rubric;
            //assigns the rubric to our view model
            if (student)
            {
                rubric = assignment.StudentRubric;
            }
            else
            {
                rubric = assignment.Rubric;
            }
            viewModel.Student = student;
            viewModel.Rubric = rubric;
            viewModel.SelectedAssignment = assignment;

            //if nothing exists, we need to build a dummy eval for the view to process
            viewModel.Evaluation.AssignmentID = assignment.ID;
            viewModel.Evaluation.EvaluatorID = ActiveCourseUser.ID;
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
        /// Note: for student rubrics, use getStudentRubricViewModel
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="cuId">Course User ID of the author</param>
        /// <returns></returns>
        private RubricViewModel GetRubricViewModel(int assignmentId, int cuId)
        {
            CourseUser cu = db.CourseUsers.Find(cuId);
            RubricViewModel viewModel = new RubricViewModel();

            Assignment assignment = db.Assignments.Find(assignmentId);

            AssignmentTeam assignmentTeam;
            viewModel.Student = false;

            viewModel.SelectedAssignment = assignment;

            assignmentTeam = GetAssignmentTeam(assignment, cu);
            Rubric rubric = assignment.Rubric;

            if (assignment == null || assignmentTeam == null)
            {
                return viewModel;
            }
            viewModel.Rubric = rubric;

            viewModel.SelectedTeam = assignmentTeam;

            //pull a prior evaluation if it exists
            RubricEvaluation eval = (from e in db.RubricEvaluations
                    where e.RecipientID == assignmentTeam.TeamID
                    select e).FirstOrDefault();
            
            if (eval != null)
            {
                viewModel.Evaluation = eval;
            }
            else //if nothing exists, we need to build a dummy eval for the view to process
            {
                viewModel.Evaluation.Recipient = assignmentTeam.Team;
                
                viewModel.Evaluation.AssignmentID = assignment.ID;
                viewModel.Evaluation.EvaluatorID = ActiveCourseUser.ID;
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

            //assignments are stored within categories, which are found within
            //the active course.
            List<Assignment> rubricAssignmentList = (from a in db.Assignments
                                                     where a.CourseID == assignment.CourseID &&
                                                     a.Rubric != null
                                                     select a).ToList();

            viewModel.AssignmentList = rubricAssignmentList;
            //viewModel.RubricEvaluationList = (from re in db.RubricEvaluations where re.AssignmentID == assignment.ID select re).ToList();
            if (assignment.HasTeams)
            {
                viewModel.TeamList = assignment.AssignmentTeams.OrderBy(t => t.Team.Name).ToList();
            }
            else
            {
                viewModel.TeamList = assignment.AssignmentTeams.OrderBy(l => l.Team.TeamMembers.FirstOrDefault().CourseUser.UserProfile.LastName).ThenBy(f => f.Team.TeamMembers.FirstOrDefault().CourseUser.UserProfile.FirstName).ToList();
            }

            viewModel.AssignmentList = rubricAssignmentList;
            if (assignment.HasTeams)
            {
                viewModel.TeamList = assignment.AssignmentTeams.OrderBy(t => t.Team.Name).ToList();
            }
            else
            {
                viewModel.TeamList = assignment.AssignmentTeams.OrderBy(t => t.Team.TeamMembers.FirstOrDefault().CourseUser.DisplayName(ActiveCourseUser.AbstractRole)).ToList();
            }
            viewModel.RubricEvaluationList = (from r in db.RubricEvaluations
                                              where r.AssignmentID == assignment.ID
                                              select r).ToList();
            return viewModel;
        }

        /// <summary>
        /// Only used for student rubrics. 
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="authorCuId">ID of the author (person being reviewed) Note: if it's a team, this can be the
        /// ID of any member of the team</param>
        /// <param name="reviewerCuId">CourseUser ID of the reviewer Note: if it's a team, this can be the
        /// ID of any member of the team</param>
        /// <returns></returns>
        private RubricViewModel GetStudentRubricViewModel(int assignmentId, int authorCuId, int reviewerCuId)
        {
            //Id of author
            CourseUser author = db.CourseUsers.Find(authorCuId);
            CourseUser reviewer = db.CourseUsers.Find(reviewerCuId);
            RubricViewModel viewModel = new RubricViewModel();

            Assignment assignment = db.Assignments.Find(assignmentId);
            if (assignment == null)
            {
                return viewModel;
            }

            viewModel.SelectedAssignment = assignment;
            viewModel.Student = true;

            //author team
            viewModel.SelectedTeam = GetAssignmentTeam(assignment.PreceedingAssignment, author);
            int authorTeamId = viewModel.SelectedTeam.TeamID;

            AssignmentTeam reviewingTeam = GetAssignmentTeam(assignment, reviewer);

            Rubric rubric = assignment.StudentRubric;
            viewModel.Rubric = rubric;

            //get Ids of all members of the team doing the review. Use this list to make sure that the current 
            //user is on this team
            List<int> ReviewingTeamCUIDs = reviewingTeam.Team.TeamMembers.Select(tm => tm.CourseUserID).ToList();

            //get the rubric evaluation where 
                //the recipientID matches the the author team and
                //the evaluator ID (CourseUserID) exists in the team that is doing the review
            RubricEvaluation eval = (from e in db.RubricEvaluations
                    where e.RecipientID == authorTeamId &&
                    ReviewingTeamCUIDs.Contains(e.EvaluatorID)
                    select e).FirstOrDefault();

            if (eval != null)
            {
                viewModel.Completed = true;
                viewModel.Evaluation = eval;
            }
            else
            {
                viewModel.Evaluation.Recipient = db.Teams.Find(authorTeamId);
                viewModel.Evaluation.AssignmentID = assignment.ID;
                viewModel.Evaluation.EvaluatorID = ActiveCourseUser.ID;
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

            return viewModel;
        }

        [HttpPost]
        public ActionResult Index()
        {
            RubricViewModel vm = BuildViewModelFromForm();

            ViewBag.isEditable = true;

            if (!HasValidViewModel(vm))
            {
                return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index", area = "" }));
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

                //if the evaluation has been published, send notification to student
                if (vm.Evaluation.IsPublished)
                {
                   (new NotificationController()).SendRubricEvaluationCompletedNotification(vm.Evaluation.Assignment, vm.Evaluation.Recipient);
                }
            }
            return View(vm);
        }

        /// <summary>
        /// This index is only used for the instructor's version of the rubric view.
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="cuId">Course User ID of the author</param>
        /// <returns></returns>
        [CanGradeCourse]
        public ActionResult Index(int assignmentId, int cuId)
        {
            return IndexHelper(GetRubricViewModel(assignmentId, cuId));
        }

        /// <summary>
        /// this creates the student's version of the rubric view
        /// get the rubric that current user (cuId) performed on the authorTeam. This passed activeCourseUserID
        /// as the reviewID since the active course user should only ever fill out a rubric as themselves.
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="cuId">ID of the author (person being reviewed) Note: if it's a team, this is the ID
        /// of the person who submitted the assignment</param>
        [CanSubmitAssignments]
        public ActionResult StudentIndex(int assignmentId, int cuId)
        {
            return IndexHelper(GetStudentRubricViewModel(assignmentId, cuId, ActiveCourseUser.ID));
        }

        // Perform actions that are the same for both Index and StudentIndex
        private ActionResult IndexHelper(RubricViewModel viewModel)
        {
            if (!HasValidViewModel(viewModel))
            {
                return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index", area = "" }));
            }

            ViewBag.isEditable = true;

            return View("Index", viewModel);
        }

        /// <summary>
        /// Called from CriticalReviewStudentDownloadDecorator. This function creates a merged RubricViewModel
        /// that consists of all rubric evaluations that have been done at 
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="authorTeamId"></param>
        /// <returns></returns>
        public ActionResult ViewForCriticalReview(int assignmentId, int authorTeamId)
        {
            RubricViewModel mergedRubric = new RubricViewModel();
            Team authorTeam = db.Teams.Find(authorTeamId);

            //Get a view model for each rubric in the assignment that was done on the author
            List<RubricViewModel> rubricViewModels = new List<RubricViewModel>();

            //compile a list of all of the reviewerIDs (Course User IDs) who were assigned to review authorTeam's assignment. 
            //some of these may not have actually submitted their review.
            List<Team> reviewingTeams = (from rt in db.ReviewTeams
                                     where rt.AuthorTeamID == authorTeamId
                                     && rt.AssignmentID == assignmentId
                                   select rt.ReviewingTeam).ToList();

            List<int> reviewerIds = reviewingTeams.Select(rt => rt.TeamMembers.FirstOrDefault().CourseUserID).ToList();


            foreach (int reviewId in reviewerIds)
            {
                RubricViewModel rvm = GetStudentRubricViewModel(assignmentId,
                    authorTeam.TeamMembers.FirstOrDefault().CourseUserID,
                    reviewId);
                if (rvm.Completed)
                {
                    rubricViewModels.Add(rvm);
                }
            }

            if (rubricViewModels.Count > 0)
            {
                mergedRubric = MergeRubricViewModels(rubricViewModels);
            }

            if (HasValidViewModel(mergedRubric))
            {
                bool isOwnAssignment = false;

                if (ActiveCourseUser.AbstractRole.CanSubmit)
                {
                    Assignment assignment = db.Assignments.Find(assignmentId);
                    AssignmentTeam team = GetAssignmentTeam(assignment, ActiveCourseUser);
                    if (team != null)
                    {
                        isOwnAssignment = true;
                    }
                }

                if (ActiveCourseUser.AbstractRole.CanGrade || isOwnAssignment || ActiveCourseUser.AbstractRole.Anonymized)
                {
                    Assignment assignment = db.Assignments.Find(assignmentId);
                    
                    CourseUser cu = db.CourseUsers.Find(authorTeam.TeamMembers.FirstOrDefault().CourseUserID);
                    AssignmentTeam at = GetAssignmentTeam(assignment, cu);
                    ViewBag.AssignmentName = assignment.AssignmentName;
                    ViewBag.DisplayGrade = false;
                    if (mergedRubric.MergedEvaluations.Count > 0)
                    {
                        ViewBag.DisplayGrade = true;
                        //ViewBag.Grade = RubricEvaluation.GetGradeAsPercent(mergedRubric.Evaluation.ID);

                        List<double> grades = rubricViewModels.Select(rvm => RubricEvaluation.GetStudentGradeAsDouble(rvm.Evaluation.ID)).ToList();
                        ViewBag.Grade = (grades.Sum() / rubricViewModels.Count).ToString("P");
                    }
                    ViewBag.isEditable = false;

                    return View(mergedRubric);
                }
            }
            return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index", area = "" }));
        }

        /// <summary>
        /// Helper function that merges a list of rubric viewmodels into one. The merged RubricViewModel
        /// contains a list of rubricEvaluations (one for each original) stored in MergedEvaluations. the IsMerged
        /// Property will also be true.
        /// </summary>
        /// <param name="rubricViewModels">List of viewModels to be merged</param>
        /// <returns>Returns the merged result.</returns>
        private RubricViewModel MergeRubricViewModels(List<RubricViewModel> rubricViewModels)
        {
            RubricViewModel mergedViewModel = new RubricViewModel();

            if (rubricViewModels.Count < 1)
            {
                //need to create a nice empty one here to handle this case
            }

            mergedViewModel.isMerged = true;
            mergedViewModel.Student = true;

            //assign things that are the same between rubrics 
            RubricViewModel temp = rubricViewModels.FirstOrDefault();
            mergedViewModel.Rubric = temp.Rubric;
            mergedViewModel.SelectedTeam = temp.SelectedTeam;
            //mergedViewModel.Evaluation.Recipient = temp.Evaluation.Recipient;
            mergedViewModel.Evaluation.AssignmentID = temp.Evaluation.AssignmentID;
            mergedViewModel.Evaluation.EvaluatorID = temp.Evaluation.EvaluatorID;

            foreach (RubricViewModel rvm in rubricViewModels)
            {
                mergedViewModel.MergedEvaluations.Add(rvm.Evaluation);
            }

            if (mergedViewModel.MergedEvaluations.Count > 1)
            {
                List<double?> averageScores = new List<double?>();
                for (int i = 0; i < mergedViewModel.MergedEvaluations.FirstOrDefault().CriterionEvaluations.Count; i++)
                {
                    List<int?> scoreList = mergedViewModel.MergedEvaluations.Select(e => e.CriterionEvaluations.ElementAt(i).Score).ToList();
                    double? average = scoreList.Sum() / (double)scoreList.Count;
                    averageScores.Add(average);
                }

                ViewBag.averageScores = averageScores;
            }

            return mergedViewModel;
        }

        public ActionResult View(int assignmentId, int cuId)
        {
            RubricViewModel viewModel = GetRubricViewModel(assignmentId, cuId);

            if (HasValidViewModel(viewModel))
            {
                bool isOwnAssignment = false;

                if (ActiveCourseUser.AbstractRole.CanSubmit && ActiveCourseUser.ID == cuId)
                {
                    Assignment assignment = db.Assignments.Find(assignmentId);
                    AssignmentTeam team = GetAssignmentTeam(assignment, ActiveCourseUser);
                    if (team != null)
                    {
                        isOwnAssignment = true;
                    }
                }
                 
                if (ActiveCourseUser.AbstractRole.CanGrade || isOwnAssignment || ActiveCourseUser.AbstractRole.Anonymized)
                {
                    Assignment assignment = db.Assignments.Find(assignmentId);
                    CourseUser cu = db.CourseUsers.Find(cuId);
                    AssignmentTeam at = GetAssignmentTeam(assignment, cu);
                    ViewBag.AssignmentName = assignment.AssignmentName;
                    ViewBag.DisplayGrade = false;
                    if (viewModel.Evaluation.CriterionEvaluations.Count > 0)
                    {
                        ViewBag.DisplayGrade = true;
                        ViewBag.Grade = RubricEvaluation.GetGradeAsPercent(viewModel.Evaluation.ID);
                    }
                    ViewBag.isEditable = false;

                    return View(viewModel);
                }
            }
            return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index", area = "" }));
        }

        public ActionResult ViewAsUneditable(int assignmentId)
        {
            RubricViewModel viewModel = GetUneditableRubricViewModel(assignmentId);
            if (!(viewModel.Rubric == null || viewModel.SelectedAssignment.CourseID != ActiveCourseUser.AbstractCourseID))
            {
                Assignment assignment = db.Assignments.Find(assignmentId);
                ViewBag.Score = null;
                ViewBag.AssignmentName = assignment.AssignmentName;
                ViewBag.isEditable = false;
                ViewBag.DisplayGrade = false;
                return View("View", viewModel);
            }
            return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index", area = "" }));
        }

        [HttpPost]
        [CanModifyCourse]
        public ActionResult ImportIndividualToCSV(HttpPostedFileBase file)
        {
            List<string> parsedData = new List<string>();
            StreamReader readFile = new StreamReader(file.InputStream);
            string assignmentName = "";
            string courseName = "";
            string studentNames = "";
            string line;
            RubricViewModel rvm = new RubricViewModel();

            while ((line = readFile.ReadLine()) != null)
            {
                if (!line.StartsWith("---") && line.Trim() != "")
                {
                    parsedData.Add(line);
                }
            }
            while (!parsedData.ElementAt(0).StartsWith("***"))
            {
                parsedData.RemoveAt(0);
            }
            assignmentName = parsedData.ElementAt(0).Split(':')[1].Trim();
            parsedData.RemoveAt(0);

            Assignment curAssignment = (from a in db.Assignments
                                         where a.AssignmentName == assignmentName
                                         select a).FirstOrDefault();

            if(curAssignment != null)
            {
                courseName = parsedData.ElementAt(0).Split(':')[1].Split('(')[0].Trim();
                parsedData.RemoveAt(0);

                if(ActiveCourseUser.AbstractCourse.Name == courseName)
                {
                    studentNames = parsedData.ElementAt(0).Split(':')[1].Trim();
                    parsedData.RemoveAt(0);
                    Team team = (from t in db.Teams
                                 where t.Name == studentNames
                                 select t).FirstOrDefault();
                    

                    if (team != null)
                    {
                        int userID = (from u in team.TeamMembers
                                      where u.Team.Name == studentNames
                                      select u.CourseUserID).FirstOrDefault();

                        RubricEvaluation rubricEvaluation = (from re in db.RubricEvaluations
                                                             where re.Assignment.ID == curAssignment.ID
                                                             && re.RecipientID == team.ID
                                                             select re).FirstOrDefault();

                        if (rubricEvaluation == null)
                        {
                            rubricEvaluation = new RubricEvaluation();
                            Rubric rubric = db.Rubrics.Find(curAssignment.Rubric.ID);

                            rubricEvaluation.AssignmentID = curAssignment.ID;
                            rubricEvaluation.EvaluatorID = ActiveCourseUser.ID;
                            rubricEvaluation.RecipientID = team.ID;

                            foreach(Criterion c in rubric.Criteria)
                            {
                                CriterionEvaluation criterionEvaluation = new CriterionEvaluation();
                                criterionEvaluation.CriterionID = c.ID;
                                rubricEvaluation.CriterionEvaluations.Add(criterionEvaluation);
                            }
                            db.RubricEvaluations.Add(rubricEvaluation);
                            db.SaveChanges();
                        }

                        //int i = 0;
                        foreach (CriterionEvaluation ce in rubricEvaluation.CriterionEvaluations)
                        {
                            if (parsedData.FirstOrDefault().Split('(')[0].Substring(4).Trim() == ce.Criterion.CriterionTitle)
                            {
                                // removing the criterion title row
                                parsedData.RemoveAt(0);
                                // Removing the points title row
                                parsedData.RemoveAt(0);
                                // Makeing sure the points field was populated
                                if(!parsedData.ElementAt(0).StartsWith("***"))
                                {
                                    ce.Score = Convert.ToInt32(parsedData.ElementAt(0));
                                    // removing the points row after getting the value
                                    parsedData.RemoveAt(0);
                                }

                                // will need to change this to handle merging
                                ce.Comment = "";
                                parsedData.RemoveAt(0);

                                // Makeing sure the user actually entered comments
                                while(parsedData.Count != 0 && !parsedData.ElementAt(0).StartsWith("***"))
                                {
                                    ce.Comment += parsedData.ElementAt(0);
                                    // removing the comments string from the list
                                    parsedData.RemoveAt(0);
                                }
                            }
                        }

                        if (curAssignment.Rubric.HasGlobalComments)
                        {
                            if (parsedData.FirstOrDefault().StartsWith("*** General Comments:"))
                            {
                                // remove the general comments header
                                parsedData.RemoveAt(0);
                                if (!parsedData.ElementAt(0).StartsWith("***"))
                                {
                                    rubricEvaluation.GlobalComment = parsedData.ElementAt(0);
                                    // Remove the general comments for the list
                                    parsedData.RemoveAt(0);
                                }
                            }
                        }
                        db.SaveChanges();

                        return RedirectToAction("Index", "Rubric", new { assignmentId = curAssignment.ID, cuId = userID });
                    }
                }
            }
            
            //return RedirectToRoute(new RouteValueDictionary(new { controller = "Home", action = "Index" }));
            ViewBag.Error = "Could Not import the rubric as the formatting is not correct!\n";
            return View("Index", "Assignment");

        }

        [CanModifyCourse]
        public ActionResult ImportAllToCSV(HttpPostedFileBase file)
        {
            return View();
        }

        [CanModifyCourse]
        public ActionResult ExportIndividualToCSV(int teamID, int assignID)
        {
            string completeRubricString = "";
            AbstractCourse currentCourse = ActiveCourseUser.AbstractCourse;
            Team team = db.Teams.Find(teamID);
            Assignment curAssignment = (from a in db.Assignments
                                        where a.ID == assignID
                                        select a).FirstOrDefault();
            
            completeRubricString = "*** ASSIGNMENT: " + curAssignment.AssignmentName + "\n";
            completeRubricString += "*** COURSE: " + currentCourse.Name + "(" + (currentCourse as Course).Semester + ", " + (currentCourse as Course).Year + ")\n";
            completeRubricString += "\n---Please use this file to complete rubric evaluations for this\n";
            completeRubricString += "---assignment\n";
            completeRubricString += "----   *** are uneditable lines / --- are comment lines\n\n";
            completeRubricString += "--------------------------------------------------------\n";

            completeRubricString += parseRubricToString(team, curAssignment);

            ViewBag.CompleteRubricString = completeRubricString;


            context.Response.AppendHeader("Content-Disposition", "attachment; filename=\"" + currentCourse.Name + "_" + curAssignment.AssignmentName + "_" + team.DisplayName(ActiveCourseUser.AbstractRole) + " rubric.txt\"");
            Response.ContentType = "application/octet-stream";

            return View();
        }

        [CanModifyCourse]
        public ActionResult ExportAllToCSV(int assignID)
        {
            string completeRubricString = "";
            AbstractCourse currentCourse = ActiveCourseUser.AbstractCourse;
            Assignment curAssignment = (from a in db.Assignments
                                        where a.ID == assignID
                                        select a).FirstOrDefault();

            List<AssignmentTeam> assignTeamsList = (from at in db.AssignmentTeams
                                                    where at.AssignmentID == assignID
                                                    select at).ToList();
            
            completeRubricString =  "*** ASSIGNMENT: " + curAssignment.AssignmentName + "\n";
            completeRubricString += "*** COURSE:     " + currentCourse.Name + "(" + (currentCourse as Course).Semester +", " + (currentCourse as Course).Year + ")\n";
            completeRubricString += "\n---Please use this file to complete rubric evaluations for this\n";
            completeRubricString += "---assignment\n";
            completeRubricString += "*** are uneditable lines / --- are comment lines\n\n";
            completeRubricString += "--------------------------------------------------------\n";


            foreach (AssignmentTeam a in assignTeamsList)
            {
                completeRubricString += parseRubricToString(a.Team, curAssignment);
                completeRubricString += "--------------------------------------------------------\n";
            }

            ViewBag.CompleteRubricString = completeRubricString;

            context.Response.AppendHeader("Content-Disposition", "attachment; filename=\"" + currentCourse.Name  + "_" + curAssignment.AssignmentName + "_rubric.txt\"");
            Response.ContentType = "application/octet-stream";

            return View();
        }

        public string parseRubricToString(Team team, Assignment curAssignment)
        {
            int i = 0;
            string header = "";
            string finalString = "";
            string criterions = "";
            string points = "";
            string comment = "";
            string gComment = "";
            string globalComments = "";

            header = "------------------------------------------------\n";
            header += "*** STUDENT/TEAM: " + team.DisplayName(ActiveCourseUser.AbstractRole) + "\n";

            // Criterion rows set up
            RubricEvaluation rubricEvaluation = new RubricEvaluation();
            rubricEvaluation = (from re in db.RubricEvaluations
                                where re.AssignmentID == curAssignment.ID
                                && re.RecipientID == team.ID
                                select re).FirstOrDefault();

            Rubric rubric = db.Rubrics.Find(curAssignment.ID);

            List<Criterion> criterionList = (from c in db.Criteria
                                             where c.RubricID == curAssignment.RubricID
                                             select c).ToList();
            foreach (Criterion criterion in criterionList)
            {
                // existing graded rubric
                if (rubricEvaluation != null)
                {
                    points = (from g in rubricEvaluation.CriterionEvaluations
                             where g.CriterionID == criterion.ID
                             select g.Score).FirstOrDefault().ToString();
                    comment = (from c in rubricEvaluation.CriterionEvaluations
                               where c.CriterionID == criterion.ID
                               select c.Comment).FirstOrDefault().ToString();
                }
                criterions += "*** " + criterion.CriterionTitle + " (Weight: " + criterion.Weight + ")\n";
                criterions += "*** Points (0 - " + curAssignment.Rubric.Levels.Sum(s => s.PointSpread) + "):\n";
                criterions += points + "\n";
                criterions += "*** Comments: \n";
                criterions += comment + "\n\n";
                criterions += "------------------------------------------------\n";

                i++;
            }

            // Global comments string set up
            if (curAssignment.Rubric.HasGlobalComments)
            {
                if (rubricEvaluation != null)
                {
                    gComment = rubricEvaluation.GlobalComment;
                }
                gComment += "\n";

                globalComments = "*** General Comments:\n" + gComment + "\n";
                globalComments += "------------------------------------------------\n";
            }
            finalString = header + criterions + globalComments;

            return (finalString);
        }
    }
}
