using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;

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

        //
        // GET: /Assignment/

        public ActionResult Index()
        {
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
            //This can be used to simulate a long load time
            /*Int64 i = 0;
            while (i < 2000000000)
            {
                i++;
            }*/

            try
            {
                StudioActivity studioActivity = db.AbstractAssignmentActivities.Find(id) as StudioActivity;

                StudioAssignment assignment = studioActivity.AbstractAssignment as StudioAssignment;

                if (studioActivity.AbstractAssignment.Category.Course == activeCourse.AbstractCourse)
                {
                    //FileSystem.GetAssignmentActivitySubmissionFolder(activeCourse.Course as Course, studioActivity.ID);

                    ActivityTeacherTableViewModel viewModel = new ActivityTeacherTableViewModel();

                    int numberOfSubmissions = 0;
                    int numberGraded = 0;

                    foreach (TeamUserMember teamUser in studioActivity.TeamUsers)
                    {
                        ActivityTeacherTableViewModel.SubmissionInfo info = new ActivityTeacherTableViewModel.SubmissionInfo();

                        //This checks when something was submitted by the folder modify time it is imperative that they don't get modified except when a student submits something to that folder.
                        DirectoryInfo submissionFolder = new DirectoryInfo(FileSystem.GetTeamUserSubmissionFolder(false, activeCourse.AbstractCourse as Course, studioActivity.ID, teamUser));

                        //if team
                        if (teamUser is TeamMember)
                        {
                            info.isTeam = true;
                            info.SubmitterID = teamUser.ID;
                            info.Name = (teamUser as TeamMember).Team.Name;
                        }

                            //if student
                        else
                        {
                            info.isTeam = false;
                            info.SubmitterID = teamUser.ID;
                            info.Name = (teamUser as UserMember).UserProfile.LastName + ", " + (teamUser as UserMember).UserProfile.FirstName;
                        }

                        if (submissionFolder.Exists)
                        {
                            //unfortunately LastWriteTime for a directory does not take into account it's file or
                            //sub directories and these we need to check to see when the last file was written too.
                            info.Time = submissionFolder.LastWriteTime;
                            foreach (FileInfo file in submissionFolder.GetFiles())
                            {
                                if (file.LastWriteTime > info.Time)
                                {
                                    info.Time = file.LastWriteTime;
                                }
                            }
                            numberOfSubmissions++;
                        }
                        else
                        {
                            info.Time = null;
                        }

                        if ((from c in studioActivity.Scores where c.TeamUserMemberID == teamUser.ID select c).FirstOrDefault() != null)
                        {
                            info.Graded = true;
                            numberGraded++;
                        }
                        else
                        {
                            info.Graded = false;
                        }
                        viewModel.SubmissionsInfo.Add(info);
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

        public ActionResult InlineReview(int assignmentActivityID, int teamUserID)
        {
            return View();
        }
    }
}