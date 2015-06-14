using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interfaces;
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
            Assert.IsTrue(result);
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
                            EventDate = eventDate,
                            EventTypeId = (int) EventType.AskForHelpEvent,
                            SenderId = 1,
                            SolutionName = solutionName,
                            Code = "c#"
                        },
                        new BuildEvent
                        {
                            EventDate = eventDate,
                            EventTypeId = (int) EventType.BuildEvent,
                            SenderId = 1,
                            SolutionName = solutionName,
                            CriticalErrorName = "critical error"
                        },
                        new ExceptionEvent
                        {
                            EventDate = eventDate,
                            EventTypeId = (int) EventType.ExceptionEvent,
                            SenderId = 1,
                            SolutionName = solutionName,
                            ExceptionCode = 1,
                            DocumentName = "ex doc",
                            ExceptionType = "ex type",
                            ExceptionDescription = "ex desc"
                        },
                        new FeedPostEvent
                        {
                            EventDate = eventDate,
                            EventTypeId = (int) EventType.FeedPostEvent,
                            SenderId = 1,
                            SolutionName = solutionName,
                            Comment = "fp comment"
                        },
                        // below events need to set up data dependencies
                        //new HelpfulMarkGivenEvent
                        //{
                        //    EventDate = eventDate,
                        //    EventTypeId = (int) EventType.HelpfulMarkGivenEvent,
                        //    SenderId = 1,
                        //    SolutionName = solutionName,
                        //    LogCommentEventId = 1
                        //},
                        //new LogCommentEvent
                        //{
                        //    EventDate = eventDate,
                        //    EventTypeId = (int) EventType.LogCommentEvent,
                        //    SenderId = 1,
                        //    SolutionName = solutionName,
                        //    Content = "log comment"
                        //},
                        //new SubmitEvent
                        //{
                        //    EventDate = eventDate,
                        //    EventTypeId = (int) EventType.SubmitEvent,
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
