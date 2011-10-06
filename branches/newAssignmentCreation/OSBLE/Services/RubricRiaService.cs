namespace OSBLE.Services
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;
    using OSBLE.Models.AbstractCourses;
    using OSBLE.Models.Courses;
    using OSBLE.Models.Courses.Rubrics;

    // Implements application logic using the OsbleEntities context.
    // TODO: Add your application logic to these methods or in additional methods.
    // TODO: Wire up authentication (Windows/ASP.NET Forms) and uncomment the following to disable anonymous access
    // Also consider adding roles to restrict access as appropriate.
    // [RequiresAuthentication]
    [EnableClientAccess()]
    public class RubricRiaService : OSBLEService
    {
        public Criterion DummyCriterion()
        {
            throw new NotImplementedException("You're not supposed to use this!");
        }

        public Level DummyLevel()
        {
            throw new NotImplementedException("You're not supposed to use this!");
        }

        public Community DummyCommunity()
        {
            throw new NotImplementedException("You're not supposed to use this!");
        }

        public CellDescription DummyCellDescription()
        {
            throw new NotImplementedException("You're not supposed to use this!");
        }

        public CourseRubric DummyCourseRubric()
        {
            throw new NotImplementedException("You're not supposed to use this!");
        }

        public AbstractCourse GetActiveCourse()
        {
            return currentCourse;
        }

        public IQueryable<CellDescription> GetCellDescriptions(int rubricId)
        {
            Rubric rubric = db.Rubrics.Find(rubricId);
            if (rubric != null)
            {
                return rubric.CellDescriptions.AsQueryable();
            }
            else return null;
        }

        public IQueryable<AbstractCourse> GetCourses()
        {
            var courses = from course in db.AbstractCourses
                          join cu in db.CourseUsers on course.ID equals cu.AbstractCourseID
                          where cu.UserProfileID == currentUserProfile.ID
                          &&
                          (
                             cu.AbstractRole.CanModify
                             ||
                             course is Community
                          )
                          select course;
            return courses.AsQueryable();
        }

        public IQueryable<Rubric> GetRubricsForCourse(int courseId)
        {
            var rubrics = from rubric in db.Rubrics
                          join cr in db.CourseRubrics on rubric.ID equals cr.RubricID
                          where cr.AbstractCourseID == courseId
                          select rubric;
            return rubrics.AsQueryable();
        }

        [Update]
        [Insert]
        public void AddCourseRubric(CourseRubric courseRubric)
        {
            //make sure that the association doesn't already exist
            int count = (from cr in db.CourseRubrics
                         where cr.RubricID == courseRubric.RubricID
                         &&
                         cr.AbstractCourseID == courseRubric.AbstractCourseID
                         select cr).Count();
            if (count == 0)
            {
                db.CourseRubrics.Add(courseRubric);
                db.SaveChanges();
            }
        }

        [Insert]
        public void AddCellDescription(CellDescription desc)
        {
            Criterion crit = (from c in db.Criteria where c.ID == desc.CriterionID select c).FirstOrDefault();
            crit.Rubric.CellDescriptions.Add(desc);
            db.Entry(crit.Rubric).State = EntityState.Modified;
            db.SaveChanges();
        }

        [Delete]
        public void DeleteCellDescription(CellDescription desc)
        {
            db.CellDescriptions.Remove(desc);
            db.SaveChanges();
        }

        [Insert]
        public void AddCriterion(Criterion crit)
        {
            db.Criteria.Add(crit);
            db.SaveChanges();
        }

        [Insert]
        public void AddLevel(Level level)
        {
            db.Levels.Add(level);
            db.SaveChanges();
        }

        [Insert]
        public void AddRubric(Rubric rubric)
        {
            db.Rubrics.Add(rubric);
            db.SaveChanges();
        }

        public void clearLevelsAndCrit(int rubricID)
        {
            Rubric rubric = db.Rubrics.Find(rubricID);

            db.Entry(rubric).State = EntityState.Modified;

            //when updating a rubric, we must thow away any existing levels, criteria,
            //and cell descriptions
            List<Level> levels = (from l in db.Levels where l.RubricID == rubric.ID select l).ToList();
            List<Criterion> criteria = (from c in db.Criteria where c.RubricID == rubric.ID select c).ToList();
            List<CellDescription> cellDesc = (from desc in db.CellDescriptions
                                              join level in db.Levels on desc.LevelID equals level.ID
                                              join crit in db.Criteria on desc.CriterionID equals crit.ID
                                              where level.RubricID == rubric.ID
                                              &&
                                              crit.RubricID == rubric.ID
                                              select desc).ToList();

            foreach (Level l in levels)
            {
                db.Levels.Remove(l);
            }
            foreach (Criterion c in criteria)
            {
                db.Criteria.Remove(c);
            }
            foreach (CellDescription d in cellDesc)
            {
                db.CellDescriptions.Remove(d);
            }
            db.SaveChanges();
        }

        [Update]
        public void UpdateRubric(Rubric rubric)
        {
            db.Entry(rubric).State = EntityState.Modified;
            db.SaveChanges();
        }

        public bool RubricHasEvaluations(int rubricId)
        {
            int items = (from e in db.RubricEvaluations
                         where e.AssignmentActivity.AbstractAssignment.Rubric.ID == rubricId
                         select e).Count();
            if (items > 0)
            {
                return true;
            }
            return false;
        }
    }
}