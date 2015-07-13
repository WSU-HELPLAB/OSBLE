using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel.Channels;
using System.ServiceModel;

namespace OSBIDE.Library.ServiceClient
{
    public static class ServiceBindings
    {
        public static EndpointAddress OsbideServiceEndpoint
        {
            get
            {
#if DEBUG
                //go to the debug endpoint address if we're in debug mode
                return LocalOsbideServiceEndpoint;
#else
                //otherwise, hit the real server
                return RemoteOsbideServiceEndpoint;
#endif
            }
        }

        public static EndpointAddress LocalOsbideServiceEndpoint
        {
            get
            {
                EndpointAddress endpoint = new EndpointAddress("http://localhost:8080/Services/OsbideWebService.svc");
                return endpoint;
            }
        }

        public static EndpointAddress RemoteOsbideServiceEndpoint
        {
            get
            {
                EndpointAddress endpoint = new EndpointAddress("http://osbide.com/Services/OsbideWebService.svc");
                return endpoint;
            }
        }

        public static Binding OsbideServiceBinding
        {
            get
            {
                BasicHttpBinding serviceBinding = new BasicHttpBinding();
                serviceBinding.Name = "OsbideWebServiceBinding";
                
                //match values with those found in web.config file inside of OSBIDE.Web
                serviceBinding.SendTimeout = new TimeSpan(0, 0, 15, 0, 0);
                serviceBinding.ReceiveTimeout = new TimeSpan(0, 0, 15, 0, 0);

                serviceBinding.MaxBufferSize = 2147483647;
                serviceBinding.MaxReceivedMessageSize = 2147483647;
                serviceBinding.MaxBufferPoolSize = 2147483647;

                serviceBinding.ReaderQuotas.MaxDepth = 2147483647;
                serviceBinding.ReaderQuotas.MaxStringContentLength = 2147483647;
                serviceBinding.ReaderQuotas.MaxArrayLength = 2147483647;
                serviceBinding.ReaderQuotas.MaxBytesPerRead = 2147483647;
                serviceBinding.ReaderQuotas.MaxNameTableCharCount = 2147483647;

                return serviceBinding;
            }
        }
    }
}
