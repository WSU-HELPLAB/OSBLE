using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

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

            //assume that commas are used to denote directory hierarchy
            rootPath += "\\" + filePath.Replace(',', '\\');
            return new FileStreamResult(FileSystem.GetDocumentForRead(rootPath), "application/octet-stream");
        }

    }
}
