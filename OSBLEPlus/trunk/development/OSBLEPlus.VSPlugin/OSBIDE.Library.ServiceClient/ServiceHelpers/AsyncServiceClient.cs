using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using OSBLE.Models.Users;
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
            // THIS IS FOR DEBUGGING ONLY, THIS TURNS OFF HTTPS VALIDATION
            // DO NOT USE ON THE LIVE SERVER
#if DEBUG
            ServicePointManager.ServerCertificateValidationCallback =
                delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
                {
                    return true;
                };
#endif
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

        public static async Task<string> CommunityStatus()
        {
            using (var client = GetClient())
            {
                var task = client.GetStringAsync("api/community/communityenabled");
                await task;

                return task.Result.TrimEnd('"').TrimStart('"');
            }
        }

        public static async Task<DateTime> GetMostRecentWhatsNewItem()
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync("api/course/mostrecentwhatsnewitem");
                await task;

                return JsonConvert.DeserializeObject<DateTime>(task.Result.Content.ReadAsStringAsync().Result);
            }
        }

        public static async Task<string> Login(string userName, string password)
        {
            using (var client = GetClient())
            {
                // Send post data via key/value pairs in a dictionary to be deserialized into json
                // for the new HttpPost method.
                Dictionary<string, string> values = new Dictionary<string, string>()
                {
                    {"e", userName},
                    {"hp", UserProfile.GetPasswordHash(password)}
                };

                var content = new FormUrlEncodedContent(values);

                var task = client.PostAsync("api/userprofiles/login", content);

                await task;

                return task.Result.StatusCode == HttpStatusCode.OK
                                               ? task.Result.Content.ReadAsStringAsync().Result
                                               : string.Empty;
            }
        }

        public static async Task<string> Logout()
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync(string.Format("{0}/Account/LogOff", StringConstants.WebClientRoot));
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
                var task = client.GetAsync(string.Format("api/userprofiles/isvalidkey?a={0}", authToken));
                await task;

                return JsonConvert.DeserializeObject<bool>(task.Result.Content.ReadAsStringAsync().Result);
            }
        }

        public static async Task<List<ProfileCourse>> GetCoursesForUser(string authToken)
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync(string.Format("api/userprofiles/getcoursesforuser?a={0}", authToken));
                await task;

                return JsonConvert.DeserializeObject<List<ProfileCourse>>(task.Result.Content.ReadAsStringAsync().Result);
            }
        }

        public static async Task<List<SubmisionAssignment>> GetAssignmentsForCourse(int courseId, string authToken)
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync(string.Format("api/course/getassignmentsforcourse?id={0}&a={1}", courseId, authToken));
                await task;
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter> { new AssignmentJsonConverter() }
                };
                return JsonConvert.DeserializeObject<List<SubmisionAssignment>>(task.Result.Content.ReadAsStringAsync().Result);
            }
        }

        public static async Task<DateTime?> GetLastAssignmentSubmitDate(int assignmentId, string authToken)
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync(string.Format("api/course/getlastsubmitdateforassignment?id={0}&a={1}", assignmentId, authToken));
                await task;

                return JsonConvert.DeserializeObject<DateTime?>(task.Result.Content.ReadAsStringAsync().Result);
            }
        }

        public static async Task<DateTime> GetMostRecentSocialActivity(string authToken)
        {
            using (var client = GetClient())
            {
                var task = client.GetAsync(string.Format("api/userprofiles/mostrecentsocialactivity?a={0}", authToken));
                await task;

                return JsonConvert.DeserializeObject<DateTime>(task.Result.Content.ReadAsStringAsync().Result);
            }
        }

        public static async Task<int> SubmitAssignment(SubmitEvent submitEvent, string authToken)
        {
            using (var client = GetClient())
            {
                var request = new SubmissionRequest
                {
                    AuthToken = authToken,
                    SubmitEvent = submitEvent,
                };
                var response = await client.PostAsXmlAsync("api/course/post", request);

                return response.IsSuccessStatusCode
                        ? JsonConvert.DeserializeObject<int>(response.Content.ReadAsStringAsync().Result)
                        : 0;
            }
        }

        public static async Task<int> SubmitLog(EventPostRequest request)
        {
            using (var client = GetClient())
            {
                try
                {
                    var task = client.PostAsXmlAsync("api/eventcollection/post", request);
                    await task;

                    var result = JsonConvert.DeserializeObject<int>(task.Result.Content.ReadAsStringAsync().Result);
                    return result;
                }
                catch (Exception)
                {
                    return 0;
                }



            }
        }

        public static async Task<bool> SubmitLocalErrorLog(LocalErrorLogRequest request)
        {
            using (var client = GetClient())
            {
                try
                {
                    var task = client.PostAsJsonAsync("api/userprofiles/submitlocalerrorlog", request);
                    await task;
                    return task.Result.StatusCode == HttpStatusCode.OK;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Used to check if there are new interventions ready for the user.
        /// </summary>
        /// <returns>returns true/false</returns>
        public static async Task<string> ProcessIntervention(string authToken)
        {
            try
            {
                using (var client = GetClient())
                {
                    var task = client.GetStringAsync(string.Format("api/intervention/RefreshInterventions?authToken={0}", authToken));
                    await task;

                    return task.Result.TrimEnd('"').TrimStart('"');
                }
            }
            catch (Exception)
            {
                return "false";
            }

        }

        public static async Task<string> InterventionsEnabled()
        {
            try
            {
                using (var client = GetClient())
                {
                    var task = client.GetStringAsync("api/intervention/InterventionsEnabled");
                    await task;

                    return task.Result.TrimEnd('"').TrimStart('"');
                }
            }
            catch (Exception)
            {
                return "false";
            }
        }

        public static async Task<int> GetInterventionRefreshThresholdValue()
        {
            try
            {
                using (var client = GetClient())
                {
                    var task = client.GetStringAsync(string.Format("{0}API/Intervention/InterventionRefreshThreshold", StringConstants.DataServiceRoot));
                    await task;
                    return int.Parse(task.Result);
                }
            }
            catch (Exception e)
            {
                return 5;
            }
        }
    }
}
