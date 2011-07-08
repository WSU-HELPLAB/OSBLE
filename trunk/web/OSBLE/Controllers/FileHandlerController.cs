using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    public class FileHandlerController : OSBLEController
    {
        public ActionResult CourseDocument(int courseId, string filePath)
        {
            var course = (from c in db.CoursesUsers
                          where c.UserProfileID == currentUser.ID && c.CourseID == courseId
                          select c).FirstOrDefault();

            if (course != null)
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
            return RedirectToAction("Index", "Home");
        }

        [NotForCommunity]
        public ActionResult GetSubmissionDeliverable(int assignmentActivityID, int userProfileID, string fileName, DeliverableType type)
        {
            AbstractAssignmentActivity activity = db.AbstractAssignmentActivities.Find(assignmentActivityID);

            //rather then checking every step try catch will take care of it
            try
            {
                //If u are looking at the activeCourse and you can either see all or looking at your own let it pass
                if (activeCourse.CourseID == activity.AbstractAssignment.Category.CourseID && (activeCourse.CourseRole.CanSeeAll || currentUser.ID == userProfileID))
                {
                    var teamUser = (from c in activity.TeamUsers where c.Contains(db.UserProfiles.Find(userProfileID)) select c).FirstOrDefault();

                    string path = FileSystem.GetDeliverable(activeCourse.Course as Course, assignmentActivityID, teamUser, fileName, GetFileExtensions(type));

                    if (path != null)
                    {
                        return new FileStreamResult(FileSystem.GetDocumentForRead(path), "application/octet-stream") { FileDownloadName = new FileInfo(path).Name };
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error in GetSubmission", e);
            }

            throw new Exception("File Not Found");
        }

        public ActionResult GetSubmissionZip(int assignmentActivityID, int teamUserID)
        {
            throw new NotImplementedException();
        }
    }
}