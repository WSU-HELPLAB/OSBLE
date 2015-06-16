using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.DataAccess.Profiles;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;
using OSBLEPlus.Services.Controllers;

namespace OSBLEPlus.Services.Tests.Activities
{
    [TestClass]
    public class EventCollectorTests
    {
        [Ignore]
        [TestMethod]
        // since it's hard to mock the auth module with file cache
        // this is a hard test
        // to make sure the event request can go across the actual wire
        public void TestPost()
        {
            var result = RunAsync().Result;
            Assert.IsTrue(result);
        }

        static async Task<bool> RunAsync()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(StringConstants.DataServiceRoot);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var request = new EventPostRequest
                {
                    AuthToken = "test",
                    AskHelpEvents = new[]
                    {
                        new AskForHelpEvent
                        {
                            EventDate = DateTime.Now,
                            EventTypeId = 1,
                            SolutionName = "solution",
                            Code = "c#",
                            SenderId = 1
                        },
                        new AskForHelpEvent
                        {
                            EventDate = DateTime.Now,
                            EventTypeId = 1,
                            SolutionName = "solution 2",
                            Code = "c#",
                            SenderId = 1
                        }

                    },
                    BuildEvents = new []
                    {
                        new BuildEvent
                        {
                            EventDate = DateTime.Now,
                            EventTypeId = (int) EventType.BuildEvent,
                            SenderId = 1,
                            SolutionName = "build solution 1",
                            CriticalErrorName = "critical error"
                        },
                        new BuildEvent
                        {
                            EventDate = DateTime.Now,
                            EventTypeId = (int) EventType.BuildEvent,
                            SenderId = 1,
                            SolutionName = "build solution",
                            CriticalErrorName = "critical error 2"
                        }
                    }
                };

                var response = await client.PostAsJsonAsync("api/eventcollection", request);

                return response.IsSuccessStatusCode;
            }
        }
    }
}
