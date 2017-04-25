using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
#if !DEBUG
    [RequireHttps]
#endif
    [OsbleAuthorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class AssignmentController : OSBLEController
    {
        public AssignmentController()
        {
            ViewBag.CurrentTab = "Assignments";
            ViewBag.HideMail = ActiveCourseUser != null ? OSBLE.Utility.DBHelper.GetAbstractCourseHideMailValue(ActiveCourseUser.AbstractCourseID): false;  
        }

        [CanModifyCourse]
        public ActionResult Delete(int id)
        {
            //verify that the user attempting a delete owns this course and that the id is valid
            if (!ActiveCourseUser.AbstractRole.CanModify)
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


            if (ActiveCourseUser.AbstractRole.CanGrade)
            {
                //For CanGrade roles, show all assignments
                Assignments = (from assignment in db.Assignments
                               where assignment.CourseID == ActiveCourseUser.AbstractCourseID
                               orderby assignment.IsDraft, assignment.ReleaseDate
                               select assignment).ToList();

                //We want the number of Posters who's initial posts should be tracked. So students in this course.
                ViewBag.TotalDiscussionPosters = (from cu in db.CourseUsers
                                                  where cu.AbstractCourseID == ActiveCourseUser.AbstractCourseID &&
                                                  cu.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                                  select cu).Count();
            }
            else if (ActiveCourseUser.AbstractRole.CanSubmit)
            {
                //for CanSubmit, show all assignments except draft assignment
                Assignments = (from assignment in db.Assignments
                               where !assignment.IsDraft &&
                               assignment.CourseID == ActiveCourseUser.AbstractCourseID
                               orderby assignment.ReleaseDate
                               select assignment).ToList();

                
                //This Dictionary contains:
                    //Key: AssignmentID
                    //Value: tuple(submissionTime (as string), discussion team ID as int or null)
                Dictionary<int, string> submissionInfo = new Dictionary<int,string>();
                Dictionary<int, DiscussionTeam> dtInfo = new Dictionary<int, DiscussionTeam>();

                foreach (Assignment a in Assignments)
                {
                    if (a.HasDeliverables)
                    {
                        AssignmentTeam at = GetAssignmentTeam(a, ActiveCourseUser);
                        DateTime? subTime = FileSystem.GetSubmissionTime(at);                        
                        string submissionTime = "No Submission";
                        if (subTime != null) //found a submission time, Reassign submissionTime
                        {
                            DateTime convertedSubTime = new DateTime(subTime.Value.Ticks, DateTimeKind.Unspecified);
                            //Submission time comes back in UTC
                            convertedSubTime = convertedSubTime.UTCToCourse(ActiveCourseUser.AbstractCourseID);
                            submissionTime = convertedSubTime.ToString();
                            
                        }
                        submissionInfo.Add(a.ID, submissionTime);
                    }
                    else
                    {
                        submissionInfo.Add(a.ID, "No Submission");
                    }

                    if (a.Type == AssignmentTypes.DiscussionAssignment || a.Type == AssignmentTypes.CriticalReviewDiscussion)
                    {
                        foreach (DiscussionTeam dt in a.DiscussionTeams)
                        {
                            foreach (TeamMember tm in dt.GetAllTeamMembers())
                            {
                                if (tm.CourseUserID == ActiveCourseUser.ID) //Checking if Client is a member within the DiscussionTeam
                                {
                                    if (dtInfo.ContainsKey(a.ID) == false)
                                    {
                                        dtInfo.Add(a.ID, dt);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        dtInfo.Add(a.ID, null);
                    }
                    ViewBag.dtInfo = dtInfo;
                    
                }

                //Gathering the Team Evaluations for the current user's teams.
                List<TeamEvaluation> teamEvaluations = (from t in db.TeamEvaluations
                                                        where t.EvaluatorID == ActiveCourseUser.ID
                                                        select t).ToList();

                ViewBag.TeamEvaluations = teamEvaluations;
                ViewBag.CourseUser = ActiveCourseUser;
                ViewBag.SubmissionInfoDictionary = submissionInfo;                
            }
            else if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator)
            {
                //for moderators, grab only discussion/Critical review assignment types that they are to partcipate in
                    //Or any discussions that are classwide
                var discussionBasedAssignment = (from assignment in db.Assignments
                               where !assignment.IsDraft &&
                               assignment.CourseID == ActiveCourseUser.AbstractCourseID &&
                               (assignment.AssignmentTypeID == (int)AssignmentTypes.CriticalReviewDiscussion ||
                               assignment.AssignmentTypeID == (int)AssignmentTypes.DiscussionAssignment)
                               orderby assignment.DueDate
                               select assignment).ToList();

                //going through all the discussion assignment's discussion teams looking for a team member who is the current user.
                foreach (Assignment assignment in discussionBasedAssignment)
                {
                    //Checking if classwide
                    if (assignment.HasDiscussionTeams == false)
                    {
                        Assignments.Add(assignment);
                        continue;
                    }

                    //checking if user is on team
                    bool addedAssignment = false;
                    foreach (DiscussionTeam dt in assignment.DiscussionTeams)
                    {
                        if (addedAssignment) //if the assignment has already been added, then break out of this foreach and go back to assignment foreach.
                        {
                            break;
                        }
                        foreach (TeamMember tm in dt.GetAllTeamMembers())
                        {
                            if (tm.CourseUserID == ActiveCourseUser.ID)
                            {
                                Assignments.Add(assignment);
                                addedAssignment = true;
                                break;
                            }
                        }
                    }
                }
            }

            //Seperate all assignments for organizing into one list
            List<Assignment> Past = (from a in Assignments
                                     where a.DueDate < DateTime.UtcNow &&
                                     !a.IsDraft
                                     orderby a.DueDate
                                     select a).ToList();

            List<Assignment> Present = (from a in Assignments
                                        where a.ReleaseDate < DateTime.UtcNow &&
                                        a.DueDate > DateTime.UtcNow &&
                                        !a.IsDraft
                                        orderby a.DueDate
                                        select a).ToList();

            List<Assignment> Future = (from a in Assignments
                                       where a.DueDate >= DateTime.UtcNow &&
                                       a.ReleaseDate >= DateTime.UtcNow &&
                                       !a.IsDraft
                                       orderby a.DueDate
                                       select a).ToList();

            List<Assignment> Draft = (from a in Assignments
                                      where a.IsDraft
                                      orderby a.DueDate
                                      select a).ToList();

            //Count them
            ViewBag.PastCount = Past.Count();
            ViewBag.PresentCount = Present.Count();
            ViewBag.FutureCount = Future.Count();
            ViewBag.DraftCount = Draft.Count();

            //Combine back into one list.
            Present.AddRange(Past);
            Present.AddRange(Future);
            Present.AddRange(Draft);
            List<Assignment> AllAssignments = Present;


            ViewBag.Assignments = AllAssignments;
            ViewBag.CurrentDate = DateTime.UtcNow;
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
            if (assignment.CourseID == ActiveCourseUser.AbstractCourse.ID) 
            {
                assignment.IsDraft = !assignment.IsDraft;
                db.SaveChanges();
                if (assignment.IsDraft) //assignment has been changed to draft, remove associated events
                {
                    if (assignment.AssociatedEvent != null)
                    {
                        db.Events.Remove(assignment.AssociatedEvent);
                        assignment.AssociatedEventID = null;
                        db.SaveChanges();
                    }

                    if (assignment.DiscussionSettings != null && assignment.DiscussionSettings.AssociatedEventID != null)
                    {
                        //remove event manually
                        db.Events.Remove(assignment.DiscussionSettings.AssociatedEvent);
                        assignment.DiscussionSettings.AssociatedEventID = null;
                        db.SaveChanges();
                    }
                }
                else //Published, add an event
                {
                    EventController.CreateAssignmentEvent(assignment, ActiveCourseUser.ID, db);
                } 
            }
            return RedirectToRoute(new { action = "Index" });
        }

        /// <summary>
        /// This function will publish all the non-student-evaluated rubrics for the given assignment, and return the user to their original page.
        /// </summary>
        /// <param name="assignmentId"></param>
        [CanGradeCourse]
        public ActionResult PublishAllRubrics(int assignmentId)
        {
            if (assignmentId > 0)
            {
                Assignment assignment = db.Assignments.Find(assignmentId);

                //Getting the list of evaluations that have been saved as draft from a "CanGrade" role, to avoid grabbing student-evaluated rubrics.
                List<RubricEvaluation> evaluations = (from e in db.RubricEvaluations
                                                      where e.AssignmentID == assignment.ID &&
                                                      e.IsPublished == false &&
                                                      e.Evaluator.AbstractRole.CanGrade
                                                      select e).ToList();

                // For TAs, get rid of evaluations not within their section
                if (ActiveCourseUser.AbstractRoleID == (int) CourseRole.CourseRoles.TA)
                {
                    List<string> sectionIds = new List<string>();
                    if (ActiveCourseUser.Section == -1)
                    {
                        sectionIds.AddRange(ActiveCourseUser.MultiSection.Split(',').ToList());                        
                    }
                    else
                    {
                        sectionIds.Add(ActiveCourseUser.Section.ToString());
                    }

                    for (int i = 0; i < evaluations.Count; i++)
                    {
                        bool inSection = false;
                        foreach (TeamMember member in evaluations[i].Recipient.TeamMembers) // Must find at least one team member in TA's section to let TA see this team
                        {
                            if (sectionIds.Contains(member.CourseUser.Section.ToString()))
                            {
                                inSection = true;
                                break;
                            }
                        }
                        if (!inSection)
                        {
                            evaluations.RemoveAt(i);
                            i--;
                        }
                    }   
                }                

                foreach (RubricEvaluation re in evaluations)
                {
                    re.IsPublished = true;
                    re.DatePublished = DateTime.Now;
                    (new NotificationController()).SendRubricEvaluationCompletedNotification(assignment, re.Recipient);
                }
                db.SaveChanges();
            }
            return Redirect(Request.UrlReferrer.ToString());
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
            //For the code below, order is important. The order is UserProfile.LastName, thenby UserProfile.Firstname

            Assignment assignment = db.Assignments.Find(TeamEvaluationAssignmentId);
            Team precTeam = db.Teams.Find(precedingTeamId);

            var cuIDs = (from tm in precTeam.TeamMembers
                         select tm.CourseUserID).ToList();

            List<TeamEvaluation> OurTeamEvals = db.TeamEvaluations.Where(te => cuIDs.Contains(te.RecipientID) && te.TeamEvaluationAssignmentID == assignment.ID).ToList();
            List<double> MultipliersInOrder = new List<double>();
            List<string> CommentsInOrder = new List<string>();
            List<CourseUser> CourseUsersInOrder = (from tm in precTeam.TeamMembers
                                                   where tm.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                                   orderby tm.CourseUser.UserProfile.LastName, tm.CourseUser.UserProfile.FirstName
                                                   select tm.CourseUser).ToList();
            double[,] table;

            table = new double[CourseUsersInOrder.Count, CourseUsersInOrder.Count + 1];
            int i = 0;
            int j = 0;

            foreach (CourseUser cu in CourseUsersInOrder)
            {
                j = 0;
                List<TeamEvaluation> myEvals= (from te in OurTeamEvals
                                 where te.EvaluatorID == cu.ID
                                 select te).ToList().OrderBy(te => te.Recipient.UserProfile.LastName).ThenBy(te => te.Recipient.UserProfile.FirstName).ToList();

                //If there are evaluations then calculate multipler from them, otherwise they get a default multiplier of 1.0
                if (OurTeamEvals.Count > 0) //Only calculate multiplier if there are evaluations
                {
                    double myPoints = (from te in OurTeamEvals
                                       where te.RecipientID == cu.ID
                                       select te.Points).Sum();

                    double myMulti = myPoints / ((OurTeamEvals.Count / CourseUsersInOrder.Count) * 100);
                    MultipliersInOrder.Add(myMulti);
                }
                else
                {
                    MultipliersInOrder.Add(1.0);
                }
                

                if (myEvals != null && myEvals.Count > 0) //Using existing evaluation
                {
                    CommentsInOrder.Add(myEvals.FirstOrDefault().Comment);
                    foreach (TeamEvaluation te in myEvals)
                    {
                        table[i,j] = te.Points;
                        j++;
                    }
                }
                else //Creating Evlauations as they did not exist
                {
                    CommentsInOrder.Add("");
                    foreach (CourseUser cu2 in CourseUsersInOrder)
                    {
                        table[i, j] = 0;
                        j++;
                    }
                }
                i++;
            }

            ViewBag.CommentsInOrder = CommentsInOrder;
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
            Team previousTeam;
            if (a.PreceedingAssignment.Type == AssignmentTypes.DiscussionAssignment)
            {
                //Note: This could have non-student types on the team, such as moderators or TAs
                DiscussionTeam dt = GetDiscussionTeam(a.PreceedingAssignment, ActiveCourseUser);
                previousTeam = new Team();
                previousTeam.TeamMembers = dt.Team.TeamMembers.Where(tm => tm.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student).ToList();
            }
            else
            {
                previousTeam = GetAssignmentTeam(a.PreceedingAssignment, ActiveCourseUser).Team;
            }
            AssignmentTeam at = GetAssignmentTeam(a, ActiveCourseUser);
            if (at != null && a.Type == AssignmentTypes.TeamEvaluation)
            {
                ViewBag.AssignmentTeam = at;
                ViewBag.PreviousTeam = previousTeam;
                
                List<TeamEvaluation> teamEvals = (from te in db.TeamEvaluations
                                           where
                                               te.TeamEvaluationAssignmentID == assignmentId &&
                                               te.EvaluatorID == ActiveCourseUser.ID
                                           orderby te.Recipient.UserProfile.LastName
                                           select te).ToList();
                //MG: evaluator (currentuser) must have completed at as many evaluations as team members from the previous assignment. 
                //Otherwise, use artificial team evals for view
                if (teamEvals.Count < previousTeam.TeamMembers.Count) //Creating new team eval
                {
                    List<TeamEvaluation> artificialTeamEvals = new List<TeamEvaluation>();

                    foreach (TeamMember tm in previousTeam.TeamMembers.OrderBy(tm2 => tm2.CourseUser.UserProfile.LastName))
                    {
                        TeamEvaluation te = new TeamEvaluation();
                        te.Points = 0;
                        te.Recipient = tm.CourseUser;
                        artificialTeamEvals.Add(te);
                    }
                    ViewBag.SubmitButtonValue = "Submit";
                    ViewBag.TeamEvaluations = artificialTeamEvals;
                    ViewBag.InitialPointsPossible = previousTeam.TeamMembers.Count * 100;
                }
                else //using existing team evals 
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
             IAssignmentTeam pAt;
             if (assignment.PreceedingAssignment.Type == AssignmentTypes.DiscussionAssignment)
             {
                 pAt = GetDiscussionTeam(assignment.PreceedingAssignment, ActiveCourseUser);
             }
             else
             {
                 pAt = GetAssignmentTeam(assignment.PreceedingAssignment, ActiveCourseUser);
             }

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

            List<int> TeamEvalPoints = new List<int>();

            //Creating or editing TeamEvaluations for each team member from the previous assignment assignment team
            //since the team could be a discussion team, only select team members who are students.
            foreach (TeamMember tm in pAt.Team.TeamMembers.Where(tm => tm.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student))
            {
                TeamEvaluation te = (from eval in existingTeamEvaluations
                                     where eval.RecipientID == tm.CourseUserID
                                     select eval).FirstOrDefault();

                string param = "points-" + tm.CourseUserID;
                int paramPoints = Convert.ToInt32(Request.Params[param]);
                TeamEvalPoints.Add(paramPoints);

                if (te == null) //No TE exists, create one
                {
                    TeamEvaluation newTE = new TeamEvaluation();
                    newTE.TeamEvaluationAssignmentID = assignmentId;
                    newTE.AssignmentUnderReviewID = (int)assignment.PrecededingAssignmentID;
                    newTE.EvaluatorID = ActiveCourseUser.ID;
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

            if (assignment.TeamEvaluationSettings.DiscrepancyCheckSize > 0 && (TeamEvalPoints.Max() - TeamEvalPoints.Min()) > assignment.TeamEvaluationSettings.DiscrepancyCheckSize)
            {
                (new NotificationController()).SendTeamEvaluationDiscrepancyNotification(pAt.TeamID, assignment);
            }

            db.SaveChanges();
            return RedirectToAction("Index");
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
            assignment.CriticalReviewPublishDate = DateTime.UtcNow;
            db.SaveChanges();
            return RedirectToAction("Index", "Home", new { area = "AssignmentDetails", assignmentId = assignmentID });
        }

        /// <summary>
        /// This function will export all student grades for the selected assignment.
        /// </summary>
        /// <param name="assignmentID">assignment ID</param>
        /// <returns></returns>
        [CanGradeCourse]
        public ActionResult ExportAssignmentGrades(int assignmentID)
        {
            //find all students for current course
            List<CourseUser> students = (from c in db.CourseUsers
                                         where c.AbstractCourseID == ActiveCourseUser.AbstractCourseID &&
                                         c.AbstractRoleID == (int)CourseRole.CourseRoles.Student
                                         select c).ToList();

            if (ActiveCourseUser.Section != -2) //instructors or all sections users can download all student grades
            {
                List<int> sections = new List<int>(); //need to keep track of multiple sections.

                if (ActiveCourseUser.Section == -1) //multiple sections
                {
                    List<string> idList = ActiveCourseUser.MultiSection.Split(',').ToList();
                    foreach (string id in idList)
	                {
		                int section;

                        if (Int32.TryParse(id, out section))
                        {
                            sections.Add(section);
                        }
	                }
                }
                else
                {
                    sections.Add(ActiveCourseUser.Section);
                }

                // For TAs, make rid of any student that isn't in the TA's section.
                for (int i = 0; i < students.Count; i++)
                {
                    if (!sections.Contains(students[i].Section))
                    {
                        students.RemoveAt(i);
                        i--;
                    }
                }
            }  

            //key-value pair for names-grades
            Dictionary<string, string> grades = new Dictionary<string, string>();
            //seed dictionary with student last, first names
            foreach (CourseUser student in students)
            {
                grades.Add(student.UserProfile.FullName, "");
            }
            //get graded rubrics
            List<RubricEvaluation> rubricEvaluations = null;
            rubricEvaluations = db.RubricEvaluations.Where(re => re.Evaluator.AbstractRole.CanGrade &&
                                                                     re.AssignmentID == assignmentID).ToList();

            if (rubricEvaluations.Count > 0) //make sure there are rubrics saved
            {
                foreach (RubricEvaluation rubricEvaluation in rubricEvaluations)
                {
                    string rubricStudentName = "";

                    //we need to go through teams to handle team assignments. this works for individuals because an individual is a team of 1
                    foreach (var teamMember in rubricEvaluation.Recipient.TeamMembers)
                    {
                        rubricStudentName= teamMember.CourseUser.UserProfile.FullName;
                        
                        if (rubricEvaluation.IsPublished)
                        {
                            //update value to match key
                            if (grades.ContainsKey(rubricStudentName))
                            {
                                grades[rubricStudentName] = RubricEvaluation.GetGradeAsDouble(rubricEvaluation.ID).ToString();        
                            }
                        }
                    }
                }

                //sort the grades A-Z by last name
                var sortedGradesList = grades.Keys.ToList();
                sortedGradesList.Sort();
                
                //make a csv for export
                var csv = new StringBuilder();

                foreach (var key in sortedGradesList)
                {
                    //place quotes around name so the first, last format doesn't break the csv
                    string temp = "\"" + key + "\"";
                    var newLine = String.Format("{0},{1}{2}", temp, grades[key], Environment.NewLine);
                    csv.Append(newLine);
                }
                
                const string contentType = "text/plain";
                var bytes = Encoding.UTF8.GetBytes(csv.ToString());

                return File(bytes, contentType, rubricEvaluations.First().Assignment.AssignmentName + " "+ DateTime.Now + " (Exported Grades).csv");
            }

            if (Request.UrlReferrer != null) 
                return Redirect(Request.UrlReferrer.ToString());
            else
                return RedirectToAction("Index", "Home", new { area = "AssignmentDetails", assignmentId = assignmentID }); 
                
            
        }
    }
}
