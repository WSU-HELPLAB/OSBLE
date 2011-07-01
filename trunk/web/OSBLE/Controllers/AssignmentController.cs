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
                        sa.Category.CourseID == ActiveCourse.CourseID &&
                        // There must be at least two activities in the assignment
                        sa.AssignmentActivities.Count() >= 2 &&
                        // The first activity must be a studio activity
                        (sa.AssignmentActivities.OrderBy(aa => aa.ReleaseDate).FirstOrDefault() is StudioActivity) &&
                        // The last activity must be a stop activity
                        (sa.AssignmentActivities.OrderByDescending(aa => aa.ReleaseDate).FirstOrDefault() is StopActivity)
                ).ToList();

            Dictionary<int, List<Tuple<bool, DateTime>>> submissionDictionary = new Dictionary<int, List<Tuple<bool, DateTime>>>();

            if (activeCourse.CourseRole.CanSubmit)
            {
                //Get whether or not the students (CanSubmit) have submitted each deliverable for each submission activity
                var submissionActivities = (from c in studioAssignments
                                            from d in c.AssignmentActivities
                                            where d is SubmissionActivity
                                            select d as SubmissionActivity);

                foreach (SubmissionActivity activity in submissionActivities)
                {
                    List<Tuple<bool, DateTime>> submitted = new List<Tuple<bool, DateTime>>();

                    string folderLocation = FileSystem.GetSubmissionFolder(activeCourse.Course as Course, activity.ID, GetTeamorUserForCurrentUser(activity));

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
                                timeSubmitted = fileInfo.CreationTime;
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

            if (ActiveCourse.CourseRole.CanModify)
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
            ViewBag.CanSubmit = activeCourse.CourseRole.CanSubmit;
            ViewBag.SubmissionDictionary = submissionDictionary;

            ViewBag.DeliverableTypes = GetListOfDeliverableTypes();
            ViewBag.Submitted = false;

            return View();
        }

        [ChildActionOnly]
        [CanModifyCourse]
        public ActionResult ActivityTeacherTable(int id)
        {
            try
            {
                StudioActivity studioActivity = db.AbstractAssignmentActivity.Find(id) as StudioActivity;

                StudioAssignment assignment = studioActivity.AbstractAssignment as StudioAssignment;

                if (studioActivity.AbstractAssignment.Category.Course == activeCourse.Course)
                {
                    //FileSystem.GetAssignmentActivitySubmissionFolder(activeCourse.Course as Course, studioActivity.ID);

                    ActivityTeacherTableViewModel viewModel = new ActivityTeacherTableViewModel();

                    IEnumerable<TeamUser> submitters;

                    if (studioActivity.isTeam)
                    {
                        submitters = (from c in studioActivity.Teams select new TeamUser() { Team = c }).ToList();

                        //linq barfs on enums in entity framework so got to set it manually
                        foreach (TeamUser teamUser in submitters)
                        {
                            teamUser.TeamOrUser = TeamOrUser.Team;
                        }
                    }
                    else
                    {
                        submitters = (from c in db.CoursesUsers where c.CourseID == activeCourse.CourseID && c.CourseRole.CanSubmit select new TeamUser { UserProfile = c.UserProfile }).ToList();

                        //linq barfs on enums in entity framework so got to set it manually
                        foreach (TeamUser teamUser in submitters)
                        {
                            teamUser.TeamOrUser = TeamOrUser.User;
                        }
                    }

                    foreach (TeamUser teamUser in submitters)
                    {
                        ActivityTeacherTableViewModel.SubmissionInfo info = new ActivityTeacherTableViewModel.SubmissionInfo();

                        DirectoryInfo submissionFolder = new DirectoryInfo(FileSystem.GetSubmissionFolder(activeCourse.Course as Course, studioActivity.ID, new TeamUser() { TeamOrUser = TeamOrUser.User, UserProfile = teamUser.UserProfile }));

                        //if team
                        if (TeamOrUser.Team == teamUser.TeamOrUser)
                        {
                            info.isTeam = true;
                            info.SubmitterID = teamUser.Team.ID;
                            info.Name = teamUser.Team.Name;
                        }

                            //if student
                        else
                        {
                            info.isTeam = false;
                            info.SubmitterID = teamUser.UserProfile.ID;
                            info.Name = teamUser.UserProfile.LastName + ", " + teamUser.UserProfile.FirstName;
                        }

                        if (submissionFolder.Exists)
                        {
                            info.Time = submissionFolder.LastWriteTime;
                        }
                        else
                        {
                            info.Time = null;
                        }

                        //will need to fix this
                        info.Graded = false;

                        viewModel.SubmissionsInfo.Add(info);
                    }

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
    }
}