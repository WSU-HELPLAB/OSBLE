﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Services.Tests.Activities
{
    [TestClass]
    public class EventCollectorTests
    {
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
                    AuthToken = "83-B6-77-B8-54-83-30-7D-0F-EE-68-38-6D-E7-42-5E-2A-D1-3A-72",
                    AskHelpEvents = new[]
                    {
                        new AskForHelpEvent
                        {
                            SolutionName = "solution",
                            Code = "c#",
                            SenderId = 1
                        },
                        new AskForHelpEvent
                        {
                            SolutionName = "solution 2",
                            Code = "c#",
                            SenderId = 1
                        }

                    },
                    BuildEvents = new []
                    {
                        new BuildEvent
                        {
                            SenderId = 1,
                            SolutionName = "build solution 1",
                        },
                        new BuildEvent
                        {
                            SenderId = 1,
                            SolutionName = "build solution",
                        }
                    }
                };

                var response = await client.PostAsJsonAsync("api/eventcollection/post", request);

                return response.IsSuccessStatusCode;
            }
        }
    }
}
