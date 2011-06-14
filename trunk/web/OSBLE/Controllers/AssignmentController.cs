using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class AssignmentController : OSBLEController
    {
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

            // Past assignments are non-draft assignments whose final stop date has already passed.
            List<StudioAssignment> pastAssignments = studioAssignments.Where(
                    sa =>
                        !sa.IsDraft &&
                        sa.AssignmentActivities.OrderByDescending(aa => aa.ReleaseDate).FirstOrDefault().ReleaseDate <= DateTime.Now
                    )
                    .OrderBy(sa =>
                                sa.AssignmentActivities.OrderBy(aa=>aa.ReleaseDate).FirstOrDefault().ReleaseDate)
                    .ToList();

            // Present assignments are any (non-draft) for which we are between the first start date and last end date.
            List<StudioAssignment> presentAssignments = studioAssignments.Where(
                     sa =>
                        !sa.IsDraft &&
                        sa.AssignmentActivities.OrderBy(aa=> aa.ReleaseDate).FirstOrDefault().ReleaseDate <= DateTime.Now &&
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

            return View();
        }

    }
}
