using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility;
using Ionic.Zip;

namespace OSBLEPlus.Services.Tests.Activities
{
    [TestClass]
    public class CourseControllerTests
    {
        [TestMethod]
        public void CanSubmitAssignment()
        {
            var result = RunAsync().Result;
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
            using (var zip = new ZipFile())
            {
                zip.AddFile(path);
                zip.Save(stream);
                stream.Position = 0;

                submit.CreateSolutionBinary(stream.ToArray());
            }
            File.Delete(path);
            return submit;
        }
        private static async Task<bool> RunAsync()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(StringConstants.DataServiceRoot);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                SubmitEvent temp = CreateSubmitEvent();

                var request = new SubmissionRequest
                {
                    AuthToken = "83-B6-77-B8-54-83-30-7D-0F-EE-68-38-6D-E7-42-5E-2A-D1-3A-72",
                    SubmitEvent = temp,
                };
                var response = await client.PostAsJsonAsync("api/course/post", request);

                return response.IsSuccessStatusCode;
            }
        }
    }
}
