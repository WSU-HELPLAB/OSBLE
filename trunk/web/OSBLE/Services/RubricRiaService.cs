
namespace OSBLE.Services
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using System.Linq;
    using System.ServiceModel.DomainServices.EntityFramework;
    using System.ServiceModel.DomainServices.Hosting;
    using System.ServiceModel.DomainServices.Server;
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

        public IQueryable<AbstractCourse> GetCourses()
        {
            var courses = from course in db.AbstractCourses
                          join cu in db.CoursesUsers on course.ID equals cu.AbstractCourseID
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

        public AbstractCourse GetActiveCourse()
        {
            return currentCourse;
        }

        public IQueryable<Rubric> GetRubricsForCourse(int courseId)
        {
            var rubrics = from rubric in db.Rubrics
                          join cr in db.CourseRubrics on rubric.ID equals cr.RubricID
                          where cr.AbstractCourseID == courseId
                          select rubric;
            return rubrics.AsQueryable();
        }

        public bool SaveRubric(int courseId, Rubric rubric)
        {
            return true;
        }
    }
}


