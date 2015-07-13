using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Newtonsoft.Json;

using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Helpers;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Auth;

namespace OSBIDE.Library.ServiceClient.ServiceHelpers
{
    public class AsyncServiceClient
    {
        public static HttpClient GetClient()
        {
            var client = new HttpClient { BaseAddress = new Uri(StringConstants.DataServiceRoot) };
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }

        public static async Task<string> LibraryVersionNumber()
        {
            using (var client = GetClient())
            {
                var task = client.GetStringAsync("api/assemblyinfo/versionnumber");
                await task;

                return task.Result.TrimEnd('"').TrimStart('"');
            }
        }

        public static async Task<DateTime> GetMostRecentWhatsNewItem()
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync("api/course/mostrecentwhatsnewitem")
                                 .Result.Content.ReadAsStringAsync();
                await task;

                return JsonConvert.DeserializeObject<DateTime>(task.Result);
            }
        }

        public static async Task<string> Login(string userName, string password)
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync(string.Format("api/userprofiles/login?e={0}&hp={1}",
                                            userName,
                                            Authentication.GetOsblePasswordHash(password)));
                await task;

                return task.Result.StatusCode == HttpStatusCode.OK
                                               ? task.Result.Content.ReadAsStringAsync().Result
                                               : string.Empty;
            }
        }

        public static async Task<bool> IsValidKey(string authToken)
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync(string.Format("api/userprofiles/isvalidkey?a={0}", authToken))
                                 .Result.Content.ReadAsStringAsync();
                await task;

                return JsonConvert.DeserializeObject<bool>(task.Result);
            }
        }

        public static async Task<List<ProfileCourse>> GetCoursesForUser(string authToken)
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync(string.Format("api/userprofiles/getcoursesforuser?a={0}", authToken))
                                 .Result.Content.ReadAsStringAsync();
                await task;

                return JsonConvert.DeserializeObject<List<ProfileCourse>>(task.Result);
            }
        }

        public static async Task<List<SubmisionAssignment>> GetAssignmentsForCourse(int courseId, string authToken)
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync(string.Format("api/course/getassignmentsforcourse?{0}&a={1}", courseId, authToken))
                                 .Result.Content.ReadAsStringAsync();
                await task;

                return JsonConvert.DeserializeObject<List<SubmisionAssignment>>(task.Result);
            }
        }

        public static async Task<DateTime?> GetLastAssignmentSubmitDate(int assignmentId, string authToken)
        {
            using (var client = GetClient())
            {
                var task =
                    client.GetAsync(string.Format("api/course/getlastsubmitdateforassignment?{0}&a={1}", assignmentId,
                        authToken))
                        .Result.Content.ReadAsStringAsync();
                await task;

                return JsonConvert.DeserializeObject<DateTime?>(task.Result);
            }
        }

        public static async Task<DateTime> GetMostRecentSocialActivity(string authToken)
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync(string.Format("api/userprofiles/mostrecentsocialactivity?a={0}", authToken))
                    .Result.Content.ReadAsStringAsync();
                await task;

                return JsonConvert.DeserializeObject<DateTime>(task.Result);
            }
        }

        public static async Task<int> SubmitAssignment(SubmitEvent submitEvent, string authToken)
        {
            using (var client = GetClient())
            {
                var request = new SubmissionRequest
                {
                    AuthToken = authToken,
                    SubmitEvent = submitEvent
                };

                var response = await client.PostAsJsonAsync("api/eventcollection", request);
                return JsonConvert.DeserializeObject<int>(response.Content.ReadAsStringAsync().Result);
            }
        }

        public static async Task<long> SubmitLog(EventPostRequest request)
        {
            using (var client = GetClient())
            {
                var task = client.PostAsJsonAsync("api/eventcollection/post", request);
                await task;

                return task.Result.StatusCode == HttpStatusCode.OK
                                               ? JsonConvert.DeserializeObject<long>(task.Result.Content.ReadAsStringAsync().Result)
                                               : -1;
            }
        }

        public static async Task<bool> SubmitLocalErrorLog(LocalErrorLogRequest request)
        {
            using (var client = GetClient())
            {
                var task = client.PostAsJsonAsync("api/userprofiles/submitlocalerrorlog", request);
                await task;

                return task.Result.StatusCode == HttpStatusCode.OK;
            }
        }
    }
}
