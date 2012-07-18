using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLE.UnitTests.AuthenticationService;
using OSBLE.UnitTests.OsbleService;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;
using OSBLE.Models.Assignments;
using OSBLE.Models;
using Ionic.Zip;

namespace OSBLE.UnitTests
{
    [TestClass]
    public class OsbleServiceTest
    {
        public OsbleServiceTest()
        {
        }

        [TestMethod]
        public void OsbleService_AuthStringTest()
        {
            AuthenticationServiceClient authClient = new AuthenticationServiceClient();
            string token = authClient.ValidateUser("bob@smith.com", "123123");
            UserProfile profile = authClient.GetActiveUser(token);

            Assert.AreNotEqual(0, token.Length);
            Assert.AreEqual(profile.FirstName.ToLower(), "bob");
        }

        [TestMethod]
        public void OsbleService_GetCoursesTest()
        {
            AuthenticationServiceClient authClient = new AuthenticationServiceClient();
            OsbleServiceClient osbleClient = new OsbleServiceClient();
            string token = authClient.ValidateUser("bob@smith.com", "123123");
            Course[] courses = osbleClient.GetCourses(token);
            int count = courses.Where(c => c.ID == 4).Count();
            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void OsbleService_GetRoleTest()
        {
            AuthenticationServiceClient authClient = new AuthenticationServiceClient();
            OsbleServiceClient osbleClient = new OsbleServiceClient();
            string token = authClient.ValidateUser("bob@smith.com", "123123");

            //course id 4: OSBLE 101
            AbstractRole role = osbleClient.GetCourseRole(4, token);

            //bob should be able to modify this course
            Assert.AreEqual(true, role.CanModify);
        }

        [TestMethod]
        public void OsbleService_GetAssignmentsTest()
        {
            AuthenticationServiceClient authClient = new AuthenticationServiceClient();
            OsbleServiceClient osbleClient = new OsbleServiceClient();
            string token = authClient.ValidateUser("bob@smith.com", "123123");
            UserProfile profile = authClient.GetActiveUser(token);
            Assignment[] serviceAssignments = osbleClient.GetCourseAssignments(4, token);
            using (OSBLEContext db = new OSBLEContext())
            {
                CourseUser courseUser = (from cu in db.CourseUsers
                                         where cu.AbstractCourseID == 4
                                         && cu.UserProfileID == profile.ID
                                         select cu).FirstOrDefault();
                Assert.AreNotEqual(null, courseUser);
                int assignmentCount = (courseUser.AbstractCourse as Course).Assignments.Count;
                Assert.AreEqual(assignmentCount, serviceAssignments.Length);
            }
        }

        [TestMethod]
        public void OsbleService_GetAssignmentSubmissionTest()
        {
            //AC Note: This requires OSBLE to be set up properly
            AuthenticationServiceClient authClient = new AuthenticationServiceClient();
            OsbleServiceClient osbleClient = new OsbleServiceClient();
            string token = authClient.ValidateUser("betty@rogers.com", "123123");
            byte[] data = osbleClient.GetAssignmentSubmission(1, token);
            using (ZipFile zip = ZipFile.Read(data))
            {
                Assert.AreEqual(1, zip.Entries.Count);
            }
        }
    }
}
