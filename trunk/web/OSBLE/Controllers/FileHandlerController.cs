using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Ionic.Zip;
using OSBLE.Attributes;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    public class FileHandlerController : OSBLEController
    {
        public ActionResult CourseDocument(int courseId, string filePath)
        {
            var course = (from c in db.CoursesUsers
                          where c.UserProfileID == currentUser.ID && c.AbstractCourseID == courseId
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
                if (activeCourse.AbstractCourseID == activity.AbstractAssignment.Category.CourseID && (activeCourse.AbstractRole.CanSeeAll || currentUser.ID == userProfileID))
                {
                    var teamUser = (from c in activity.TeamUsers where c.Contains(db.UserProfiles.Find(userProfileID)) select c).FirstOrDefault();

                    string path = FileSystem.GetDeliverable(activeCourse.AbstractCourse as Course, assignmentActivityID, teamUser, fileName, GetFileExtensions(type));

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

        [Authorize]
        [CanGradeCourse]
        [NotForCommunity]
        public ActionResult GetAllSubmissionsForActivity(int assignmentActivityID)
        {
            AbstractAssignmentActivity acitivity = db.AbstractAssignmentActivities.Find(assignmentActivityID);

            try
            {
                if (acitivity.AbstractAssignment.Category.CourseID == activeCourse.AbstractCourseID)
                {
                    Stream stream = FileSystem.FindZipFile(activeCourse.AbstractCourse as Course, acitivity);

                    string zipFileName = acitivity.Name + ".zip";

                    if (stream != null)
                    {
                        return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = zipFileName };
                    }

                    //This can be used to simulate a long load time
                    Int64 i = 0;
                    while (i < 2000000000)
                    {
                        i++;
                    }

                    string submissionfolder = FileSystem.GetAssignmentActivitySubmissionFolder(acitivity.AbstractAssignment.Category.Course, acitivity.ID);

                    using (ZipFile zipfile = new ZipFile())
                    {
                        DirectoryInfo acitvityDirectory = new DirectoryInfo(submissionfolder);

                        if (!acitvityDirectory.Exists)
                        {
                            FileSystem.CreateZipFolder(activeCourse.AbstractCourse as Course, zipfile, acitivity);
                        }
                        else
                        {
                            foreach (DirectoryInfo submissionDirectory in acitvityDirectory.GetDirectories())
                            {
                                zipfile.AddDirectory(submissionDirectory.FullName, (from c in acitivity.TeamUsers where c.ID.ToString() == submissionDirectory.Name select c).FirstOrDefault().Name);
                            }

                            FileSystem.CreateZipFolder(activeCourse.AbstractCourse as Course, zipfile, acitivity);
                        }
                        stream = FileSystem.GetDocumentForRead(zipfile.Name);

                        return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = zipFileName };
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error in GetSubmissionZip", e);
            }
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [CanGradeCourse]
        [NotForCommunity]
        public ActionResult GetSubmissionZip(int assignmentActivityID, int teamUserID)
        {
            //This can be used to simulate a long load time
            /*Int64 i = 0;
            while (i < 2000000000)
            {
                i++;
            }*/

            AbstractAssignmentActivity acitivity = db.AbstractAssignmentActivities.Find(assignmentActivityID);

            try
            {
                TeamUserMember teamUser = db.TeamUsers.Find(teamUserID);
                if (acitivity.AbstractAssignment.Category.CourseID == activeCourse.AbstractCourseID && acitivity.TeamUsers.Contains(teamUser))
                {
                    Stream stream = FileSystem.FindZipFile(activeCourse.AbstractCourse as Course, acitivity, teamUser);

                    string zipFileName = acitivity.Name + " by " + teamUser.Name + ".zip";

                    if (stream != null)
                    {
                        return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = zipFileName };
                    }

                    //This can be used to simulate a long load time
                    /*Int64 i = 0;
                    while (i < 2000000000)
                    {
                        i++;
                    }*/

                    string submissionfolder = FileSystem.GetTeamUserSubmissionFolder(false, (activeCourse.AbstractCourse as Course), assignmentActivityID, db.TeamUsers.Find(teamUserID));

                    using (ZipFile zipfile = new ZipFile())
                    {
                        zipfile.AddDirectory(submissionfolder);

                        FileSystem.CreateZipFolder(activeCourse.AbstractCourse as Course, zipfile, acitivity, teamUser);

                        stream = FileSystem.GetDocumentForRead(zipfile.Name);

                        return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = zipFileName };
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error in GetSubmissionZip", e);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}