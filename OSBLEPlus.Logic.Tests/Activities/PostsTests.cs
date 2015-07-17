using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using Ionic.Zip;

namespace OSBLEPlus.Logic.Tests.Activities
{
    [TestClass]
    public class PostsTests
    {
        [TestMethod]
        public void TestPost()
        {
            // Arrange
            var events = Events;

            // Act
            var result = Posts.Post(events);

            // Assert
            Assert.IsTrue(result > 0);
        }


        [TestMethod]
        public void TestSubmission()
        {
            var submit = CreateSubmitEvent();
            string path = Directory.GetCurrentDirectory();
            Posts.SaveToFileSystem(submit, 1, path);
            var fullpath = path + "\\Courses\\4\\Assignments\\3\\1\\Test User\\testfile.txt";
            File.SetAttributes(fullpath, FileAttributes.Normal);
            bool success = File.Exists(fullpath);
            File.Delete(fullpath);
            Directory.Delete(path+"\\Courses", true);
            Assert.IsTrue(success);
        }

        #region setup helpers
        private static IEnumerable<IActivityEvent> Events
        {
            get
            {
                var eventDate = DateTime.Now;
                var solutionName = new Uri(Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase)).LocalPath;
                var events = new List<IActivityEvent>();

                for (var idx = 0; idx < 25; idx ++)
                {
                    eventDate = eventDate.AddMinutes(10);
                    events.AddRange(new List<IActivityEvent>
                    {
                        new AskForHelpEvent
                        {
                            SenderId = 1,
                            SolutionName = solutionName,
                            Code = "c#"
                        },
                        new BuildEvent
                        {
                            SenderId = 1,
                            SolutionName = solutionName,
                        },
                        new ExceptionEvent
                        {
                            SenderId = 1,
                            SolutionName = solutionName,
                            ExceptionCode = 1,
                            DocumentName = "ex doc",
                            ExceptionType = "ex type",
                            ExceptionDescription = "ex desc"
                        },
                        new FeedPostEvent
                        {
                            SenderId = 1,
                            SolutionName = solutionName,
                            Comment = "fp comment",
                            CourseId = 1
                        },
                        // below events need to set up data dependencies
                        //new HelpfulMarkGivenEvent
                        //{
                        //    SenderId = 1,
                        //    SolutionName = solutionName,
                        //    LogCommentEventId = 1
                        //},
                        //new LogCommentEvent
                        //{
                        //    SenderId = 1,
                        //    SolutionName = solutionName,
                        //    Content = "log comment"
                        //},
                        //new SubmitEvent
                        //{
                        //    SenderId = 1,
                        //    SolutionName = solutionName,
                        //    AssignmentId = 1
                        //}
                    });
                }

                return events;
            }
        }

        private static SubmitEvent CreateSubmitEvent()
        {
            var submit = new SubmitEvent
            {
                //SolutionName = "C:/SubmissionTest/Source/TestSolution.sln",
                CourseId = 4,
                AssignmentId = 3,
                SenderId = 1,
                Sender = new User
                {
                    FirstName = "Test",
                    LastName = "User"
                }
            };
            //submit.GetSolutionBinary();
            string path = "testfile.txt";
            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine("CourseId = 4");
                sw.WriteLine("AssignmentId = 3");
                sw.WriteLine("SenderId = 1");
                sw.WriteLine("Name = Test User");
            }
            var stream = new MemoryStream();
            using (var zip = new ZipFile()){
                zip.AddFile(path);
                zip.Save(stream);
                stream.Position = 0;

                submit.CreateSolutionBinary(stream.ToArray());
            }
            File.Delete(path);
            return submit;
        }
        #endregion
    }
}
