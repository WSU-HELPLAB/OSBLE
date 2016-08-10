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
            ViewBag.HideMail = OSBLE.Utility.DBHelper.GetAbstractCourseHideMailValue(ActiveCourseUser.AbstractCourseID); 
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
                    double score = 0;
                    Double.TryParse(Request.Form[critScoreKey], out score);
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
                critEval.Score = 0.0;
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

            // For TAs, make sure they are allowed to view this student!
            if (ActiveCourseUser.AbstractRole.Name == "TA")
            {
                if (ActiveCourseUser.Section != cu.Section)
                {
                    return viewModel;
                }
            }
            
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
                    critEval.Score = 0.0;
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
            else if (assignment.DiscussionTeams != null && assignment.DiscussionTeams.Count > 0)
            {
                // need a DiscussionTeamList to be able to prevent null reference exceptions when grading a discussion assignment
                viewModel.DiscussionTeamList = assignment.DiscussionTeams.OrderBy(t => t.Team.Name).ToList();
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
                    critEval.Score = 0.0;
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
                    List<double?> scoreList = mergedViewModel.MergedEvaluations.Select(e => e.CriterionEvaluations.ElementAt(i).Score).ToList();
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
        public ActionResult ImportIndividualFromFile(HttpPostedFileBase file)
        {
            List<string> parsedData = new List<string>();
            StreamReader readFile = new StreamReader(file.InputStream);
            string assignmentName = "";
            string courseName = "";
            string studentNames = "";
            string line;
            List<string> errorMessages = new List<string>();
            bool importError = false;

            try  // Catch taken from failure code at the bottom of this function, most breaking happens in these parsing loops
            {
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

                // Check to see if the assignment name in the rubric matches the assignment we're evaluating
                string currentAssignmentName = Session["AssignmentName"].ToString();
                if (assignmentName != currentAssignmentName)
                {
                    errorMessages.Add("The assignment name in the rubric does not match the current assignment.");
                    importError = true;
                }

                Assignment curAssignment = (from a in db.Assignments where a.AssignmentName == assignmentName select a).FirstOrDefault();
                List<AssignmentTeam> assignTeamsList = (from at in db.AssignmentTeams where at.AssignmentID == curAssignment.ID select at).ToList();

                if (curAssignment != null)
                {
                    // Check to see if the course name in the rubric matches the course we're evaluating
                    courseName = parsedData.ElementAt(0).Split(':')[1].Split('(')[0].Trim();
                    string currentCourseName = (ViewBag.ActiveCourse as CourseUser).AbstractCourse.Name;
                    if (courseName != currentCourseName)
                    {
                        errorMessages.Add("The course name in the rubric does not match the current course.");
                        importError = true;
                    }
                    parsedData.RemoveAt(0);

                    if (ActiveCourseUser.AbstractCourse.Name == courseName)
                    {
                        // Check to see if the student name in the rubric matches the course we're evaluating
                        studentNames = parsedData.ElementAt(0).Split(':')[1].Trim();
                        string currentStudentName = Session["StudentName"].ToString();

                        if (studentNames != currentStudentName)
                        {
                            errorMessages.Add("The student/team name in the rubric does not match the current student/team.");
                            importError = true;
                        }
                        parsedData.RemoveAt(0);

                        CourseUser user = new CourseUser();

                        foreach (AssignmentTeam tempTeam in assignTeamsList)
                        {
                            if (tempTeam.Team.Name == studentNames)
                            {
                                user = tempTeam.Team.TeamMembers.Where(m => m.CourseUser.UserProfile.FullName == studentNames).FirstOrDefault().CourseUser;
                                break;
                            }
                        }
                        AssignmentTeam assignmentTeam = GetAssignmentTeam(curAssignment, user);

                        if (assignmentTeam != null)
                        {
                            int userID = user.ID;

                            RubricEvaluation rubricEvaluation = (from re in db.RubricEvaluations
                                                                 where re.Assignment.ID == curAssignment.ID
                                                                     && re.RecipientID == assignmentTeam.TeamID
                                                                 select re).FirstOrDefault();

                            if (rubricEvaluation == null)
                            {
                                rubricEvaluation = new RubricEvaluation();
                                Rubric rubric = db.Rubrics.Find(curAssignment.Rubric.ID);

                                rubricEvaluation.AssignmentID = curAssignment.ID;
                                rubricEvaluation.EvaluatorID = ActiveCourseUser.ID;
                                rubricEvaluation.RecipientID = assignmentTeam.Team.ID;

                                foreach (Criterion c in rubric.Criteria)
                                {
                                    CriterionEvaluation criterionEvaluation = new CriterionEvaluation();
                                    criterionEvaluation.CriterionID = c.ID;
                                    rubricEvaluation.CriterionEvaluations.Add(criterionEvaluation);
                                }
                                db.RubricEvaluations.Add(rubricEvaluation);
                            }

                            //int i = 0;
                            foreach (CriterionEvaluation ce in rubricEvaluation.CriterionEvaluations)
                            {
                                // Get the max score possible for this criterion.
                                int maxScore = 0;
                                foreach(Level level in curAssignment.Rubric.Levels)
                                {
                                    maxScore += level.PointSpread;
                                }

                                if (parsedData.FirstOrDefault().Split('(')[0].Substring(4).Trim() == ce.Criterion.CriterionTitle)
                                {
                                    // removing the criterion title row
                                    parsedData.RemoveAt(0);
                                    // Removing the points title row
                                    parsedData.RemoveAt(0);
                                    // Makeing sure the points field was populated
                                    if (!parsedData.ElementAt(0).StartsWith("***"))
                                    {
                                        double increment = 1.0; 
                                        ce.Score = Convert.ToDouble(parsedData.ElementAt(0));

                                        // Make sure score is within bounds and has proper increment value
                                        if (ce.Score < 0)
                                        {
                                            if (!errorMessages.Contains("Criterion scores cannot be below zero."))
                                                errorMessages.Add("Criterion scores cannot be below zero.");
                                            importError = true;
                                        }
                                        if (ce.Score > maxScore)
                                        {
                                            if (!errorMessages.Contains("Criterion scores cannot be higher than possible point spread."))
                                                errorMessages.Add("Criterion scores cannot be higher than possible point spread.");
                                            importError = true;
                                        }

                                        if (curAssignment.Rubric.EnableQuarterStep) increment = 0.25;
                                        else if (curAssignment.Rubric.EnableHalfStep) increment = 0.5;

                                        if (ce.Score % increment != 0)
                                        {
                                            if (!errorMessages.Contains("Criterion scores did not have an acceptable decimal point value."))
                                                errorMessages.Add("Criterion scores did not have an acceptable decimal point value.");
                                            if (!errorMessages.Contains("Point values can be in 0.5 or 0.25 increments if such options were selected in the assignment settings."))
                                                errorMessages.Add("Point values can be in 0.5 or 0.25 increments if such options were selected in the assignment settings.");
                                            importError = true;
                                        }

                                        // removing the points row after getting the value
                                        parsedData.RemoveAt(0);
                                    }
                                    else
                                    {
                                        ce.Score = 0;   // Default score to 0. Without this, the slider will disappear
                                    }

                                    // will need to change this to handle merging
                                    ce.Comment = "";
                                    parsedData.RemoveAt(0);

                                    // Makeing sure the user actually entered comments
                                    while (parsedData.Count != 0 && !parsedData.ElementAt(0).StartsWith("***"))
                                    {
                                        ce.Comment += parsedData.ElementAt(0);
                                        // removing the comments string from the list
                                        parsedData.RemoveAt(0);
                                        if (parsedData.Count != 0 && !parsedData.ElementAt(0).StartsWith("***"))
                                            ce.Comment += "\n";
                                    }
                                }
                                else
                                {
                                    errorMessages.Add("Rubric contained criterion " + "\"" + parsedData.FirstOrDefault().Split('(')[0].Substring(4).Trim() + "\" which does not exist in the current assignment");
                                    importError = true;
                                }
                            }

                            if (curAssignment.Rubric.HasGlobalComments)
                            {
                                if (parsedData.FirstOrDefault().StartsWith("*** General Comments:"))
                                {
                                    // remove the general comments header
                                    parsedData.RemoveAt(0);

                                    rubricEvaluation.GlobalComment = "";
                                    // Check to see if any lines are left (Instructor could have left global comments blank)
                                    while (parsedData.Count > 0 && !parsedData.ElementAt(0).StartsWith("***"))
                                    {
                                        rubricEvaluation.GlobalComment += parsedData.ElementAt(0);
                                        // Remove the general comments for the list
                                        parsedData.RemoveAt(0);

                                        if (parsedData.Count > 0) rubricEvaluation.GlobalComment += "\n";
                                    }
                                }
                            }

                            if (!importError)
                            {
                                db.SaveChanges();
                                Session["CuId"] = "";           // Remove current user id session variable if successful so future errors don't forward to this person's rubric
                                return RedirectToAction("Index", "Rubric", new { assignmentId = curAssignment.ID, cuId = userID });
                            }
                        }
                    }
                }
            }
            catch
            {
                ViewBag.Error = "Could not import the rubric as the formatting is not correct!\n";
                errorMessages.Add("Make sure you uploaded the correct .txt rubric file. It's name does not matter, but it's content must be correct.");
                errorMessages.Add("All lines beginning with *** and the divider lines must be left in place.");
                ViewBag.errorMessages = errorMessages;
                ViewBag.ErrorLocation = "ImportIndividual";
                return View("ImportError");
            }

            if (importError)
            {
                ViewBag.Error = "Could not import the rubric as the formatting is not correct!\n";
                errorMessages.Add("If you want to make sure the format is correct, simply export a rubric from the Rubric Page and only edit score and comment lines.");
                ViewBag.errorMessages = errorMessages;
                ViewBag.ErrorLocation = "ImportIndividual";
            }

            return View("ImportError");
        }

        [CanModifyCourse]
        public ActionResult ImportAllFromFile(HttpPostedFileBase file)              // Written by Nathan VelaBorja
        {
            int assignmentId = Convert.ToInt32(Session["AssignmentId"].ToString());
            AbstractCourse currentCourse = ActiveCourseUser.AbstractCourse;
            Assignment curAssignment = (from a in db.Assignments where a.ID == assignmentId select a).FirstOrDefault();
            List<AssignmentTeam> assignTeamsList = (from at in db.AssignmentTeams where at.AssignmentID == assignmentId select at).ToList();
            List<string> errorMessages = new List<string>();
            StreamReader readFile = new StreamReader(file.InputStream);
            bool importError = false;
            List<string> fileLines = new List<string>();                            // Represent all lines of the import file excluding the comment lines.
            List<string>[] studentLines = new List<string>[assignTeamsList.Count];
            string inputLine = "";
            int inputIndex = 0, studentIndex = -1;

            // Constuct each studentLines List
            for (int i = 0; i < studentLines.Count(); i++) studentLines[i] = new List<string>(); 

            while ((inputLine = readFile.ReadLine()) != null)                       // First get all lines of the file and push them into fileLines.
            {
                if (!inputLine.StartsWith("---") && inputLine.Trim() != "")         // Exclude comment, divider, and blank lines
                    fileLines.Add(inputLine);
            }

            try   // Main parsing try. This will catch any out-of-range exceptions or general format errors in the file.
            {
                // First check to see if the rubric is for the right assignment. (should be on first line)
                string currentAssignmentName = Session["AssignmentName"].ToString();
                string assignmentHeaderFormat = "*** ASSIGNMENT: ";
                string studentHeaderFormat = "*** STUDENT/TEAM: ";
                string generalCommentHeader = "*** General Comments:";
                string assignmentNameFromFile = fileLines[inputIndex];
                assignmentNameFromFile = assignmentNameFromFile.Substring(assignmentHeaderFormat.Length);

                if (assignmentNameFromFile == currentAssignmentName)               // Continue parsing only if the assignment name is correct
                {
                    while (!fileLines[inputIndex].StartsWith(studentHeaderFormat)) // Move to first student header
                        inputIndex++;

                    while(inputIndex < fileLines.Count)
                    {
                        string currentLine = fileLines[inputIndex];
                        if (currentLine.StartsWith(studentHeaderFormat))           // If we find student header, incremement student index.
                        {
                            studentIndex++;
                            if (inputIndex >= fileLines.Count) break;
                        }

                        studentLines[studentIndex].Add(currentLine);
                        inputIndex++;
                    }   // If the try fails here, there must have been more students in the rubric than are in the class.

                    // At this point, each studentLines[] list will have the grade and comment lines for that student/team.
                    // Now we have to loop through each and record/update scores.

                    foreach(List<string> lines in studentLines)  
                    {
                        inputIndex = 0;
                        string studentName = lines[inputIndex].Substring(studentHeaderFormat.Length);
                        inputIndex++;

                        // Make sure student is in the class
                        CourseUser user = new CourseUser();

                        foreach(AssignmentTeam tempTeam in assignTeamsList)
                        {
                            if (tempTeam.Team.Name == studentName)
                            {
                                user = tempTeam.Team.TeamMembers.Where(m => m.CourseUser.UserProfile.FullName == studentName).FirstOrDefault().CourseUser;
                                break;
                            }
                        }
                        AssignmentTeam assignmentTeam = GetAssignmentTeam(curAssignment, user);

                        if (assignmentTeam != null)
                        {
                            // If so, get userId and the rubric evaluation if one already exists
                            int userID = user.ID;

                            RubricEvaluation rubricEvaluation = (from e in db.RubricEvaluations where e.RecipientID == assignmentTeam.TeamID select e).FirstOrDefault();

                            if (rubricEvaluation == null)   // If not, we need to create a new one.
                            {
                                rubricEvaluation = new RubricEvaluation();
                                Rubric rubric = db.Rubrics.Find(curAssignment.Rubric.ID);

                                rubricEvaluation.AssignmentID = curAssignment.ID;
                                rubricEvaluation.EvaluatorID = ActiveCourseUser.ID;
                                rubricEvaluation.RecipientID = assignmentTeam.Team.ID;

                                foreach (Criterion c in rubric.Criteria)
                                {
                                    CriterionEvaluation criterionEvaluation = new CriterionEvaluation();
                                    criterionEvaluation.CriterionID = c.ID;
                                    rubricEvaluation.CriterionEvaluations.Add(criterionEvaluation);
                                }
                                db.RubricEvaluations.Add(rubricEvaluation);
                            }

                            foreach (CriterionEvaluation ce in rubricEvaluation.CriterionEvaluations)
                            {
                                // Get the max score possible for this criterion.
                                int maxScore = 0;
                                foreach (Level level in curAssignment.Rubric.Levels)
                                {
                                    maxScore += level.PointSpread;
                                }

                                if (lines[inputIndex].Split('(')[0].Substring(4).Trim() == ce.Criterion.CriterionTitle)
                                {
                                    // Move past the title row and the point title row
                                    inputIndex += 2;

                                    // Making sure the points field was populated
                                    if (!lines[inputIndex].StartsWith("***"))
                                    {
                                        ce.Score = Convert.ToDouble(lines[inputIndex]);  // Grab score then move to next line
                                        double increment = 1.0;
                                        inputIndex++;

                                        // Make sure score is within bounds and proper increment
                                        if (ce.Score < 0)
                                        {
                                            if (!errorMessages.Contains("Criterion scores cannot be below zero."))
                                                errorMessages.Add("Criterion scores cannot be below zero.");
                                            importError = true;
                                        }
                                        if (ce.Score > maxScore)
                                        {
                                            if (!errorMessages.Contains("Criterion scores cannot be higher than possible point spread."))
                                                errorMessages.Add("Criterion scores cannot be higher than possible point spread.");
                                            importError = true;
                                        }

                                        if (curAssignment.Rubric.EnableQuarterStep) increment = 0.25;
                                        else if (curAssignment.Rubric.EnableHalfStep) increment = 0.5;

                                        if (ce.Score % increment != 0)
                                        {
                                            if (!errorMessages.Contains("Criterion scores did not have an acceptable decimal point value."))
                                                errorMessages.Add("Criterion scores did not have an acceptable decimal point value.");
                                            if (!errorMessages.Contains("Point values can be in 0.5 or 0.25 increments if such options were selected in the assignment settings."))
                                                errorMessages.Add("Point values can be in 0.5 or 0.25 increments if such options were selected in the assignment settings.");
                                            importError = true;
                                        }
                                    }
                                    else
                                    {
                                        ce.Score = 0;
                                    }

                                    // will need to change this to handle merging
                                    ce.Comment = "";
                                    inputIndex++;

                                    // Making sure the user actually entered comments
                                    while (inputIndex < lines.Count && !lines[inputIndex].StartsWith("***"))
                                    {
                                        ce.Comment += lines[inputIndex];
                                        inputIndex++;
                                        if (inputIndex < lines.Count && !lines[inputIndex].StartsWith("***"))
                                            ce.Comment += "\n";
                                    }
                                }
                                else
                                {
                                    errorMessages.Add("Rubric contained criterion " + "\"" + lines[inputIndex].Split('(')[0].Substring(4).Trim() + "\" which does not exist in the current assignment");
                                    importError = true;
                                }
                            }

                            if (curAssignment.Rubric.HasGlobalComments)
                            {
                                if (lines[inputIndex].StartsWith(generalCommentHeader))
                                {
                                    // Move past the general comments header
                                    inputIndex++;

                                    // Check to see if any lines are left (Instructor could have left global comments blank)
                                    if (inputIndex < lines.Count && !lines[inputIndex].StartsWith("***"))
                                    {
                                        rubricEvaluation.GlobalComment = lines[inputIndex];
                                        inputIndex++;
                                    }
                                    else rubricEvaluation.GlobalComment = "";
                                }
                            }
                        }
                        else
                        {
                            errorMessages.Add("Student " + studentName + " is in the rubric but could not be found in the class.");
                            importError = true;
                        }
                    }
                }
                else
                {
                    errorMessages.Add("The assignment name specified in the rubric does not match the current assignment name.");
                    importError = true;
                }
            }
            catch
            {
                errorMessages.Add("Make sure the number of students in the rubric match the number of students in the class.");
                importError = true;
            }

            if (importError)
            {
                ViewBag.Error = "Could not import the rubric as the formatting is not correct!\n";
                errorMessages.Add("If you want to make sure the format is correct, simply export a rubric from the Rubric Page and only edit score and comment lines.");
                ViewBag.errorMessages = errorMessages;
                ViewBag.ErrorLocation = "ImportAll";

                return View("ImportError");
            }

            db.SaveChanges();
            return View("ImportSuccess");
        }

        [CanModifyCourse]
        public ActionResult ExportIndividualToFile(int teamID, int assignID)
        {
            string completeRubricString = "";
            string studentName = Session["StudentName"].ToString();
            AbstractCourse currentCourse = ActiveCourseUser.AbstractCourse;
            Team team = db.Teams.Find(teamID);
            Assignment curAssignment = (from a in db.Assignments
                                        where a.ID == assignID
                                        select a).FirstOrDefault();

            completeRubricString = "-----------------------------------------------------------------------------\n";
            completeRubricString += "--- Please use this file to complete rubric evaluations for this assignment.\n";
            completeRubricString += "--- Lines that begin with *** may not be changed.\n";
            completeRubricString += "--- Lines that begin with --- will be ignored by OSBLE.\n";
            completeRubricString += "-----------------------------------------------------------------------------\n\n";
            completeRubricString += "*** ASSIGNMENT: " + curAssignment.AssignmentName + "\n";
            completeRubricString += "*** COURSE:     " + currentCourse.Name + "(" + (currentCourse as Course).Semester + ", " + (currentCourse as Course).Year + ")\n";
            if (curAssignment.Rubric.EnableQuarterStep)
                completeRubricString += "--- This assignment allows 0.25 point increments\n\n";
            else if (curAssignment.Rubric.EnableHalfStep)
                completeRubricString += "--- This assignment allows 0.50 point increments\n\n";
            else completeRubricString += "--- This assignment allows only full point increments\n\n";

            completeRubricString += parseRubricToString(team, curAssignment);

            ViewBag.CompleteRubricString = completeRubricString;

            context.Response.AppendHeader("Content-Disposition", "attachment; filename=\"" + currentCourse.Name + "_" + curAssignment.AssignmentName + "_" + team.DisplayName(ActiveCourseUser.AbstractRole) + " rubric.txt\"");
            Response.ContentType = "application/octet-stream";

            return View();
        }

        [CanModifyCourse]
        public ActionResult ExportAllToFile(int assignID)
        {
            string completeRubricString = "";
            AbstractCourse currentCourse = ActiveCourseUser.AbstractCourse;
            Assignment curAssignment = (from a in db.Assignments
                                        where a.ID == assignID
                                        select a).FirstOrDefault();

            List<AssignmentTeam> assignTeamsList = (from at in db.AssignmentTeams
                                                    where at.AssignmentID == assignID
                                                    select at).ToList();
            
            completeRubricString = "-----------------------------------------------------------------------------\n";
            completeRubricString += "--- Please use this file to complete rubric evaluations for this assignment.\n";
            completeRubricString += "--- Lines that begin with *** may not be changed.\n";
            completeRubricString += "--- Lines that begin with --- will be ignored by OSBLE.\n";
            completeRubricString += "-----------------------------------------------------------------------------\n\n";
            completeRubricString += "*** ASSIGNMENT: " + curAssignment.AssignmentName + "\n";
            completeRubricString += "*** COURSE:     " + currentCourse.Name + "(" + (currentCourse as Course).Semester + ", " + (currentCourse as Course).Year + ")\n";
            if (curAssignment.Rubric.EnableQuarterStep)
                completeRubricString += "--- This assignment allows 0.25 point increments\n\n";
            else if (curAssignment.Rubric.EnableHalfStep)
                completeRubricString += "--- This assignment allows 0.50 point increments\n\n";
            else completeRubricString += "--- This assignment allows only full point increments\n\n";

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
                    try
                    {     
                        comment = (from c in rubricEvaluation.CriterionEvaluations
                                   where c.CriterionID == criterion.ID
                                   select c.Comment).FirstOrDefault().ToString();
                    }
                    catch
                    {
                        comment = "";
                    }
                }
                criterions += "*** " + criterion.CriterionTitle + " (Weight: " + criterion.Weight + ")\n";
                criterions += "*** Points (0 - " + curAssignment.Rubric.Levels.Sum(s => s.PointSpread) + ")\n";
                if (points == "") points = "--- INSERT SCORE HERE";
                criterions += points + "\n";
                criterions += "*** Comments: \n";
                if (comment == "") comment = "--- INSERT COMMENT HERE";
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

                if (gComment == "\n") gComment = "--- INSERT GENERAL COMMENTS HERE (optional)";
                globalComments = "*** General Comments:\n" + gComment + "\n";
                globalComments += "------------------------------------------------\n";
            }
            finalString = header + criterions + globalComments;

            return (finalString);
        }
    }
}
