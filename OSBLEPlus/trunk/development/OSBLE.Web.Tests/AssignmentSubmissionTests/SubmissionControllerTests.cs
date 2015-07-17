using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLE.Controllers;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;

namespace OSBLE.Web.Tests.AssignmentSubmissionTests
{
    [TestClass]
    public class SubmissionControllerTests
    {
        [TestMethod]
        public void CanCreateAssignmentForAnchorDiscussion()
        {
            AbstractCourse c = new Course();
            //Given posted files
            //-generate 2 files
            HttpPostedFileWrapper file1 = new HttpPostedFileWrapper(null);
            HttpPostedFileWrapper file2 = new HttpPostedFileWrapper(null);
            List<HttpPostedFileBase> files=new List<HttpPostedFileBase>();
            files.Add(file1);
            files.Add(file2);

            //Given existing assignment
            //-get from db
            int assign = 10;

            //Given AuthorTeamId
            int authorteam = 75;
            AssignmentTeam at = new AssignmentTeam();
            at.TeamID = authorteam;

            //When Create assignment for anchor discussion is requested
            SubmissionController sc = new SubmissionController();
            sc.Create(assign, files, authorteam);



            //Then the files are generated on path
            string submission = OSBLE.FileSystem.GetDeliverable(c as Course,
    assign, at, null, null);

        }

                [TestMethod]
    }
}
