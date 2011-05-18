using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models;

namespace OSBLE
{
    public static class FileSystem
    {
        public static string GetRootPath()
        {
            return HttpContext.Current.Server.MapPath("~/FileSystem/");
        }

        public static string GetSchoolPath(School school)
        {
            return GetRootPath() + school.ID + "/";
        }

        public static string GetCoursePath(Course course)
        {
            //return GetSchoolPath(course.Schoo
            return "bob";
        }
    }
}