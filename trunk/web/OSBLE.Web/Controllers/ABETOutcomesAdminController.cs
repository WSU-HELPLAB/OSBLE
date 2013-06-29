using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using OSBLE.Models.HomePage;
using OSBLE.Attributes;
using OSBLE.Models;

namespace OSBLE.Controllers
{
    public class ABETOutcomesAdminController : OSBLEController
    {
        //
        // GET: /ABETOutcomes/
        [OsbleAuthorize]
        [IsAdmin]
        public ActionResult Index()
        {
            // Although the administrative link for the ABET outcomes editor 
            // shouldn't even appear if the departments list isn't made, we 
            // check for that here anyway.
            StringBuilder jsArr = new StringBuilder("var ABET_existing_outcomes = new Array();");
            jsArr.AppendLine();
            string[] depts = null;
            Dictionary<string, string> existing = new Dictionary<string, string>();
            OSBLE.Models.FileSystem.OSBLEDirectory fs =
                OSBLE.Models.FileSystem.Directories.GetAdmin();
            string path = fs.GetPath();
            int i = 0;
            if (System.IO.Directory.Exists(path))
            {
                path = System.IO.Path.Combine(path, "departments.txt");
                if (System.IO.File.Exists(path))
                {
                    depts = System.IO.File.ReadAllLines(path);

                    foreach (string dept in depts)
                    {
                        existing[dept] = LoadABETOptions(dept);

                        jsArr.AppendFormat(
                            "ABET_existing_outcomes[{0}] = {1};",
                            i++, MakeJSLinesArray(existing[dept]));
                        jsArr.AppendLine();
                    }
                }
            }

            // Set the department list and options dictionary so 
            // that the view can use it
            ViewBag.DepartmentList = depts;
            ViewBag.OptionsDictionary = existing;
            ViewBag.JSDeptOpts = jsArr.ToString();
            
            return View();
        }

        private string LoadABETOptions(string departmentName)
        {
            OSBLE.Models.FileSystem.OSBLEDirectory fs =
                Models.FileSystem.Directories.GetAdmin();
            string path = fs.GetPath();
            if (System.IO.Directory.Exists(path))
            {
                // We're expecting a text file with a name in the format:
                // [department]_abet_outcomes.txt
                path = System.IO.Path.Combine(
                    path, departmentName + "_abet_outcomes.txt");
                if (System.IO.File.Exists(path))
                {
                    return System.IO.File.ReadAllText(path);
                }
            }

            return string.Empty;
        }

        private static string MakeJSLinesArray(string linesString)
        {
            StringBuilder sb = new StringBuilder("new Array(");
            string[] lines;
            if (linesString.Contains("\r\n"))
            {
                lines = linesString.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                lines = linesString.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }

            for (int i = 0; i < lines.Length; i++)
            {
                sb.AppendFormat("\"{0}\"{1}", lines[i].Replace("\"", "\\\""), 
                    (lines.Length - 1 == i) ? string.Empty : ", ");
            }
            sb.Append(")");

            return sb.ToString();
        }

        [HttpPost, IsAdmin]
        public ActionResult Save()
        {
            // For the save we'll be dumping all the text from the text area 
            // into a text file in the root file system.
            
            // Get the department from the form data
            string dept = Request.Form["slctDepartment"];

            if (string.IsNullOrEmpty(dept))
            {
                throw new Exception(
                    "Was expecting a valid department from the form data but instead " +
                    "got a null or empty string. Make sure that the department list " +
                    "file does not have blank lines.");
            }

            // Get the list of ABET options from the form data
            string opts = Request.Form["taOptions"];
            if (null == opts)
            {
                // Empty strings are OK, null strings are not
                throw new Exception("Form data missing \"taOptions\".");
            }

            // Write the file
            OSBLE.Models.FileSystem.OSBLEDirectory fs =
                Models.FileSystem.Directories.GetAdmin();
            fs.AddFile(dept + "_abet_outcomes.txt",
                System.Text.Encoding.UTF8.GetBytes(opts));

            // Go back to the main admin page
            return RedirectToAction("Index", "Admin");
        }
    }
}
