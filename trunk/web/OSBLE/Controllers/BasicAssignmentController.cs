using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
using Newtonsoft.Json;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.ViewModels;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class BasicAssignmentController : OSBLEController
    {
        public class SerializableTeamMembers
        {
            public int UserID { get; set; }

            public int TeamID { get; set; }

            public string Name { get; set; }

            public int Section { get; set; }

            public bool IsModerator { get; set; }

            public bool Subbmitted { get; set; }

            public int InTeamID { get; set; }

            public string InTeamName { get; set; }
        }

        public BasicAssignmentController()
            : base()
        {
            ViewBag.CurrentTab = "Assignments";
        }

        //
        // GET: /Assignment/

        /*
        public ViewResult Index()
        {
            //This is what it was but no longer works
            var abstractgradables = db.AbstractGradables.ToList();
            return View(abstractgradables);
            //return View();
        }*/

        //
        // GET: /Assignment/Details/5

        /*
        public ViewResult Details(int id)
        {
            SubmissionActivity assignment = db.AbstractGradables.Find(id) as SubmissionActivity;
            return View(assignment);
        }
        */
        //
        // GET: /Assignment/Create

        [CanModifyCourse]
        public ActionResult Create()
        {
            //we create a basic assignment that is a StudioAssignment with a submission and a stop
            List<Deliverable> Deliverables = new List<Deliverable>();
            BasicAssignmentViewModel viewModel = new BasicAssignmentViewModel();

            // Copy default Late Policy settings
            Course active = activeCourse.Course as Course;
            viewModel.Submission.HoursLatePerPercentPenalty = active.HoursLatePerPercentPenalty;
            viewModel.Submission.HoursLateUntilZero = active.HoursLateUntilZero;
            viewModel.Submission.PercentPenalty = active.PercentPenalty;
            viewModel.Submission.MinutesLateWithNoPenalty = active.MinutesLateWithNoPenalty;

            viewModel.TeamCreation = new SilverlightObject
            {
                CSSId = "team_creation_silverlight",
                XapName = "TeamCreation",
                Width = "850",
                Height = "610",
                OnLoaded = "SLObjectLoaded",
                Parameters = new Dictionary<string, string>()
                {
                }
            };

            List<SerializableTeamMembers> teamMembmers = new List<SerializableTeamMembers>();

            var couresesUsers = (from c in db.CoursesUsers
                                 where c.CourseID == activeCourse.CourseID
                                 && (c.CourseRole.ID == (int)CourseRole.OSBLERoles.Moderator
                                 || c.CourseRole.ID == (int)CourseRole.OSBLERoles.Student)
                                 select c).ToList();

            int i = 0;
            foreach (CoursesUsers cu in couresesUsers)
            {
                SerializableTeamMembers teamMember = new SerializableTeamMembers();
                teamMember.IsModerator = cu.CourseRole.ID == (int)CourseRole.OSBLERoles.Moderator;
                teamMember.Name = cu.UserProfile.FirstName + " " + cu.UserProfile.LastName;
                teamMember.Section = cu.Section;

                ///////////////////////TEST/////////////////////////
                if (i % 2 == 0)
                {
                    teamMember.Subbmitted = true;
                }
                i++;
                ///////////////////////////////////////////////////

                teamMembmers.Add(teamMember);
            }

            viewModel.SerializedTeamMembersJSON = viewModel.TeamCreation.Parameters["teamMembers"] = Uri.EscapeDataString(JsonConvert.SerializeObject(teamMembmers));

            ViewBag.Categories = new SelectList(db.Categories, "ID", "Name");
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");
            ViewBag.Deliverables = Deliverables;
            return View(viewModel);
        }

        //
        // POST: /Assignment/Create

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Create(BasicAssignmentViewModel basic)
        {
            if (basic.Submission.ReleaseDate >= basic.Stop.ReleaseDate)
            {
                ModelState.AddModelError("time", "The due date must come after the release date");
            }
            if (ModelState.IsValid)
            {
                db.BasicAssignments.Add(basic.Assignment);

                SubmissionActivity submission = new SubmissionActivity();
                StopActivity stop = new StopActivity();

                submission.ReleaseDate = basic.Submission.ReleaseDate;
                submission.Name = basic.Submission.Name;
                submission.PointsPossible = basic.Submission.PointsPossible;

                submission.HoursLatePerPercentPenalty = basic.Submission.HoursLatePerPercentPenalty;
                submission.HoursLateUntilZero = basic.Submission.HoursLateUntilZero;
                submission.PercentPenalty = basic.Submission.PercentPenalty;
                submission.MinutesLateWithNoPenalty = basic.Submission.MinutesLateWithNoPenalty;

                stop.ReleaseDate = basic.Stop.ReleaseDate;

                basic.Assignment.AssignmentActivities.Add(submission);
                basic.Assignment.AssignmentActivities.Add(stop);

                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(db.Categories, "ID", "Name", basic.Assignment.CategoryID);
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");

            return View(basic);
        }

        //
        // GET: /Assignment/Edit/5

        [CanModifyCourse]
        public ActionResult Edit(int id)
        {
            BasicAssignmentViewModel basicViewModel = new BasicAssignmentViewModel();
            BasicAssignment assignment = db.BasicAssignments.Find(id);

            if (assignment.Category.CourseID != ActiveCourse.CourseID)
            {
                return RedirectToAction("Index", "Home");
            }

            basicViewModel.Assignment = assignment;
            basicViewModel.Submission = assignment.AssignmentActivities.Where(aa => aa is SubmissionActivity).First() as SubmissionActivity;
            basicViewModel.Stop = assignment.AssignmentActivities.Where(aa => aa is StopActivity).FirstOrDefault() as StopActivity;

            ViewBag.Categories = new SelectList(db.Categories, "ID", "Name", assignment.CategoryID);
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");

            return View(basicViewModel);
        }

        [HttpPost]
        [CanModifyCourse]
        public ActionResult Edit(BasicAssignmentViewModel basic)
        {
            BasicAssignment assignment = db.BasicAssignments.Find(basic.Assignment.ID);

            // Make sure assignment to update belongs to this course.
            if (assignment.Category.CourseID != ActiveCourse.CourseID)
            {
                return RedirectToAction("Index", "Home");
            }

            // If category updated, ensure it belongs to the course as well.
            if (basic.Assignment.CategoryID != assignment.CategoryID)
            {
                Category c = db.Categories.Find(basic.Assignment.CategoryID);
                if (c.CourseID != ActiveCourse.CourseID)
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            if (basic.Submission.ReleaseDate >= basic.Stop.ReleaseDate)
            {
                ModelState.AddModelError("time", "The due date must come after the release date");
            }
            if (ModelState.IsValid)
            {
                // Find current submission and stop activities from basic assignment
                SubmissionActivity submission = assignment.AssignmentActivities.Where(aa => aa is SubmissionActivity).First() as SubmissionActivity;
                StopActivity stop = assignment.AssignmentActivities.Where(aa => aa is StopActivity).FirstOrDefault() as StopActivity;

                assignment.Deliverables.Clear();

                // Update Basic Assignment fields
                assignment.CategoryID = basic.Assignment.CategoryID;
                assignment.Name = basic.Assignment.Name;
                assignment.Description = basic.Assignment.Description;
                assignment.PointsPossible = basic.Assignment.PointsPossible;
                assignment.Deliverables = basic.Assignment.Deliverables;

                // Update Submission Activity fields

                submission.ReleaseDate = basic.Submission.ReleaseDate;
                submission.Name = basic.Submission.Name;
                submission.PointsPossible = basic.Submission.PointsPossible;

                submission.HoursLatePerPercentPenalty = basic.Submission.HoursLatePerPercentPenalty;
                submission.HoursLateUntilZero = basic.Submission.HoursLateUntilZero;
                submission.PercentPenalty = basic.Submission.PercentPenalty;
                submission.MinutesLateWithNoPenalty = basic.Submission.MinutesLateWithNoPenalty;

                // Update Stop Activity fields

                stop.ReleaseDate = basic.Stop.ReleaseDate;

                // Flag models as modified and save to DB

                db.Entry(assignment).State = EntityState.Modified;
                db.Entry(submission).State = EntityState.Modified;
                db.Entry(stop).State = EntityState.Modified;

                db.SaveChanges();
            }

            ViewBag.Categories = new SelectList(db.Categories, "ID", "Name", basic.Assignment.CategoryID);
            ViewBag.DeliverableTypes = new SelectList(GetListOfDeliverableTypes(), "Value", "Text");

            return View(basic);
        }

        //
        // GET: /Assignment/Delete/5

        /*
        public ActionResult Delete(int id)
        {
            SubmissionActivity assignment = db.AbstractGradables.Find(id) as SubmissionActivity;
            return View(assignment);
        }
        */

        //
        // POST: /Assignment/Delete/5

        /*
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            SubmissionActivity assignment = db.AbstractGradables.Find(id) as SubmissionActivity;
            db.AbstractGradables.Remove(assignment);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
         */

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        private List<SelectListItem> GetListOfDeliverableTypes()
        {
            List<SelectListItem> fileTypes = new List<SelectListItem>();
            int i = 0;
            DeliverableType deliverable = (DeliverableType)i;
            while (Enum.IsDefined(typeof(DeliverableType), i))
            {
                Type type = deliverable.GetType();

                FieldInfo fi = type.GetField(deliverable.ToString());

                //we get the attributes of the selected language
                FileExtensions[] attrs = (fi.GetCustomAttributes(typeof(FileExtensions), false) as FileExtensions[]);

                //make sure we have more than (should be exactly 1)
                if (attrs.Length > 0 && attrs[0] is FileExtensions)
                {
                    //we get the first attributes value which should be the fileExtension
                    string s = deliverable.ToString();
                    s += " (";
                    s += string.Join(", ", attrs[0].Extensions);
                    s += ")";

                    SelectListItem sli = new SelectListItem();

                    sli.Text = s;
                    sli.Value = i.ToString();

                    fileTypes.Add(sli);
                }
                else
                {
                    //throw and exception if not decorated with any attrs because it is a requirement
                    throw new Exception("Languages must have be decorated with a FileExtensionAttribute");
                }

                i++;
                deliverable = (DeliverableType)i;
            }

            return fileTypes;
        }
    }
}