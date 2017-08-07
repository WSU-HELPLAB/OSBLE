using System.Net;
using System.Net.Http;
using System.Web.Http;

using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility.Auth;

using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using OSBLEPlus.Logic.Utility;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Remoting.Contexts;
using System.Data.SqlClient;
using Dapper;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using OSBLEPlus.Services.Models;

namespace OSBLEPlus.Services.Controllers
{
    public class WordController : ApiController
    {
        /// <summary>
        /// Web api function that processes wordstats submissions
        /// </summary>
        /// <param name="request">Word statistics</param>
        /// <returns>HttpResponseMessage</returns>
        [HttpPost]
        public HttpResponseMessage Post(WordStats request)
        {
            /*
            var auth = new Authentication();
            if (!auth.IsValidKey(request.AuthToken))
                return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };
            */

            int result = 1;

            return new HttpResponseMessage
            {
                StatusCode = result > 0 ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
                Content = new StringContent(result.ToString())
            };
        }
    }
}
