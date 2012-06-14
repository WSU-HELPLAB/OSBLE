using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models;
using OSBLE.Models.Assignments;

namespace OSBLE.Services
{
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class OsbleService
    {
        private AuthenticationService _authService = new AuthenticationService();
        private OSBLEContext _db = new OSBLEContext();

        /// <summary>
        /// Returns a list of Courses associated with the provided auth token
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public Course[] GetCourses(string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new Course[0];
            }
            UserProfile profile = _authService.GetActiveUser(authToken);
            List<Course> efCourses = (from cu in _db.CourseUsers
                                      where cu.UserProfileID == profile.ID
                                      &&
                                      cu.AbstractCourse is Course
                                      select cu.AbstractCourse as Course).ToList();

            //convert entity framework-based course to normal course for easier wire
            //transfer
            List<Course> nonEfCourses = new List<Course>(efCourses.Count);
            foreach (Course course in efCourses)
            {
                //use copy constructor to remove crud
                nonEfCourses.Add(new Course(course));
            }
            return nonEfCourses.ToArray();
        }

        /// <summary>
        /// Returns all assignments associated with the given course
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public Assignment[] GetCourseAssignments(int courseId, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new Assignment[0];
            }
            UserProfile profile = _authService.GetActiveUser(authToken);

            //verify that the provided user is in this course
            CourseUser courseUser = _db.CourseUsers
                                       .Where(cu => cu.UserProfileID == profile.ID)
                                       .Where(cu => cu.AbstractCourseID == courseId)
                                       .FirstOrDefault();
            if (courseUser == null)
            {
                return new Assignment[0];
            }
            var query = from assignment in _db.Assignments
                        where assignment.IsWizardAssignment == true
                        &&
                        assignment.CourseID == courseId
                        select assignment;
            return query.ToArray();
        }

        /// <summary>
        /// Returns role information for the given course and authToken
        /// </summary>
        /// <param name="authToken"></param>
        /// <param name="courseUserId"></param>
        /// <returns></returns>
        [OperationContract]
        public CourseRole GetCourseRole(int courseId, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new CourseRole();
            }
            UserProfile profile = _authService.GetActiveUser(authToken);
            CourseUser courseUser = _db.CourseUsers
                                    .Where(cu => cu.AbstractCourseID == courseId)
                                    .Where(cu => cu.UserProfileID == profile.ID)
                                    .FirstOrDefault();

            //trying to access course data for the wrong person
            if (courseUser == null)
            {
                return new CourseRole();
            }
            
            return new CourseRole(courseUser.AbstractRole);
        }
    }
}
