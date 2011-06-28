using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

namespace OSBLE.Controllers
{
    [Authorize]
    [CanSubmitAssignments]
    public class SubmissionController : OSBLEController
    {
        private OSBLEContext db = new OSBLEContext();

        //
        // GET: /Submission/Create

        public ActionResult Create(int? id)
        {
            if (id != null)
            {
                AbstractAssignmentActivity activity = db.AbstractAssignmentActivity.Find(id);

                if (activity != null)
                {
                    AbstractAssignment assignment = db.AbstractAssignments.Find(activity.AbstractAssignmentID);

                    if (assignment != null && assignment.Category.CourseID == activeCourse.CourseID && activeCourse.CourseRole.CanSubmit == true && assignment is StudioAssignment)
                    {
                        setViewBagDeliverables((assignment as StudioAssignment).Deliverables);

                        return View();
                    }
                }
            }

            throw new Exception();
        }

        private void setViewBagDeliverables(ICollection<Deliverable> deliverables)
        {
            Dictionary<Deliverable, string[]> allowedFileExtensions = new Dictionary<Deliverable, string[]>();

            foreach (Deliverable deliverable in deliverables)
            {
                allowedFileExtensions.Add(deliverable, GetFileExtensions((DeliverableType)deliverable.Type));
            }

            ViewBag.Deliverables = allowedFileExtensions;
        }

        public ActionResult SubmittedSuccessfully()
        {
            return View();
        }

        //
        // POST: /Submission/Create

        [HttpPost]
        public ActionResult Create(int? id, IEnumerable<HttpPostedFileBase> files)
        {
            if (id != null)
            {
                AbstractAssignmentActivity activity = db.AbstractAssignmentActivity.Find(id);

                if (activity as SubmissionActivity != null)
                {
                    AbstractAssignment assignment = db.AbstractAssignments.Find(activity.AbstractAssignmentID);

                    List<Deliverable> deliverables = new List<Deliverable>((assignment as StudioAssignment).Deliverables);

                    if (assignment != null && assignment.Category.CourseID == activeCourse.CourseID && activeCourse.CourseRole.CanSubmit == true && assignment is StudioAssignment)
                    {
                        TeamMember teamMember = new TeamMember();
                        if ((activity as SubmissionActivity).isTeam)
                        {
                            teamMember.TeamUser = TeamsOrUsers.Team;
                            TeamMember temp = null;
                            foreach (Team team in (activity as SubmissionActivity).Teams)
                            {
                                temp = findTeamMember(team.Members, currentUser.ID);
                                if (temp != null)
                                {
                                    teamMember.Team = team;
                                    teamMember.TeamID = team.ID;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            teamMember.TeamUser = TeamsOrUsers.User;
                            teamMember.User = currentUser;
                            teamMember.UserProfileID = currentUser.ID;
                        }

                        int i = 0;
                        foreach (var file in files)
                        {
                            if (file != null && file.ContentLength > 0)
                            {
                                DeliverableType type = (DeliverableType)deliverables[i].Type;
                                string fileName = Path.GetFileName(file.FileName);
                                string extension = Path.GetExtension(file.FileName);

                                string[] allowFileExtensions = GetFileExtensions(type);

                                if (allowFileExtensions.Contains(extension))
                                {
                                    var path = Path.Combine(FileSystem.GetSubmissionFolder(activeCourse.Course as Course, (int)id, teamMember), deliverables[i].Name + extension);
                                    file.SaveAs(path);
                                }
                                else
                                {
                                    ModelState.AddModelError("FileExtensionMatch", "The file " + fileName + " does not have an allowed extension please convert the file to the correct type");
                                    setViewBagDeliverables((assignment as StudioAssignment).Deliverables);
                                    return View();
                                }
                            }
                            i++;
                        }
                        return RedirectToAction("SubmittedSuccessfully");
                    }
                }
            }

            return Create(id);
        }

        private TeamMember findTeamMember(ICollection<TeamMember> members, int userProfileID)
        {
            foreach (TeamMember member in members)
            {
                if (member.TeamUser == TeamsOrUsers.Team)
                {
                    TeamMember teamMember = findTeamMember(member.Team.Members, userProfileID);
                    if (teamMember != null)
                    {
                        return member;
                    }
                }
                else
                {
                    if (member.UserProfileID == userProfileID)
                    {
                        return member;
                    }
                }
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}