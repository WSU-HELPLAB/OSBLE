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
using System.IO;

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

        [TestMethod]
        public void OsbleService_SubmitAssignmentTest()
        {
            //AC Note: again, this requires OSBLE to be set up property.  May need
            //to change values accordingly
            AuthenticationServiceClient authClient = new AuthenticationServiceClient();
            OsbleServiceClient osbleClient = new OsbleServiceClient();
            string token = authClient.ValidateUser("betty@rogers.com", "123123");
            
            ZipFile file = new ZipFile();

            //AC note: may need to change document location
            FileStream stream = File.OpenRead("D:\\acarter\\temp\\address.pdf");

            //AC Note: may need to change name of file
            file.AddEntry("hw1.pdf", stream);
            MemoryStream zipStream = new MemoryStream();
            file.Save(zipStream);

            //AC note: may need to change assignment ID (first parameter)
            bool result = osbleClient.SubmitAssignment(1, zipStream.ToArray(), token);
            Assert.AreEqual(true, result);
        }

        [TestMethod]
        public void OsbleService_GetReviewItemsTest()
        {
            //AC Note: This requires OSBLE to be set up properly
            AuthenticationServiceClient authClient = new AuthenticationServiceClient();
            OsbleServiceClient osbleClient = new OsbleServiceClient();
            string token = authClient.ValidateUser("betty@rogers.com", "123123");
            byte[] data = osbleClient.GetReviewItems(2, token);
            using (ZipFile zip = ZipFile.Read(data))
            {
                zip.Save("D:\\acarter\\temp\\GetReviewItemsTest.zip");
            }
        }

        [TestMethod]
        public void OsbleService_SubmitReviewTest()
        {
            //AC Note: again, this requires OSBLE to be set up property.  May need
            //to change values accordingly
            AuthenticationServiceClient authClient = new AuthenticationServiceClient();
            OsbleServiceClient osbleClient = new OsbleServiceClient();
            string token = authClient.ValidateUser("betty@rogers.com", "123123");

            ZipFile file = new ZipFile();

            //AC note: may need to change document location
            FileStream stream = File.OpenRead("D:\\acarter\\temp\\address.pdf");

            //AC Note: may need to change name of file
            file.AddEntry("sdfsdfsdf.pdf", stream);
            MemoryStream zipStream = new MemoryStream();
            file.Save(zipStream);
            
            //AC note: may need to change assignment ID (first parameter)
            bool result = osbleClient.SubmitReview(7, 2, zipStream.ToArray(), token);
            Assert.AreEqual(true, result);
        }
    }
}
