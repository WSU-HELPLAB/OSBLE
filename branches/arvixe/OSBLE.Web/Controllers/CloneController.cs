using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Configuration;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Models.HomePage;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Data;
using OSBLE.Utility;

namespace OSBLE.Controllers
{
    public class CloneController : OSBLEController
    {
        //this controller is used to copy and clone courses and assignments
        // GET: /Clone/

        /// <summary>
        /// yc: course cloning, for any course the current user has been an instructor in
        /// </summary>
        /// <returns></returns>
        [CanCreateCourses]
        public ActionResult CloneCourse()
        {
            //find all courses current users is in
            List<CourseUser> allUsersCourses = db.CourseUsers.Where(cu => cu.UserProfileID == CurrentUser.ID).ToList();
            List<CourseUser> previousInstructedCourses = allUsersCourses.Where(cu => (cu.AbstractCourse is Course)
                    &&
                    (cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)).OrderByDescending(cu => (cu.AbstractCourse as Course).StartDate).ToList();
            ViewBag.pastCourses = previousInstructedCourses;
            return View();
        }

        /// <summary>
        /// yc: chose the course to select an assignment from
        /// </summary>
        /// <returns></returns>
        public ActionResult CloneAssignment()
        {
            //find all courses current users is in
            List<CourseUser> allUsersCourses = db.CourseUsers.Where(cu => cu.UserProfileID == CurrentUser.ID).ToList();
            List<CourseUser> previousInstructedCourses = allUsersCourses.Where(cu => (cu.AbstractCourse is Course)
                    &&
                    (cu.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)).OrderByDescending(cu => (cu.AbstractCourse as Course).StartDate).ToList();
            ViewBag.pastCourses = previousInstructedCourses;
            return View();
        }
        /// <summary>
        /// yc: course cloning setup
        /// </summary>
        /// <param name="courseid"></param>
        /// <returns></returns>
        [CanCreateCourses]
        [CanModifyCourse]
        public ActionResult CourseSetup(int courseid)
        {
            Course pastCourse = (from c in db.Courses
                                 where c.ID == courseid
                                 select c).FirstOrDefault();
            if (pastCourse != null)
            {
                Course Clone = new Course();
                Clone.AllowDashboardPosts = pastCourse.AllowDashboardPosts;
                Clone.AllowDashboardReplies = pastCourse.AllowDashboardReplies;
                Clone.AllowEventPosting = pastCourse.AllowEventPosting;
                Clone.CalendarWindowOfTime = pastCourse.CalendarWindowOfTime;
                Clone.HoursLatePerPercentPenalty = pastCourse.HoursLatePerPercentPenalty;
                Clone.HoursLateUntilZero = pastCourse.HoursLateUntilZero;
                Clone.MinutesLateWithNoPenalty = pastCourse.MinutesLateWithNoPenalty;
                Clone.Name = pastCourse.Name;
                Clone.Number = pastCourse.Number;
                Clone.Prefix = pastCourse.Prefix;
                Clone.PercentPenalty = pastCourse.PercentPenalty;
                Clone.RequireInstructorApprovalForEventPosting = pastCourse.RequireInstructorApprovalForEventPosting;
                Clone.TimeZoneOffset = pastCourse.TimeZoneOffset;
                //clone upkeep stuff from original course
                Clone.ID = pastCourse.ID;
                return View(Clone);
            }
            else
            {
                //could not find it, send them an empty coures
                return RedirectToAction("Create");
            }
        }

        [HttpPost]
        [CanCreateCourses]
        [CanModifyCourse]
        public ActionResult CourseSetup(Course clone)
        {
            Course oldCourse = (Course)(from c in db.AbstractCourses
                                        where c.ID == clone.ID
                                        select c).FirstOrDefault();
            Course getNewId = new Course();
            clone.ID = getNewId.ID;
            CourseController cc = new CourseController();

            if (ModelState.IsValid)
            {
                db.Courses.Add(clone);
                db.SaveChanges();

                clone.TimeZoneOffset = Convert.ToInt32(Request.Params["course_timezone"]);
                cc.createMeetingTimes(clone, clone.TimeZoneOffset);
                cc.createBreaks(clone);

                // Make current user an instructor on new course.
                CourseUser cu = new CourseUser();
                cu.AbstractCourseID = clone.ID;
                cu.UserProfileID = CurrentUser.ID;
                cu.AbstractRoleID = (int)CourseRole.CourseRoles.Instructor;


                db.CourseUsers.Add(cu);
                db.SaveChanges();

                Cache["ActiveCourse"] = clone.ID;

                return RedirectToAction("SelectAssignmentsToClone", new { courseID = oldCourse.ID });
            }
            return View(clone);
        }

        [HttpGet]
        [CanModifyCourse]
        public ActionResult AssignmentSetup(int courseid)
        {
            return RedirectToAction("SelectAssignmentsToClone", "Course", new { courseID = courseid });

        }
        /// <summary>
        /// yc: all assignments should be cloned EXACTLY like the course source. find all the old assignments
        /// and create a new entry into the database with corresponding components also added
        /// </summary>
        /// <param name="courseDestination"></param>
        /// <param name="courseSource"></param>
        /// <returns></returns>
        public bool CloneAllAssignmentsFromCourse(Course courseDestination, Course courseSource)
        {
            List<Assignment> previousAssignments = (from a in db.Assignments
                                                    where a.CourseID == courseSource.ID
                                                    select a).ToList();

            if (CopyAssignments(courseDestination, courseSource, previousAssignments))
                return true;
            else
                return false;

        }


        /// <summary>
        /// yc: with a given list of assignments, copy them from one course to another.
        /// </summary>
        /// <param name="courseDestination"></param>
        /// <param name="courseSource"></param>
        /// <param name="previousAssignments"></param>
        /// <returns></returns>
        public bool CopyAssignments(Course courseDestination, Course courseSource, List<Assignment> previousAssignments)
        {
            try
            {
                //calculate # of weeks since start date
                double difference = courseDestination.StartDate.Subtract(courseSource.StartDate).TotalDays;
                //for linking purposes, key == previous id, value == the clone course that is teh same
                Dictionary<int, int> linkHolder = new Dictionary<int, int>();
                foreach (Assignment p in previousAssignments)
                {
                    //disabling assignments that are not finished being handled yet
                    if (p.Type == AssignmentTypes.AnchoredDiscussion || p.Type == AssignmentTypes.CommitteeDiscussion
                            || p.Type == AssignmentTypes.ReviewOfStudentWork)
                        continue;

                    int prid = -1, paid = p.ID;
                    //for insert sake of cloned assigntment we must temprarly hold the list of assignments 
                    //whos id links to this assignment for temporary holding
                    List<Assignment> previouslyLinked = (from pl in db.Assignments
                                                         where pl.PrecededingAssignmentID == paid
                                                         select pl).ToList();
                    //remove the links for now
                    foreach (Assignment link in previouslyLinked)
                    {
                        link.PrecededingAssignmentID = null;
                        db.Entry(link).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    //tmp holders
                    if (p.RubricID != null)
                        prid = (int)p.RubricID;
                    //we are now ready for copying
                    Assignment na = new Assignment();
                    na = p;
                    na.CourseID = courseDestination.ID; //rewrite course id
                    na.IsDraft = true;
                    na.AssociatedEvent = null;
                    na.AssociatedEventID = null;
                    na.PrecededingAssignmentID = null;
                    na.AssignmentTeams = new List<AssignmentTeam>();
                    na.DiscussionTeams = new List<DiscussionTeam>();
                    na.ReviewTeams = new List<ReviewTeam>();
                    na.Deliverables = new List<Deliverable>();


                    //recalcualte new offsets for due dates on assignment
                    if (p.CriticalReviewPublishDate != null)
                    {
                        na.CriticalReviewPublishDate = ((DateTime)(p.CriticalReviewPublishDate)).Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    }
                    CourseController cc = new CourseController();
                    // to retain the time incase of in differt daylightsavings .. shifts
                    DateTime dd = cc.convertFromUtc(courseSource.TimeZoneOffset, na.DueDate);
                    DateTime dt = cc.convertFromUtc(courseSource.TimeZoneOffset, na.DueTime);
                    DateTime rd = cc.convertFromUtc(courseSource.TimeZoneOffset, na.ReleaseDate);
                    DateTime rt = cc.convertFromUtc(courseSource.TimeZoneOffset, na.ReleaseTime);
                    dd = dd.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    dt = dt.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    rd = rd.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    rt = rt.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                    //convert back to utc
                    na.DueDate = cc.convertToUtc(courseDestination.TimeZoneOffset, dd);
                    na.DueTime = cc.convertToUtc(courseDestination.TimeZoneOffset, dt);
                    na.ReleaseDate = cc.convertToUtc(courseDestination.TimeZoneOffset, rd);
                    na.ReleaseTime = cc.convertToUtc(courseDestination.TimeZoneOffset, rt);
                    //we now have a base to save
                    db.Assignments.Add(na);
                    db.SaveChanges();


                    //fix the link now
                    foreach (Assignment link in previouslyLinked)
                    {
                        link.PrecededingAssignmentID = paid;
                        db.Entry(link).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    linkHolder.Add(paid, na.ID); //for future assignment links

                    if (p.PrecededingAssignmentID != null)
                    {
                        na.PrecededingAssignmentID = linkHolder[(int)p.PrecededingAssignmentID];
                        na.PreceedingAssignment = db.Assignments.Find(linkHolder[(int)p.PrecededingAssignmentID]);
                        db.Entry(na).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    //copy assignmenttypes
                    if (p.Type == AssignmentTypes.DiscussionAssignment || p.Type == AssignmentTypes.CriticalReviewDiscussion)
                    {
                        DiscussionSetting pds = (from ds in db.DiscussionSettings
                                                 where ds.AssignmentID == paid
                                                 select ds).FirstOrDefault();

                        DiscussionSetting nds = new DiscussionSetting();
                        nds.InitialPostDueDate = pds.InitialPostDueDate.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                        nds.InitialPostDueDueTime = pds.InitialPostDueDueTime.Add(new TimeSpan(Convert.ToInt32(difference), 0, 0, 0));
                        nds.AssociatedEventID = null;
                        nds.MaximumFirstPostLength = pds.MaximumFirstPostLength;
                        nds.MinimumFirstPostLength = pds.MinimumFirstPostLength;
                        nds.AnonymitySettings = pds.AnonymitySettings;
                        na.DiscussionSettings = nds;
                        db.Entry(na).State = EntityState.Modified;
                        db.SaveChanges();
                    }

                    //copy critical review settings
                    if (p.Type == AssignmentTypes.CriticalReview)
                    {
                        CriticalReviewSettings pcs = (from ds in db.CriticalReviewSettings
                                                      where ds.AssignmentID == paid
                                                      select ds).FirstOrDefault();

                        if (pcs != null)
                        {
                            CriticalReviewSettings ncs = new CriticalReviewSettings();
                            ncs.ReviewSettings = pcs.ReviewSettings;
                            na.CriticalReviewSettings = ncs;
                            db.Entry(na).State = EntityState.Modified;
                            db.SaveChanges();
                        }
                    }

                    //team eval
                    if (p.Type == AssignmentTypes.TeamEvaluation)
                    {
                        TeamEvaluationSettings ptes = (from tes in db.TeamEvaluationSettings
                                                       where tes.AssignmentID == paid
                                                       select tes).FirstOrDefault();

                        if (ptes != null)
                        {
                            TeamEvaluationSettings ntes = new TeamEvaluationSettings();
                            ntes.DiscrepancyCheckSize = ptes.DiscrepancyCheckSize;
                            ntes.RequiredCommentLength = ptes.RequiredCommentLength;
                            ntes.MaximumMultiplier = ptes.MaximumMultiplier;
                            ntes.AssignmentID = na.ID;
                            na.TeamEvaluationSettings = ntes;
                            db.Entry(na).State = EntityState.Modified;
                            db.SaveChanges();
                        }

                    }

                    //components
                    //rubrics
                    if (p.RubricID != null)
                        CopyRubric(p, na);

                    ///deliverables
                    List<Deliverable> pads = (from d in db.Deliverables
                                              where d.AssignmentID == paid
                                              select d).ToList();
                    foreach (Deliverable pad in pads)
                    {
                        Deliverable nad = new Deliverable();
                        nad.AssignmentID = na.ID;
                        nad.DeliverableType = pad.DeliverableType;
                        nad.Assignment = na;
                        nad.Name = pad.Name;
                        db.Deliverables.Add(nad);
                        db.SaveChanges();
                        na.Deliverables.Add(nad);
                        db.Entry(na).State = EntityState.Modified;
                        db.SaveChanges();
                    }
                    //abet stuff should prolly go here
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// yc: this copy the rubric information from one assignment to another. 
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Destination"></param>
        /// <returns>bool for if success or fail</returns>
        public bool CopyRubric(Assignment Source, Assignment Destination)
        {
            try
            {
                int sid = -1;

                if (Source.RubricID != null)
                {
                    sid = (int)Source.RubricID;
                    //create a new reburic thats an exact copy with the same critera
                    Rubric nr = new Rubric();
                    nr.HasGlobalComments = Source.Rubric.HasGlobalComments;
                    nr.HasCriteriaComments = Source.Rubric.HasCriteriaComments;
                    nr.Description = Source.Rubric.Description;
                    Destination.Rubric = nr;
                    db.Entry(Destination).State = EntityState.Modified;
                    db.SaveChanges();

                    //now get all the stuff for it
                    Dictionary<int, int> clevelHolder = new Dictionary<int, int>();
                    Dictionary<int, int> ccriterionHolder = new Dictionary<int, int>();

                    List<Level> pls = (from rl in db.Levels
                                       where rl.RubricID == sid
                                       select rl).ToList();
                    foreach (Level pl in pls)
                    {
                        Level nl = new Level();
                        nl.LevelTitle = pl.LevelTitle;
                        nl.PointSpread = pl.PointSpread;
                        nl.RubricID = nr.ID;
                        db.Levels.Add(nl);
                        db.SaveChanges();
                        clevelHolder.Add(pl.ID, nl.ID);
                    }

                    List<Criterion> prcs = (from rc in db.Criteria
                                            where rc.RubricID == sid
                                            select rc).ToList();

                    foreach (Criterion prc in prcs) //create a new criteron
                    {
                        Criterion nrc = new Criterion();
                        nrc.CriterionTitle = prc.CriterionTitle;
                        nrc.Weight = prc.Weight;
                        nrc.RubricID = nr.ID;
                        db.Criteria.Add(nrc);
                        db.SaveChanges();
                        ccriterionHolder.Add(prc.ID, nrc.ID);
                    }

                    //now descriptions
                    //for some reason, cell descriptions do not come with this assignment so lets do a search fo rit
                    List<CellDescription> pcds = (from cd in db.CellDescriptions
                                                  where cd.RubricID == sid
                                                  select cd).ToList();

                    foreach (CellDescription pcd in pcds)
                    {
                        CellDescription ncd = new CellDescription();
                        ncd.CriterionID = ccriterionHolder[pcd.CriterionID];
                        ncd.LevelID = clevelHolder[pcd.LevelID];
                        ncd.RubricID = nr.ID;
                        ncd.Description = pcd.Description;
                        db.CellDescriptions.Add(ncd);
                        db.SaveChanges();
                    }
                }
                if (Source.StudentRubricID != null)
                {
                    sid = (int)Source.StudentRubricID;
                    //create a new reburic thats an exact copy with the same critera
                    Rubric nr = new Rubric();
                    nr.HasGlobalComments = Source.Rubric.HasGlobalComments;
                    nr.HasCriteriaComments = Source.Rubric.HasCriteriaComments;
                    nr.Description = Source.Rubric.Description;

                    db.Rubrics.Add(nr);
                    db.SaveChanges();

                    Destination.StudentRubricID = nr.ID;
                    db.Entry(Destination).State = EntityState.Modified;
                    db.SaveChanges();

                    //now get all the stuff for it
                    Dictionary<int, int> slevelHolder = new Dictionary<int, int>();
                    Dictionary<int, int> scriterionHolder = new Dictionary<int, int>();

                    List<Level> pls = (from rl in db.Levels
                                       where rl.RubricID == sid
                                       select rl).ToList();
                    foreach (Level pl in pls)
                    {
                        Level nl = new Level();
                        nl.LevelTitle = pl.LevelTitle;
                        nl.PointSpread = pl.PointSpread;
                        nl.RubricID = nr.ID;
                        db.Levels.Add(nl);
                        db.SaveChanges();
                        slevelHolder.Add(pl.ID, nl.ID);
                    }

                    List<Criterion> prcs = (from rc in db.Criteria
                                            where rc.RubricID == sid
                                            select rc).ToList();

                    foreach (Criterion prc in prcs) //create a new criteron
                    {
                        Criterion nrc = new Criterion();
                        nrc.CriterionTitle = prc.CriterionTitle;
                        nrc.Weight = prc.Weight;
                        nrc.RubricID = nr.ID;
                        db.Criteria.Add(nrc);
                        db.SaveChanges();
                        scriterionHolder.Add(prc.ID, nrc.ID);
                    }

                    //now descriptions
                    //for some reason, cell descriptions do not come with this assignment so lets do a search fo rit
                    List<CellDescription> pcds = (from cd in db.CellDescriptions
                                                  where cd.RubricID == sid
                                                  select cd).ToList();

                    foreach (CellDescription pcd in pcds)
                    {
                        CellDescription ncd = new CellDescription();
                        ncd.CriterionID = scriterionHolder[pcd.CriterionID];
                        ncd.LevelID = slevelHolder[pcd.LevelID];
                        ncd.RubricID = nr.ID;
                        ncd.Description = pcd.Description;
                        db.CellDescriptions.Add(ncd);
                        db.SaveChanges();
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// yc: get for view
        /// </summary>
        /// <param name="course"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult SelectAssignmentsToClone(int courseID)
        {
            ViewBag.cid = courseID;
            List<Assignment> previousAssignments = (from a in db.Assignments
                                                    where a.CourseID == courseID
                                                    select a).ToList();
            return View(previousAssignments);
        }

        /// <summary>
        /// yc: post for select assignment, passed the count beacuse we cannot have the funciton signature
        /// </summary>
        /// <param name="cID"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult SelectAssignmentsToClone(int cID, int count)
        {
            Course o = (Course)(from c in db.AbstractCourses
                                where c.ID == cID
                                select c).FirstOrDefault();
            List<Assignment> previousAssignments = (from a in db.Assignments
                                                    where a.CourseID == cID
                                                    select a).ToList();
            List<int> i = new List<int>(); //the home of assignments we want to copy
            List<Assignment> n = new List<Assignment>();
            foreach (Assignment a in previousAssignments)
            {
                if (Request.Params["a_" + a.ID.ToString()] != null)
                {
                    i.Add(previousAssignments.IndexOf(a));
                }
            }
            foreach (int a in i)
            {
                n.Add(previousAssignments[a]);
            }
            if (n.Count > 0)
                CopyAssignments((ActiveCourseUser.AbstractCourse as Course), o, n);

            return RedirectToAction("Index", "Home");
        }
    }
}
