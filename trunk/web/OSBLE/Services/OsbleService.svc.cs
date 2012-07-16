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
using System.IO;

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
            List<Assignment> efAssignments = query.ToList();
            List<Assignment> nonEfAssignments = new List<Assignment>(efAssignments.Count);
            foreach (Assignment assignment in efAssignments)
            {
                nonEfAssignments.Add(new Assignment(assignment));
            }
            return nonEfAssignments.ToArray();
        }

        [OperationContract]
        public byte[] GetAssignmentSubmission(int assignmentId, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new byte[0];
            }
            UserProfile profile = _authService.GetActiveUser(authToken);
            Assignment assignment = _db.Assignments.Find(assignmentId);

            //make sure that the user is enrolled in the course
            CourseUser courseUser = (from cu in _db.CourseUsers
                                    where cu.AbstractCourseID == assignment.CourseID
                                    &&
                                    cu.UserProfileID == profile.ID
                                    select cu).FirstOrDefault();
            if (courseUser == null)
            {
                return new byte[0];
            }

            //users are attached to assignments through teams, so we have to find the correct team
            Team team = (from tm in _db.TeamMembers
                        join at in _db.AssignmentTeams on tm.TeamID equals at.TeamID
                        where tm.CourseUserID == courseUser.ID
                        && at.AssignmentID == assignmentId
                        select tm.Team).FirstOrDefault();

            if(team == null)
            {
                return new byte[0];
            }
                        

            OSBLE.Models.FileSystem.FileSystem fs = new Models.FileSystem.FileSystem();
            Stream stream = fs.Course(courseUser.AbstractCourseID)
                              .Assignment(assignmentId)
                              .Submission(team.ID)
                              .AllFiles()
                              .ToZipStream();
            MemoryStream ms = new MemoryStream();
            try
            {
                stream.CopyTo(ms);
            }
            catch (Exception)
            {
            }
            byte[] bytes = ms.ToArray();
            stream.Close();
            ms.Close();
            return bytes;
            
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
