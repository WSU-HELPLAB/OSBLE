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
            OSBLE.Models.FileSystem.OSBLEDirectory fs =
                Models.FileSystem.Directories.GetAdmin();
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

        [HttpPost, IsAdmin]
        public ActionResult Save()
        {
            // Get the list of departments from the form data
            string allDepts = Request.Form["taDepts"];

            if (null != allDepts)
            {
                OSBLE.Models.FileSystem.OSBLEDirectory fs =
                    Models.FileSystem.Directories.GetAdmin();
                fs.AddFile("departments.txt",
                    System.Text.Encoding.UTF8.GetBytes(allDepts));
            }

            return RedirectToAction("Index", "Admin");
        }
    }
}
