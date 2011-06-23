using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace OSBLE.Controllers
{
    public class FileHandlerController : OSBLEController
    {
        //
        // GET: /FileHandler/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult CourseDocument(int courseId, string filePath)
        {
            string rootPath = FileSystem.GetCourseDocumentsPath(courseId);

            //AC: At some point, it might be a good idea to document these hacks

            //assume that commas are used to denote directory hierarchy
            rootPath += "\\" + filePath.Replace(',', '\\');

            //if the file ends in a ".link", then we need to treat it as a web link
            if (rootPath.Substring(rootPath.LastIndexOf('.') + 1).ToLower().CompareTo("link") == 0)
            {
                string url = "";

                //open the file to get at the link stored inside
                using (TextReader tr = new StreamReader(rootPath))
                {
                    url = tr.ReadLine();
                }
                Response.Redirect(url);

                //this will never be reached, but the function requires an actionresult to be returned
                return Json("");
            }
            else
            {
                //else just return the file
                return new FileStreamResult(FileSystem.GetDocumentForRead(rootPath), "application/octet-stream");
            }

        }

    }
}
