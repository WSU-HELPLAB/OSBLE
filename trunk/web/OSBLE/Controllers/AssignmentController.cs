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

            // These are probably the nastiest set of queries in OSBLE.
            List<StudioAssignment> studioAssignments = db.StudioAssignments.Where(
                    sa =>
                        // Assignments must be from the active course
                        sa.Category.CourseID == ActiveCourse.AbstractCourseID &&
                        // There must be at least two activities in the assignment
                        sa.AssignmentActivities.Count() >= 2 &&
                        // The first activity must be a studio activity
                        (sa.AssignmentActivities.OrderBy(aa => aa.ReleaseDate).FirstOrDefault() is StudioActivity) &&
                        // The last activity must be a stop activity
                        (sa.AssignmentActivities.OrderByDescending(aa => aa.ReleaseDate).FirstOrDefault() is StopActivity)
                ).ToList();

            Dictionary<int, List<Tuple<bool, DateTime>>> submissionDictionary = new Dictionary<int, List<Tuple<bool, DateTime>>>();

            if (activeCourse.AbstractRole.CanSubmit)
            {
                //Get whether or not the students (CanSubmit) have submitted each deliverable for each submission activity
                var submissionActivities = (from c in studioAssignments
                                            from d in c.AssignmentActivities
                                            where d is SubmissionActivity
                                            select d as SubmissionActivity);

                foreach (SubmissionActivity activity in submissionActivities)
                {
                    List<Tuple<bool, DateTime>> submitted = new List<Tuple<bool, DateTime>>();

                    TeamUserMember teamUser = GetTeamUser(activity, currentUser);
                    if (teamUser == null)
                    {
                        //null teamUser must be because the student didn't exist when the assignment was created (hopefully)
                        teamUser = new UserMember() { UserProfileID = currentUser.ID };
                        activity.TeamUsers.Add(teamUser);
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
                    string folderLocation = FileSystem.GetTeamUserSubmissionFolder(true, activeCourse.AbstractCourse as Course, activity.ID, teamUser);

                    foreach (Deliverable deliverable in (activity.AbstractAssignment as StudioAssignment).Deliverables)
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

                    submissionDictionary.Add(activity.ID, submitted);
                }
            }

            // Past assignments are non-draft assignments whose final stop date has already passed.
            List<StudioAssignment> pastAssignments = studioAssignments.Where(
                    sa =>
                        !sa.IsDraft &&
                        sa.AssignmentActivities.OrderByDescending(aa => aa.ReleaseDate).FirstOrDefault().ReleaseDate <= DateTime.Now
                    )
                    .OrderBy(sa =>
                                sa.AssignmentActivities.OrderBy(aa => aa.ReleaseDate).FirstOrDefault().ReleaseDate)
                    .ToList();

            // Present assignments are any (non-draft) for which we are between the first start date and last end date.
            List<StudioAssignment> presentAssignments = studioAssignments.Where(
                     sa =>
                        !sa.IsDraft &&
                        sa.AssignmentActivities.OrderBy(aa => aa.ReleaseDate).FirstOrDefault().ReleaseDate <= DateTime.Now &&
                        sa.AssignmentActivities.OrderByDescending(aa => aa.ReleaseDate).FirstOrDefault().ReleaseDate > DateTime.Now
                    )
                    .OrderBy(sa =>
                                sa.AssignmentActivities.OrderBy(aa => aa.ReleaseDate).FirstOrDefault().ReleaseDate)
                    .ToList();

            // Future assignments are non-draft assignments whose start date has not yet happened.
            List<StudioAssignment> futureAssignments = studioAssignments.Where(
                    sa =>
                        !sa.IsDraft &&
                        sa.AssignmentActivities.OrderBy(aa => aa.ReleaseDate).FirstOrDefault().ReleaseDate > DateTime.Now
                    )
                    .OrderBy(sa =>
                            sa.AssignmentActivities.OrderBy(aa => aa.ReleaseDate).FirstOrDefault().ReleaseDate)
                    .ToList();

            List<StudioAssignment> draftAssignments = new List<StudioAssignment>();

            if (ActiveCourse.AbstractRole.CanModify)
            {
                // Draft assignments (viewable by instructor only) are assignments that have not yet been published to students
                draftAssignments = studioAssignments.Where(
                        sa =>
                            sa.IsDraft
                        )
                        .OrderBy(sa =>
                                sa.AssignmentActivities.OrderBy(aa => aa.ReleaseDate).FirstOrDefault().ReleaseDate)
                        .ToList();
            }

            KeyValuePair<int, int> listWithIndex = new KeyValuePair<int, int>(-1, -1);
            if (id != null)
            {
                int realID = (int)id;
                var assignment = (from c in studioAssignments where c.ID == realID select c).FirstOrDefault();

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
                StudioActivity studioActivity = db.AbstractAssignmentActivities.Find(id) as StudioActivity;

                StudioAssignment assignment = studioActivity.AbstractAssignment as StudioAssignment;

                if (studioActivity.AbstractAssignment.Category.Course == activeCourse.AbstractCourse)
                {
                    ActivityTeacherTableViewModel viewModel = new ActivityTeacherTableViewModel(studioActivity.AbstractAssignment, studioActivity);

                    int numberOfSubmissions = 0;
                    int numberGraded = 0;

                    foreach (TeamUserMember teamUser in studioActivity.TeamUsers)
                    {
                        ActivityTeacherTableViewModel.SubmissionInfo submissionInfo = new ActivityTeacherTableViewModel.SubmissionInfo();

                        //This checks when something was submitted by the folder modify time it is imperative that they don't get modified except when a student submits something to that folder.
                        submissionInfo.Time = GetSubmissionTime(activeCourse.AbstractCourse as Course, studioActivity, teamUser);

                        if (submissionInfo.Time != null)
                        {
                            numberOfSubmissions++;
                        }

                        //if team
                        if (teamUser is TeamMember)
                        {
                            submissionInfo.isTeam = true;
                            submissionInfo.SubmitterID = teamUser.ID;
                            submissionInfo.Name = (teamUser as TeamMember).Team.Name;
                        }

                            //if student
                        else
                        {
                            submissionInfo.isTeam = false;
                            submissionInfo.SubmitterID = teamUser.ID;
                            submissionInfo.Name = (teamUser as UserMember).UserProfile.LastName + ", " + (teamUser as UserMember).UserProfile.FirstName;
                        }

                        if ((from c in studioActivity.Scores where c.TeamUserMemberID == teamUser.ID && c.Points >= 0 select c).FirstOrDefault() != null)
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

                    ViewBag.ExpectedSubmissionsAndGrades = studioActivity.TeamUsers.Count;
                    ViewBag.activityID = studioActivity.ID;

                    var activities = (from c in assignment.AssignmentActivities orderby c.ReleaseDate select c).ToList();

                    ViewBag.DueDate = activities[activities.IndexOf(studioActivity) + 1].ReleaseDate;

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

        public ActionResult GetTeamMembers(int teamID)
        {
            try
            {
                //This is a nice way to just return a text as the view
                return this.Content(String.Join("; ", (
                    (from c in (db.TeamUsers.Find(teamID) as TeamMember).Team.Members select c.Name).ToArray())));
            }
            catch { }

            return this.Content("");
        }

        [CanGradeCourse]
        public ActionResult InlineReview(int assignmentActivityID, int teamUserID)
        {
            try
            {
                AbstractAssignmentActivity activity = db.AbstractAssignmentActivities.Find(assignmentActivityID);
                TeamUserMember teamUser = db.TeamUsers.Find(teamUserID);
                if (activity.AbstractAssignment.Category.CourseID == activeCourse.AbstractCourseID && activity.TeamUsers.Contains(teamUser))
                {
                    Session.Add("CurrentActivityID", assignmentActivityID);
                    Session.Add("TeamUserID", teamUserID);

                    //if publish file exists then teacher can not save as draft
                    bool canSaveAsDraft = !(new FileInfo(FileSystem.GetTeamUserPeerReview(false, activeCourse.AbstractCourse as Course, assignmentActivityID, teamUserID)).Exists);

                    ViewBag.Activity = activity;
                    ViewBag.TeamUser = teamUser;

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
    }
}
