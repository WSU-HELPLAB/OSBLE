using System;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLEPlus.Services.Models
{
    public class SubmitEventModelBinder : IModelBinder
    {
        public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType == typeof(SubmissionRequest))
            {
                try
                {
                    var jsonStr = actionContext.Request.Content.ReadAsStringAsync().Result;
                    var jsonTokens = jsonStr.Replace("\"", string.Empty).TrimStart('{').TrimEnd('}').Split(',');
                    var submitRequest = new SubmissionRequest
                    {
                        AuthToken = jsonTokens.First(x => x.Contains("AuthToken:")).Split(':').Last(),
                        SubmitEvent = new SubmitEvent
                        {
                            CourseId = Convert.ToInt32(jsonTokens.First(x => x.Contains("CourseId:")).Split(':').Last()),
                            AssignmentId =
                                Convert.ToInt32(jsonTokens.First(x => x.Contains("AssignmentId:")).Split(':').Last()),
                            SolutionName = jsonTokens.First(x => x.Contains("SolutionName:")).Split(':').Last(),
                            }
                    };

                    var encodedSolution = jsonTokens.First(x => x.Contains("RequestData:")).Split(':').Last();
                    submitRequest.SubmitEvent.CreateSolutionBinary(HttpServerUtility.UrlTokenDecode(encodedSolution));

                    bindingContext.Model = submitRequest;
                    return true;
                }
                catch (Exception ex)
                {
                    var msg = ex.Message;
                    return false;
                }
            }
            return false;
        }
    }
}