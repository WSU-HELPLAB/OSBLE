using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBLEPlus.Services.Tests.Profiles
{
    [TestClass]
    public class UserProfileTest
    {
        [TestMethod]
        public void TestPost()
        {
            var result = RunAsync();
            Assert.IsTrue(!string.IsNullOrWhiteSpace(result.Result));
        }

        static async Task<string> RunAsync()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(StringConstants.DataServiceRoot);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var task = client.GetAsync(string.Format("api/userprofiles/login?e={0}&hp={1}",
                    "bob@smith.com",
                    Authentication.GetOsblePasswordHash("123123")))
                    .Result
                    .Content
                    .ReadAsStringAsync();

                await task;

                return task.Result;
            }
        }
    }
}
