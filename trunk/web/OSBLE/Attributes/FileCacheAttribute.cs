// Copyright (c) 2011 Ray Liang (http://www.dotnetage.com)
// Dual licensed under the MIT and GPL licenses:
// http://www.opensource.org/licenses/mit-license.php
// http://www.gnu.org/licenses/gpl.html
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System;

namespace OSBLE.Attributes
{
    [AttributeUsage(AttributeTargets.Class | 
        AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class FileCacheAttribute : ActionFilterAttribute
    {
        private int duration = 300;
        
        public int Duration
        
        {
            get { return duration; }
            set { duration = value; }
        }
        
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var request = filterContext.RequestContext.HttpContext.Request;
            var response = filterContext.RequestContext.HttpContext.Response;
            if (request.Headers["If-Modified-Since"] != null &&
                TimeSpan.FromTicks(DateTime.Now.Ticks - DateTime.Parse(request.Headers["If-Modified-Since"]).Ticks).Seconds < Duration)
            {
                response.Write(DateTime.Now);
                response.StatusCode = 304;
                response.Headers.Add("Content-Encoding", "gzip");
                response.StatusDescription = "Not Modified";
            }
            else
            {
                base.OnActionExecuting(filterContext);
            }
        }
        
        private void SetFileCaching(HttpResponseBase response, string fileName) {
            response.AddFileDependency(fileName);
            response.Cache.SetETagFromFileDependencies();
            response.Cache.SetLastModifiedFromFileDependencies();
            response.Cache.SetCacheability(HttpCacheability.Public);
            response.Cache.SetMaxAge(new TimeSpan(7, 0, 0, 0));
            response.Cache.SetSlidingExpiration(true);
        }
        
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var result = filterContext.Result as FilePathResult;
            if (result != null)
            {
                if (!string.IsNullOrEmpty(result.FileName) && (System.IO.File.Exists(result.FileName)))
                    SetFileCaching(filterContext.HttpContext.Response, result.FileName);
            }
            base.OnActionExecuted(filterContext);
        }
    }
}