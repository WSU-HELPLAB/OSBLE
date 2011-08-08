using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace OSBLE.Models
{
    public class OsbleViewEngine : RazorViewEngine
    {
        //Add custom view paths here
        private static string[] ViewLocations = new[]{
            "~/Views/Assignments/{1}/{0}.cshtml",
            "~/Views/Assignments/{1}/{0}.vbhtml",
            "~/Views/Assignments/Wizard/{1}/{0}.cshtml",
            "~/Views/Assignments/Wizard/{1}/{0}.vbhtml"
        };

        public OsbleViewEngine()
            : base()
        {
            base.ViewLocationFormats = base.ViewLocationFormats.Union(ViewLocations).ToArray();
        }
    }
}