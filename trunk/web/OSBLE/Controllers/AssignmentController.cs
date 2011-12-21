﻿using System;
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
            //verify that the user attempting a delete owns this course
            if (!activeCourse.AbstractRole.CanModify)
            {
                return RedirectToAction("Index");
            }

            AbstractAssignment assignment = db.StudioAssignments.Find(id);
            if (assignment == null)
            {
                return RedirectToAction("Index");
            }
            return View(assignment);
        }

        [CanModifyCourse]
        [HttpPost]
        public ActionResult Delete(StudioAssignment assignment)
        {

            //verify that the user attempting a delete owns this course
            if (!activeCourse.AbstractRole.CanModify)
            {
                return RedirectToAction("Index");
            }

            //if the user didn't click "continue" get us out of here
            if (!Request.Form.AllKeys.Contains("continue"))
            {
                return RedirectToAction("Index");
            }

            assignment = db.StudioAssignments.Find(assignment.ID);
            if (assignment == null)
            {
                return RedirectToAction("Index");
            }
            
            //delete team users from the activities
            int i = 0;
            foreach(AbstractAssignmentActivity activity in assignment.AssignmentActivities)
            {
                i = 0;
                while (activity.TeamUsers.Count > 0)
                {
                    db.TeamUsers.Remove(activity.TeamUsers.ElementAt(i));
                }
            }
            db.SaveChanges();

            //Delete event data.  Magic string alert (taken from BasicAssignmentController).
            //Because events don't reference any particular model, we can't just find all
            //events that relate to the current assignemnt.  As a workaround, I figure that
            //the Description property of the event data should be specific enough to identify
            //and delete related elements.
            string descrption = "https://osble.org/Assignment?id=" + assignment.ID;
            List<Event> events = (from evt in db.Events
                                  where evt.Description.Contains(descrption)
                                  select evt).ToList();
            foreach(Event evt in events)
            {
                db.Events.Remove(evt);
            }

            //clear all assignments from the file system
            FileSystem.EmptyFolder(FileSystem.GetAssignmentsFolder(activeCourse.AbstractCourse as Course));

            db.StudioAssignments.Remove(assignment);
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

            List<Assignment> Assignments = (from assignment in db.Assignments
                                            where assignment.Category.CourseID == ActiveCourse.AbstractCourseID
                                            orderby assignment.ReleaseDate
                                            select assignment).ToList();

            Dictionary<int, List<Tuple<bool, DateTime>>> submissionDictionary = new Dictionary<int, List<Tuple<bool, DateTime>>>();

            if (activeCourse.AbstractRole.CanSubmit)
            {
                //Get whether or not the students (CanSubmit) have submitted each deliverable for each submission activity
                var submissionAssignments = (from c in Assignments
                                             where c.HasDeliverables == true
                                             select c);

                foreach (Assignment assignment in submissionAssignments)
                {
                    List<Tuple<bool, DateTime>> submitted = new List<Tuple<bool, DateTime>>();

                    AssignmentTeam assignmentTeam = GetAssignmentTeam(assignment, currentUser);
                    if (assignmentTeam == null)
                    {
                        //null assignmentTeam must be because the student didn't exist when the assignment was created (hopefully)
                        CourseUser courseUser = (from user in db.CourseUsers
                                                 where user.UserProfileID == currentUser.ID
                                                 select user).FirstOrDefault();
                        
                        TeamMember userMember = new TeamMember()
                        {
                            CourseUser = courseUser,
                            CourseUserID = courseUser.ID,
                        };

                        Team team = new Team();
                        team.Name = userMember.CourseUser.UserProfile.LastName + "," + userMember.CourseUser.UserProfile.FirstName;
                        team.TeamMembers.Add(userMember);

                        db.Teams.Add(team);
                        db.SaveChanges();

                        assignmentTeam = new AssignmentTeam() { AssignmentID = assignment.ID, Assignment = assignment, Team = team, TeamID = team.ID };

                        assignment.AssignmentTeams.Add(assignmentTeam);
                        try
                        {
                            db.SaveChanges();
                        }
                        catch (DbEntityValidationException dbEx)
                        {
                            foreach (var validationErrors in dbEx.EntityValidationErrors)
                            {
                                foreach (var validationError in validationErrors.ValidationErrors)
                                {
                                    Trace.TraceInformation("Property: {0} Error: {1}", validationError.PropertyName, validationError.ErrorMessage);
                                }
                            }
                        }
                        
                    }
                    string folderLocation = FileSystem.GetTeamUserSubmissionFolder(true, activeCourse.AbstractCourse as Course, assignment.ID, assignmentTeam);

                    foreach (Deliverable deliverable in (assignment.Deliverables))
                    {
                        string[] allowedExtensions = GetFileExtensions((DeliverableType)deliverable.Type);

                        bool found = false;

                        DateTime timeSubmitted = new DateTime();

                        foreach (string extension in allowedExtensions)
                        {
                            FileInfo fileInfo = new FileInfo(Path.Combine(folderLocation, deliverable.Name + extension));
                            if (fileInfo.Exists)
                            {
                                found = true;
                                timeSubmitted = fileInfo.LastWriteTime;
                                break;
                            }
                        }
                        submitted.Add(new Tuple<bool, DateTime>(found, timeSubmitted));
                    }

                    submissionDictionary.Add(assignment.ID, submitted);
                }
            }

            
            // Past assignments are non-draft assignments whose final stop date has already passed.
            List<Assignment> pastAssignments = (from assignment in db.Assignments
                                                where !assignment.IsDraft &&
                                                assignment.DueDate <= DateTime.Now
                                                orderby assignment.DueDate
                                                select assignment).ToList();

            // Present assignments are any (non-draft) for which we are between the first start date and last end date.
            List<Assignment> presentAssignments = (from assignment in db.Assignments
                                                   where !assignment.IsDraft &&
                                                   assignment.DueDate > DateTime.Now &&
                                                   assignment.ReleaseDate <= DateTime.Now
                                                   orderby assignment.DueDate
                                                   select assignment).ToList();

            // Future assignments are non-draft assignments whose start date has not yet happened.
            List<Assignment> futureAssignments = (from assignment in db.Assignments
                                                  where !assignment.IsDraft &&
                                                  assignment.ReleaseDate > DateTime.Now
                                                  orderby assignment.ReleaseDate
                                                  select assignment).ToList();

            List<Assignment> draftAssignments = new List<Assignment>();

            if (ActiveCourse.AbstractRole.CanModify)
            {
                // Draft assignments (viewable by instructor only) are assignments that have not yet been published to students
                draftAssignments = (from assignment in db.Assignments
                                    where assignment.IsDraft
                                    orderby assignment.ReleaseDate
                                    select assignment).ToList();
            }

            KeyValuePair<int, int> listWithIndex = new KeyValuePair<int, int>(-1, -1);
            if (id != null)
            {
                int realID = (int)id;
                var assignment = (from c in Assignments where c.ID == realID select c).FirstOrDefault();

                if (pastAssignments.Contains(assignment))
                {
                    listWithIndex = new KeyValuePair<int, int>(0, pastAssignments.IndexOf(assignment));
                }
                else if (presentAssignments.Contains(assignment))
                {
                    listWithIndex = new KeyValuePair<int, int>(1, presentAssignments.IndexOf(assignment));
                }
                else if (futureAssignments.Contains(assignment))
                {
                    listWithIndex = new KeyValuePair<int, int>(2, futureAssignments.IndexOf(assignment));
                }
            }
            ViewBag.DefaultItemOpened = listWithIndex;
            ViewBag.PastAssignments = pastAssignments;
            ViewBag.PresentAssignments = presentAssignments;
            ViewBag.FutureAssignments = futureAssignments;
            ViewBag.DraftAssignments = draftAssignments;
            ViewBag.CanSubmit = activeCourse.AbstractRole.CanSubmit;
            ViewBag.SubmissionDictionary = submissionDictionary;

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
        
        public ActionResult AssignmentView (int id)
        {
            Assignment assignment = db.Assignments.Find(id);
            List<Score> scores = assignment.Scores.ToList();
            List<Tuple<string, AssignmentTeam>> scoreAndTeam = new List<Tuple<string, AssignmentTeam>>();

            List<AssignmentTeam> teams = assignment.AssignmentTeams.ToList();
            foreach (AssignmentTeam team in teams)
            {
                bool foundMatch = false;
                foreach (Score score in scores)
                {
                    if (score.AssignmentTeam.TeamID == team.TeamID)
                    {
                        scoreAndTeam.Add(new Tuple<string, AssignmentTeam>(score.Points.ToString(), team));
                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch)
                {
                    scoreAndTeam.Add(new Tuple<string, AssignmentTeam>("NG", team));
                }
            }

            ViewBag.ScoresAndTeams = scoreAndTeam;
            return View(assignment);
        }
    }
}
