using System.Data;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Models.HomePage;
using OSBLE.Attributes;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    public class DepartmentsController : OSBLEController
    {
        //
        // GET: /Departments/
        [OsbleAuthorize]
        [IsAdmin]
        public ActionResult Index()
        {
            // The list of departments is stored in a plain text file in the 
            // root directory of the file system.
            string[] depts = null;
            OSBLE.Models.FileSystem.FileSystem fs = 
                new Models.FileSystem.FileSystem();
            string path = fs.GetPath();
            if (System.IO.Directory.Exists(path))
            {
                path = System.IO.Path.Combine(path, "departments.txt");
                if (System.IO.File.Exists(path))
                {
                    depts = System.IO.File.ReadAllLines(path);
                }
            }
            ViewBag.DepartmentList = depts;

            return View();
        }

        [HttpPost]
        public ActionResult Save()
        {
            // Get the list of departments from the form data
            string allDepts = Request.Form["taDepts"];

            if (null != allDepts)
            {
                OSBLE.Models.FileSystem.FileSystem fs = 
                new Models.FileSystem.FileSystem();
                string path = fs.GetPath();
                if (System.IO.Directory.Exists(path))
                {
                    path = System.IO.Path.Combine(path, "departments.txt");
                    System.IO.File.WriteAllText(path, allDepts);
                }
            }

            return RedirectToAction("Index", "Admin");
        }
    }
}
