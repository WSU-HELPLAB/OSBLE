using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility.Lookups;

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
                            Comment = "fp comment"
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
    }
}
