using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    /// <summary>
    /// Any content stored in an Area must have "Area" prefixed to the web url.  I think
    /// that this is kind of ugly in that it exposes the file system layout to the browser.
    /// As a workaround, I've created this controller to map a more logical web-url to the
    /// raw file system.
    /// </summary>
    public class ContentController : OSBLE.Controllers.OSBLEController
    {
        private string areaPath = "/Areas/AssignmentWizard/Content/";

        /// <summary>
        /// Handles the translation of a web url to a file.  Presently just returns raw
        /// text.  I'm not sure if this will work in all cases.  Also, I'm not using
        /// any OSBLE authentication, which might want to be examined at some point.
        /// </summary>
        /// <param name="pathInfo"></param>
        /// <returns></returns>
        public string Index(string pathInfo)
        {
            string path = Request.PhysicalApplicationPath + areaPath + pathInfo;
            string content = "";
            try
            {
                using (StreamReader reader = new StreamReader(path))
                {
                    content = reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                return "File not found.";
            }
            return content;
        }

    }
}
