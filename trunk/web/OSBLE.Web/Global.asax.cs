using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace OSBLE
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
#if DEBUG
            //Development only.
            //System.Data.Entity.Database.SetInitializer(new OSBLE.Models.OSBLEContextModelChangeInitializer());
            System.Data.Entity.Database.SetInitializer(new OSBLE.Models.OSBLEContextAlwaysCreateInitializer());
#endif

            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ConnectionStringSettings mySetting = ConfigurationManager.ConnectionStrings["StorageConnectionString"];
            string connection = mySetting.ToString();
            
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connection);
            CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer filesystem = cloudBlobClient.GetContainerReference("filesystem");

            filesystem.CreateIfNotExist();

        }
    }
}