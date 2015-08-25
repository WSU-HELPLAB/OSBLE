using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.OData;

namespace OSBLE.Controllers.Odata
{
    [OsbleAuthorize]
    public class CoursesController : EntitySetController<Course, int>
    {
        private OSBLEContext db;
        private UserProfile currentUser;
        public CoursesController()
            : base()
        {
            db = new OSBLEContext();
            currentUser = OsbleAuthentication.CurrentUser;
        }

        private IQueryable<Course> DefaultQuery
        {
            get
            {
                var query = from course in db.Courses.Include("Assignments")
                            join cu in db.CourseUsers on course.ID equals cu.AbstractCourseID
                            where cu.UserProfileID == currentUser.ID
                            && cu.AbstractRole.CanGrade
                            select course;
                return query;
            }
        }

        public override IQueryable<Course> Get()
        {
            return DefaultQuery;
        }

        protected override Course GetEntityByKey(int key)
        {
            var query = DefaultQuery;
            query = from q in query
                    where q.ID == key
                    select q;
            return query.FirstOrDefault();
        }

        public List<Assignment> GetAssignments([FromODataUri] int key)
        {
            Course currentCourse = GetEntityByKey(key);
            List<Assignment> assignments = new List<Assignment>();
            if(currentCourse != null)
            {
                assignments = currentCourse.Assignments.ToList();
            }
            return assignments;
        }
    }
}
