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
using OSBLE.Controllers;
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


        [CanModifyCourse]
        public void ConvertAllDrafts(int assignmentActivityID)
        {
            var activity = (from a in db.AbstractAssignmentActivities
                           where a.ID == assignmentActivityID
                           select a).FirstOrDefault();

            if (activity != null) //Turning all IsPublished values to true and changing their publish time
            {
                List<RubricEvaluation> reList = (from re in db.RubricEvaluations
                                                 where re.AbstractAssignmentActivityID == assignmentActivityID
                                                 select re as RubricEvaluation).ToList();

                foreach (RubricEvaluation re in reList)
                {
                    if (re.IsPublished == false) //Only publish if its not already published
                    {
                        re.IsPublished = true;
                        re.DatePublished = DateTime.Now;
                        PublishGrade(assignmentActivityID, re.RecipientID);
                    }
                }
                db.SaveChanges();
            }
        }

        //This is to be used with Ajax
        [CanModifyCourse]
        public ActionResult ActivityTeacherTable(int id)
        {
            try
            {
                StudioActivity studioActivity = db.AbstractAssignmentActivities.Find(id) as StudioActivity;

                StudioAssignment assignment = studioActivity.AbstractAssignment as StudioAssignment;

                bool hasRubric = studioActivity is SubmissionActivity && assignment.RubricID != null && assignment.RubricID != 0;

                if (studioActivity.AbstractAssignment.Category.Course == activeCourse.AbstractCourse)
                {
                    ActivityTeacherTableViewModel viewModel = new ActivityTeacherTableViewModel(studioActivity.AbstractAssignment, studioActivity);

                    int numberOfSubmissions = 0;
                    int numberGraded = 0;
                    int numberOfDrafts = 0;
                    int numberOfPublish = 0;

                    foreach (TeamUserMember teamUser in studioActivity.TeamUsers)
                    {
                        ActivityTeacherTableViewModel.SubmissionInfo submissionInfo = new ActivityTeacherTableViewModel.SubmissionInfo();
                        if (hasRubric) //Setting the publish time of the draft, if there is one.
                        {
                            var temp = (from re in db.RubricEvaluations
                                              where re.RecipientID == teamUser.ID &&
                                              re.AbstractAssignmentActivityID == studioActivity.ID
                                              select re).FirstOrDefault();
                            if (temp != null) //There is a RE. 
                            {
                                if ((temp as RubricEvaluation).IsPublished == false) //not a published RE, so its  a draft
                                {
                                    submissionInfo.DraftSaveTime = (temp as RubricEvaluation).DatePublished;
                                    numberOfDrafts++;
                                }
                                else
                                {
                                    numberOfPublish++;
                                }
                            }
                        }

                        //This checks when something was submitted by the folder modify time it is imperative that they don't get modified except when a student submits something to that folder.
                        submissionInfo.Time = GetSubmissionTime(activeCourse.AbstractCourse as Course, studioActivity, teamUser);


                        //Getting the score in the db manual LatePenaltyPercent
                        var tempScore = (from s in db.Scores
                                         where s.TeamUserMemberID == teamUser.ID
                                         select s).FirstOrDefault();

                        if (tempScore != null) //Only giving ManualLatePenaltyPercent a value if there is something there, otherwise it gets -1 (the default)
                        {
                            submissionInfo.ManualLatePenaltyPercent = (tempScore as Score).ManualLatePenaltyPercent;
                        }
                        else
                        {
                            submissionInfo.ManualLatePenaltyPercent = -1;
                        }

                        if (submissionInfo.Time != null)
                        {
                            numberOfSubmissions++;
                            submissionInfo.LatePenaltyPercent = CalcualateLatePenaltyPercent(studioActivity, (TimeSpan)calculateLateness(studioActivity.AbstractAssignment.Category.Course, studioActivity, teamUser));
                            //Calculated the late penalty percent, should save to DB if there is a difference.
                            if ((tempScore as Score).LatePenaltyPercent != submissionInfo.LatePenaltyPercent)
                            {
                                (tempScore as Score).LatePenaltyPercent = submissionInfo.LatePenaltyPercent;
                                db.SaveChanges();
                            }

                        }

                        //if team
                        if (teamUser is TeamMember)
                        {
                            submissionInfo.isTeam = true;
                            submissionInfo.SubmitterID = teamUser.ID;
                            submissionInfo.TeamID = (teamUser as TeamMember).TeamID;
                            submissionInfo.Name = (teamUser as TeamMember).Team.Name;
                            submissionInfo.TeamList = createStringOfTeamMemebers((teamUser as TeamMember).Team.Members);
                        }

                        //else student
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

                    if (studioActivity.isTeam == true)
                    {
                        //"Grades for x of y students have been saved in draft [publish all]"
                        //"Grades for x of y students have been published."
                        ViewBag.DraftMsg1 = "Grades for " + numberOfDrafts.ToString() + " of " + studioActivity.TeamUsers.Count.ToString() + " teams have been saved in draft.";
                        ViewBag.DraftMsg2 = "Grades for " + numberOfPublish.ToString() + " of " + studioActivity.TeamUsers.Count.ToString() + " teams have been published.";
                    }
                    else //not a team
                    {
                        ViewBag.DraftMsg1 = "Grades for " + numberOfDrafts.ToString() + " of " + studioActivity.TeamUsers.Count.ToString() + " students have been saved in draft.";
                        ViewBag.DraftMsg2 = "Grades for " + numberOfPublish.ToString() + " of " + studioActivity.TeamUsers.Count.ToString() + " students have been published.";
                    }

                    //This orders the list into alphabetical order
                    viewModel.SubmissionsInfo = (from c in viewModel.SubmissionsInfo orderby c.Name select c).ToList();
                    ViewBag.NumberOfSubmissions = numberOfSubmissions;
                    ViewBag.NumberGraded = numberGraded;
                    ViewBag.ExpectedSubmissionsAndGrades = studioActivity.TeamUsers.Count;
                    ViewBag.activityID = studioActivity.ID;
                    ViewBag.CategoryID = studioActivity.AbstractAssignment.CategoryID;


                    List<Score> studentScores = (from scores in db.Scores
                                                 where scores.AssignmentActivityID == studioActivity.ID
                                                 select scores).ToList();

                    ViewBag.StudentScores = studentScores;


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

        /// <summary>
        /// Changes ManaulLatePenalty value into the value passed in, for the user(s) corrisponding to the ID sent in.
        /// </summary>
        public void changeManualLatePenalty(double value, int teamUserMemeberID, int assignmentActivityID)
        {
            if (ModelState.IsValid)
            {
                //Look up score given the ID. If there is one adust it appropriately, otherwise create a new one
                var tempScore = (from s in db.Scores
                                 where s.TeamUserMemberID == teamUserMemeberID
                                 select s);

                Score mScore = null;
                if (tempScore.Count() == 0) //No score. Adding a new one to the db
                {
                    mScore = new Score();
                    mScore.PublishedDate = DateTime.Now;
                    mScore.AssignmentActivityID = assignmentActivityID;
                    mScore.Points = -1;
                    mScore.TeamUserMemberID = teamUserMemeberID;
                    mScore.ManualLatePenaltyPercent = value;
                    db.Scores.Add(mScore);
                    db.SaveChanges();
                }
                else //Score already in place
                {
                    //Assigning mScore the score from the db. Adjusting the late penalty, saving the changes to
                    //db, and finally updating the grade by running  ModifyGrade on the rawpoints. 
                    mScore = tempScore.FirstOrDefault();
                    mScore.ManualLatePenaltyPercent = value; 
                    db.SaveChanges(); //save mScore changes

                    //getting assignment activity
                    AbstractAssignmentActivity assignmentActivity = (from aa in db.AbstractAssignmentActivities
                                             where aa.ID == assignmentActivityID
                                             select aa).FirstOrDefault();

                    //getting the team user member's user ID to use with ModifiyGrade further down
                    var tum = (from tumember in db.TeamUsers
                               where tumember.ID == teamUserMemeberID
                               select tumember).FirstOrDefault();

                    string userIdentification = ""; //string for holdign the user identification
                    if (assignmentActivity.isTeam) //handle like team
                    {
                        TeamMember tm = tum as TeamMember;
                        if (tm.Team.Members.Count() > 0)
                        {
                            UserMember um = tm.Team.Members.First() as UserMember;
                            int userID = um.UserProfileID;
                            UserProfile up = (from UP in db.UserProfiles
                                              where UP.ID == userID
                                              select UP).FirstOrDefault();
                            userIdentification = up.Identification;
                            
                        }
                    }
                    else
                    {
                        UserMember um = tum as UserMember;
                        int userID = um.UserProfileID;
                        UserProfile up = (from UP in db.UserProfiles
                                          where UP.ID == userID
                                          select UP).FirstOrDefault();
                        userIdentification = up.Identification;
                    }

                    if (mScore.RawPoints != -1) //Only actually modifiy their grade if their raw points has a value. 
                    {
                        ModifyGrade(mScore.RawPoints, userIdentification, assignmentActivityID); //Update the grade
                    }
                    
                }
            }
        }
    }
}
