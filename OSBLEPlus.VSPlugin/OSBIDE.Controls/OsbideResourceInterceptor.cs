using System;
using System.Text.RegularExpressions;
using Awesomium.Core;
using OSBLEPlus.Logic.Utility;

namespace OSBIDE.Controls
{
    public class OsbideResourceInterceptor : IResourceInterceptor
    {
        public class ResourceInterceptorEventArgs : EventArgs
        {
            public VsComponent Component { get; set; }
            public string Url { get; set; }
        }

        public event EventHandler<ResourceInterceptorEventArgs> NavigationRequested = delegate { };
        private static OsbideResourceInterceptor _instance;

        private OsbideResourceInterceptor()
        {
        }

        public static OsbideResourceInterceptor Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new OsbideResourceInterceptor();
                }
                return _instance;
            }
        }

        public bool OnFilterNavigation(NavigationRequest request)
        {
            var pattern = @"component=([\w]+)";
            var subject = request.Url.Query;
            var match = Regex.Match(subject, pattern);

            //ignore bad matches
            if (match.Groups.Count == 2)
            {
                VsComponent component;
                if (Enum.TryParse(match.Groups[1].Value, out component) != true
                        || NavigationRequested == null)
                    return false;

                var url = Regex.Replace(request.Url.ToString(), pattern, "");
                var args = new ResourceInterceptorEventArgs()
                {
                    Component = component,
                    Url = url
                };
                NavigationRequested(this, args);
                return true;
            }
            return false;
        }

        public ResourceResponse OnRequest(ResourceRequest request)
        {
            //returning null results in normal behavior
            return null;
        }
    }
}
