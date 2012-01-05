using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Configuration;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Models.HomePage;
using System.Data.Entity.Validation;
using System.Diagnostics;
using OSBLE.Models.Assignments.Activities.Scores;

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
            return RedirectToAction("Index");
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
                                                assignment.DueDate <= DateTime.Now
                                                orderby assignment.DueDate
                                                select assignment).ToList();

            // Present assignments are any (non-draft) for which we are between the first start date and last end date.
            List<Assignment> presentAssignments = (from assignment in db.Assignments
                                                   where !assignment.IsDraft &&
                                                   assignment.Category.CourseID == activeCourse.AbstractCourseID &&
                                                   assignment.DueDate > DateTime.Now &&
                                                   assignment.ReleaseDate <= DateTime.Now
                                                   orderby assignment.DueDate
                                                   select assignment).ToList();

            // Future assignments are non-draft assignments whose start date has not yet happened.
            List<Assignment> futureAssignments = (from assignment in db.Assignments
                                                  where !assignment.IsDraft &&
                                                  assignment.Category.CourseID == activeCourse.AbstractCourseID &&
                                                  assignment.ReleaseDate > DateTime.Now
                                                  orderby assignment.ReleaseDate
                                                  select assignment).ToList();

            List<Assignment> draftAssignments = new List<Assignment>();
            if (ActiveCourse.AbstractRole.CanModify)
            {
                // Draft assignments (viewable by instructor only) are assignments that have not yet been published to students
                draftAssignments = (from assignment in db.Assignments
                                    where assignment.IsDraft &&
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
                                                      assignment.Category.CourseID == activeCourse.AbstractCourseID
                                                      select assignment).ToList();
                Dictionary<int, string> submissionPairs = new Dictionary<int, string>();
                foreach (Assignment a in assignmentList)
                {
                    AssignmentTeam at = OSBLEController.GetAssignmentTeam(a, currentUser);
                    DateTime? subTime = GetSubmissionTime(a.Category.Course, a, at);
                    if (subTime != null)
                    {
                        submissionPairs.Add(a.ID, subTime.Value.ToString());
                    }
                    else
                    {
                        submissionPairs.Add(a.ID, "No Submission");
                    }
                }
                ViewBag.SubmissionDictionary= submissionPairs; 
            }

            
            ViewBag.PastAssignments = pastAssignments;
            ViewBag.PresentAssignments = presentAssignments;
            ViewBag.FutureAssignments = futureAssignments;
            ViewBag.DraftAssignments = draftAssignments;
            ViewBag.CanSubmit = activeCourse.AbstractRole.CanSubmit;

            ViewBag.DeliverableTypes = GetListOfDeliverableTypes();
            ViewBag.Submitted = false;

            return View();
        }

        //This is to be used with Ajax
        [CanModifyCourse]
        public ActionResult ActivityTeacherTable(int id)
        {
            try
            {
                Assignment assignment = db.Assignments.Find(id);



                if (assignment.Category.Course == activeCourse.AbstractCourse)
                {
                    ActivityTeacherTableViewModel viewModel = new ActivityTeacherTableViewModel(assignment);

                    int numberOfSubmissions = 0;
                    int numberGraded = 0;

                    foreach (AssignmentTeam team in assignment.AssignmentTeams)
                    {
                        ActivityTeacherTableViewModel.SubmissionInfo submissionInfo = new ActivityTeacherTableViewModel.SubmissionInfo();

                        //This checks when something was submitted by the folder modify time it is imperative that they don't get modified except when a student submits something to that folder.
                        submissionInfo.Time = GetSubmissionTime(activeCourse.AbstractCourse as Course, assignment, team);

                        if (submissionInfo.Time != null)
                        {
                            numberOfSubmissions++;
                            //submissionInfo.LatePenaltyPercent = CalcualateLatePenaltyPercent(studioActivity, (TimeSpan)calculateLateness(studioActivity.AbstractAssignment.Category.Course, studioActivity, teamUser));
                        }

                        //Getting the student: only valid for non-team.
                        TeamMember student = (from a in team.Team.TeamMembers
                                              select a).FirstOrDefault();

                        //if team
                        if (assignment.HasTeams == true)
                        {
                            submissionInfo.isTeam = true;
                            submissionInfo.SubmitterID = team.TeamID;
                            submissionInfo.Name = team.Team.Name;
                        }
                        
                        //else student
                        else
                        {
                            submissionInfo.isTeam = false;
                            submissionInfo.SubmitterID = team.TeamID;
                            submissionInfo.Name = student.CourseUser.UserProfile.LastName + ", " + student.CourseUser.UserProfile.FirstName;
                        }

                        if ((from c in assignment.Scores where c.AssignmentTeamID == team.TeamID && c.Points >= 0 select c).FirstOrDefault() != null)
                        {
                            submissionInfo.Graded = true;
                            numberGraded++;
                        }
                        else
                        {
                            submissionInfo.Graded = false;
                        }
                        viewModel.SubmissionsInfo.Add(submissionInfo);
                    }

                    //This orders the list into alphabetical order
                    viewModel.SubmissionsInfo = (from c in viewModel.SubmissionsInfo orderby c.Name select c).ToList();
                    ViewBag.NumberOfSubmissions = numberOfSubmissions;
                    ViewBag.NumberGraded = numberGraded;

                    ViewBag.ExpectedSubmissionsAndGrades = assignment.AssignmentTeams.Count;

                    ViewBag.assignmentID = assignment.ID;
                    ViewBag.CategoryID = assignment.CategoryID;

                    List<Score> studentScores = assignment.Scores.ToList();

                    ViewBag.StudentScores = studentScores;

                    ViewBag.DueDate = assignment.DueDate;

                    return View(viewModel);
                }
                else
                {
                    throw new Exception("Tried to access AssignmentActivity of a different course than the active one");
                }
            }

            catch (Exception e)
            {
                throw new Exception("Failed ActivityTeacherTable", e);
            }
        }

        /// <summary>
        /// Takes the Icollectionof TeamUserMembers and returns a string with those members, sorted alphabetically in the format:
        /// "firstName1 lastName1, firstName2 lastName2 & firstName3 lastName3"
        /// </summary>
        private string createStringOfTeamMemebers(ICollection<TeamUserMember> members)
        {
            string returnVal = "";

            //Putting names in a list
            List<string> nameList = new List<string>();
            foreach (TeamUserMember tm in members)
            {
                nameList.Add(tm.Name);
            }
            //Sorting the list of names alphabetically
            nameList.Sort();

            //putting the names in "FirstName LastName" order
            for (int i = 0; i < nameList.Count; i++)
            {
                string[] name = nameList[i].Split(',');
                if (name.Count() == 2) //Only going to rearrange name if there was only 1 ','; otherwise i dont know how to handle them
                {
                    nameList[i] = name[1] + " " + name[0];
                }
            }

            //Compiling all the names into one string
            foreach (string s in nameList)
            {
                if (nameList.IndexOf(s) == nameList.Count() - 1) //Last name
                {
                    returnVal += s;
                }
                else if (nameList.IndexOf(s) == nameList.Count() - 2) //Second to last name
                {
                    returnVal += s + " & ";
                }
                else //Other names
                {
                    returnVal += s + ", ";
                }
            }
            return returnVal;
        }

        public ActionResult GetTeamMembers(int teamID)
        {
            try
            {
                //This is a nice way to just return a text as the view
                return this.Content(String.Join("; ", (
                    (from c in (db.TeamUsers.Find(teamID) as OldTeamMember).Team.Members select c.Name).ToArray())));
            }
            catch { }

            return this.Content("");
        }

        [CanGradeCourse]
        public ActionResult InlineReview(int assignmentID, int teamID)
        {
            try
            {
                Assignment assignment = db.Assignments.Find(assignmentID);
                AssignmentTeam team = db.AssignmentTeams.Find(teamID);
                if (assignment.Category.CourseID == activeCourse.AbstractCourseID && assignment.AssignmentTeams.Contains(team))
                {
                    Session.Add("CurrentAssignmentID", assignmentID);
                    Session.Add("TeamID", teamID);

                    //if publish file exists then teacher can not save as draft
                    bool canSaveAsDraft = !(new FileInfo(FileSystem.GetTeamUserPeerReview(false, activeCourse.AbstractCourse as Course, assignmentID, teamID)).Exists);

                    ViewBag.Assignment = assignment;
                    ViewBag.Team = team;

                    return View(new InlineReviewViewModel() { ReviewInterface = createEditInlineReviewSilverlightObject(canSaveAsDraft) });
                }
            }
            catch
            { }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult ViewInlineReview(int abstractAssignmentActivityId, int teamUserId)
        {
            try
            {
                AbstractAssignmentActivity activity = db.AbstractAssignmentActivities.Find(abstractAssignmentActivityId);
                TeamUserMember teamUser = db.TeamUsers.Find(teamUserId);

                ViewBag.activity = activity;
                ViewBag.TeamUser = teamUser;

                if (activity.AbstractAssignment.Category.CourseID == activeCourse.AbstractCourse.ID && teamUser.Contains(currentUser))
                {
                    Session.Add("CurrentActivityID", activity.ID);
                    Session.Add("TeamUserID", teamUser.ID);
                    return View("InlineReview", new InlineReviewViewModel() { ReviewInterface = ViewInlineReviewSilverlightObject() });
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
            List<Tuple<Score, AssignmentTeam, string>> scoreAndTeam = new List<Tuple<Score, AssignmentTeam, string>>();
            List<AssignmentTeam> teams = assignment.AssignmentTeams.ToList();

            //Sorting teams by team name or by last name if non-team assignment
            if (assignment.HasTeams)
            {
                teams.Sort((x, y) => string.Compare(x.Team.Name, y.Team.Name));
            }
            else
            {
                teams.Sort((x, y) => string.Compare(x.Team.TeamMembers.FirstOrDefault().CourseUser.UserProfile.LastName, y.Team.TeamMembers.FirstOrDefault().CourseUser.UserProfile.LastName));
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
                    if (score.AssignmentTeam.TeamID == team.TeamID)
                    {
                        scoreAndTeam.Add(new Tuple<Score, AssignmentTeam, string>(score, team, submissionTime));
                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch)
                {

                    scoreAndTeam.Add(new Tuple<Score, AssignmentTeam, string>(null, team, submissionTime));
                }
            }

            List<string[]> fileTypes = new List<string[]>();
            foreach(Deliverable d in assignment.Deliverables)
            {
                fileTypes.Add(GetFileExtensions((DeliverableType)d.Type));
            }
            ViewBag.filetypeList = fileTypes;
            ViewBag.ScoresAndTeams = scoreAndTeam;
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
    }
}

