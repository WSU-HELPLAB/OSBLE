using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web.Mvc;
using Ionic.Zip;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using System.Collections.Generic;
using OSBLE.Models.Users;
using System.Net;
using System.Configuration;
using OSBLE.Utility;
using OSBLE.Models.Annotate;

namespace OSBLE.Controllers
{
    public class FileHandlerController : OSBLEController
    {
        [OsbleAuthorize]
        [RequireActiveCourse]
        public ActionResult CourseDocument(int courseId, string filePath)
        {
            var course = (from c in db.CourseUsers
                          where c.UserProfileID == CurrentUser.ID && c.AbstractCourseID == courseId
                          select c).FirstOrDefault();

            if (course != null)
            {

                string rootPath = FileSystem.GetCourseDocumentsPath(courseId);

                //AC: At some point, it might be a good idea to document these hacks

                //assume that commas are used to denote directory hierarchy
                rootPath += "\\" + filePath.Replace('@', '\\');

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

        [OsbleAuthorize]
        [RequireActiveCourse]
        [NotForCommunity]
        public ActionResult GetSubmissionDeliverable(int assignmentID, int teamID, string fileName)
        {
            try
            {
                int currentUserID = CurrentUser.ID;
                Assignment assignment = db.Assignments.Find(assignmentID);
                AssignmentTeam team = db.AssignmentTeams.Find(assignmentID, teamID);
                TeamMember teamMember = (from teamMembers in team.Team.TeamMembers
                                         where teamMembers.CourseUser.UserProfileID == CurrentUser.ID
                                         select teamMembers).FirstOrDefault();
                //make sure assignmentActivity is part of the activeCourse and (the person can grade  or is allowed access to it)
                if (assignment.CourseID == ActiveCourseUser.AbstractCourseID && (ActiveCourseUser.AbstractRole.CanGrade || team.Team.TeamMembers.Contains(teamMember)))
                {
                    string path = FileSystem.GetDeliverable(ActiveCourseUser.AbstractCourse as Course, assignmentID, team, fileName);
                    return new FileStreamResult(FileSystem.GetDocumentForRead(path), "application/octet-stream") { FileDownloadName = new FileInfo(path).Name };
                }
            }
            catch
            { }
            //either not authorized or bad parameters were passed in
            throw new Exception();
        }

        [OsbleAuthorize]
        [RequireActiveCourse]
        [NotForCommunity]
        public ActionResult GetSubmissionDeliverableByType(int assignmentID, int userProfileID, string fileName, DeliverableType type)
        {
            Assignment assignment = db.Assignments.Find(assignmentID);
            AssignmentTeam assignmentTeam = new AssignmentTeam();
            TeamMember teamMember = new TeamMember();
            foreach (AssignmentTeam at in assignment.AssignmentTeams)
            {
                foreach (TeamMember tm in at.Team.TeamMembers)
                {
                    if (tm.CourseUser.UserProfileID == userProfileID)
                    {
                        assignmentTeam = at;
                        teamMember = tm;
                    }
                }
            }

            //rather then checking every step try catch will take care of it
            try
            {
                //If u are looking at the activeCourse and you can either see all or looking at your own let it pass
                if (ActiveCourseUser.AbstractCourseID == assignment.CourseID && (ActiveCourseUser.AbstractRole.CanSeeAll || CurrentUser.ID == userProfileID))
                {
                    //var teamUser = (from c in activity.TeamUsers where c.Contains(db.UserProfiles.Find(userProfileID)) select c).FirstOrDefault();

                    string path = FileSystem.GetDeliverable(ActiveCourseUser.AbstractCourse as Course, assignmentID, assignmentTeam, fileName, GetFileExtensions(type));

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

        [OsbleAuthorize]
        [RequireActiveCourse]
        [CanGradeCourse]
        [NotForCommunity]
        public ActionResult GetAllSubmissionsForActivity(int assignmentID)
        {
            Assignment assignment = db.Assignments.Find(assignmentID);

            try
            {
                if (assignment.CourseID == ActiveCourseUser.AbstractCourseID)
                {
                    Stream stream = FileSystem.FindZipFile(ActiveCourseUser.AbstractCourse as Course, assignment);

                    string zipFileName = assignment.AssignmentName + ".zip";

                    if (stream != null)
                    {
                        return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = zipFileName };
                    }

                    string submissionfolder = FileSystem.GetAssignmentSubmissionFolder(assignment.Course, assignment.ID);

                    using (ZipFile zipfile = new ZipFile())
                    {
                        DirectoryInfo acitvityDirectory = new DirectoryInfo(submissionfolder);

                        if (!acitvityDirectory.Exists)
                        {
                            FileSystem.CreateZipFolder(ActiveCourseUser.AbstractCourse as Course, zipfile, assignment);
                        }
                        else
                        {
                            foreach (DirectoryInfo submissionDirectory in acitvityDirectory.GetDirectories())
                            {
                                Team currentTeam = (from c in assignment.AssignmentTeams where c.TeamID.ToString() == submissionDirectory.Name select c.Team).FirstOrDefault();

                                if (currentTeam != null)
                                {
                                    string folderName = "";
                                    if (assignment.HasTeams)
                                    {
                                        folderName = currentTeam.Name;
                                    }
                                    else
                                    {
                                        folderName = currentTeam.TeamMembers.FirstOrDefault().CourseUser.DisplayName(ActiveCourseUser.AbstractRoleID);
                                    }

                                    zipfile.AddDirectory(submissionDirectory.FullName, folderName);
                                }
                            }

                            FileSystem.CreateZipFolder(ActiveCourseUser.AbstractCourse as Course, zipfile, assignment);
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

        [OsbleAuthorize]
        [RequireActiveCourse]
        [NotForCommunity]
        public ActionResult GetTeamUserPeerReview(int assignmentID, int teamID)
        {
            try
            {
                //AbstractAssignmentActivity activity = db.AbstractAssignmentActivities.Find(assignmentActivityID);
                Assignment assignment = db.Assignments.Find(assignmentID);
                //TeamUserMember teamUser = db.TeamUsers.Find(teamUserID);
                AssignmentTeam at = (from a in db.AssignmentTeams
                                     where a.TeamID == teamID &&
                                     a.AssignmentID == assignment.ID
                                     select a).FirstOrDefault();

                if ((assignment.CourseID == ActiveCourseUser.AbstractCourseID))
                {
                    if (ActiveCourseUser.AbstractRole.CanGrade)
                    {
                        //if we are dealing with a teacher first give them the published but if it doesn't exists give them a draft if that doesn't exist give em nothing
                        string path = FileSystem.GetTeamUserPeerReview(false, ActiveCourseUser.AbstractCourse as Course, assignment.ID, at.TeamID);
                        if (new FileInfo(path).Exists)
                        {
                            return new FileStreamResult(FileSystem.GetDocumentForRead(path), "application/octet-stream") { FileDownloadName = new FileInfo(path).Name };
                        }
                        else
                        {
                            path = FileSystem.GetTeamUserPeerReviewDraft(false, ActiveCourseUser.AbstractCourse as Course, assignment.ID, at.TeamID);
                            if (new FileInfo(path).Exists)
                            {
                                return new FileStreamResult(FileSystem.GetDocumentForRead(path), "application/octet-stream") { FileDownloadName = new FileInfo(path).Name };
                            }
                        }
                    }
                    else if (ActiveCourseUser.AbstractRole.CanSubmit)
                    {
                        foreach (TeamMember tm in at.Team.TeamMembers)
                        {
                            if (tm.CourseUser.UserProfileID == CurrentUser.ID)
                            {
                                //if we are dealing with student try to give them the published one but if that doesn't exist give them nothing
                                string path = FileSystem.GetTeamUserPeerReview(false, ActiveCourseUser.AbstractCourse as Course, assignment.ID, at.TeamID);
                                return new FileStreamResult(FileSystem.GetDocumentForRead(path), "application/octet-stream") { FileDownloadName = new FileInfo(path).Name };
                            }
                        }
                    }
                }
            }
            catch
            { }
            //either not authorized or bad parameters were passed in
            throw new Exception();
        }

        [OsbleAuthorize]
        [RequireActiveCourse]
        [CanGradeCourse]
        [NotForCommunity]
        public ActionResult GetSubmissionZip(int assignmentId, int teamId)
        {
            //basic assignments have the option of being annotatable.  In this case,
            //send off to annotate rather than creating a zip file.
            Assignment assignment = db.Assignments.Find(assignmentId);
            if (assignment.Type == AssignmentTypes.Basic && assignment.IsAnnotatable == true)
            {
                if (assignment.HasDeliverables && assignment.Deliverables[0].DeliverableType == DeliverableType.PDF)
                {
                    return RedirectToRoute(new { controller = "PdfCriticalReview", action = "Grade", assignmentID = assignmentId, authorTeamID = teamId });
                }
            }

            return GetSubmissionZipHelper(assignmentId, teamId);
        }

        public ActionResult GetAnnotateDocument(int assignmentID, int authorTeamID, string apiKey)
        {
            if (apiKey != ConfigurationManager.AppSettings["AnnotateApiKey"])
            {
                return RedirectToAction("Index", "Home");
            }

            Assignment assignment = db.Assignments.Find(assignmentID);

            //add in some robustness.  If we were passed in a critical review,
            //get the preceeding assignment.  Otherwise, just use the current assignment.
            if (assignment.Type == AssignmentTypes.CriticalReview)
            {
                assignment = assignment.PreceedingAssignment;
            }

            AssignmentTeam assignmentTeam = (from at in db.AssignmentTeams
                                             where at.AssignmentID == assignment.ID
                                             &&
                                             at.TeamID == authorTeamID
                                             select at
                                             ).FirstOrDefault();

            string path = FileSystem.GetDeliverable(
                assignment.Course as Course,
                assignment.ID,
                assignmentTeam,
                assignment.Deliverables[0].ToString()
                );
            string fileName = AnnotateApi.GetAnnotateDocumentName(assignment.ID, authorTeamID);
            return new FileStreamResult(FileSystem.GetDocumentForRead(path), "application/octet-stream") { FileDownloadName = fileName };
        }

        /// <summary>
        /// Get deliverables from the reviewee's assignment (previous assignment) for the current user, 
        /// needed for a critical review assignment (current user)
        /// </summary>
        /// <param name="assignmentId">critical review assignment ID</param>
        /// <param name="authorTeamId">reviewee team ID</param>
        /// <returns></returns>
        [CanSubmitAssignments]
        [OsbleAuthorize]
        [RequireActiveCourse]
        public ActionResult GetPrecedingSubmissionForCriticalReview(int assignmentId, int authorTeamId)
        {
            Assignment CRassignment = db.Assignments.Find(assignmentId);
            AssignmentTeam at = GetAssignmentTeam(CRassignment, ActiveCourseUser);
            List<int> authorTeams = (from rt in CRassignment.ReviewTeams
                                     where rt.ReviewTeamID == at.TeamID
                                     select rt.AuthorTeamID).ToList();
            if (authorTeams.Contains(authorTeamId) && CRassignment.Type == AssignmentTypes.CriticalReview)
            {
                //AC TODO: Error checking!

                //Send off to Annotate if we have exactly one deliverable and that deliverable is a PDF document
                if (CRassignment.PreceedingAssignment.Deliverables.Count == 1 && CRassignment.PreceedingAssignment.Deliverables[0].DeliverableType == DeliverableType.PDF)
                {
                    return RedirectToRoute(new { controller = "PdfCriticalReview", action = "Review", assignmentID = assignmentId, authorTeamID = authorTeamId });
                }
                else
                {
                    return GetSubmissionZipHelper((int)CRassignment.PrecededingAssignmentID, authorTeamId);
                }
            }

            return RedirectToAction("Index", "Home");
        }
        /// <summary>
        /// get a zip containing reviews that the review (current user) has performed on the 
        /// author (authorTeamId) for a speicific assignment.
        /// </summary>
        /// <param name="assignmentId">get reviews of submissions of this assignment</param>
        /// <param name="authorTeamId">get the review of this authorTeam (that is submitted by the current user)</param>
        /// <returns></returns>
        [CanSubmitAssignments]
        [OsbleAuthorize]
        [RequireActiveCourse]
        public ActionResult GetReviewForAuthor(int assignmentId, int authorTeamId)
        {
            //get authorTeam
            Team authorTeam = db.Teams.Find(authorTeamId);
            //Assignment

            //bool isOwnDocument = false;
            //foreach (TeamMember tm in authorTeam.TeamMembers)
            //{
            //    if (tm.CourseUserID == ActiveCourseUser.ID)
            //    {
            //        isOwnDocument = true;
            //        break;
            //    }
            //}

            //if (isOwnDocument)
            //{
            Assignment CRassignment = db.Assignments.Find(assignmentId);
            AssignmentTeam at = GetAssignmentTeam(CRassignment, ActiveCourseUser);

            return GetSubmissionZipHelper(assignmentId, at.TeamID, authorTeam);
            //}

            //return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// get a zip containing reviews that have been done 
        /// to the the author's (receiverId) preceding assignment 
        /// </summary>
        /// <param name="assignmentId">assignment ID of the critical review</param>
        /// <param name="receiverId">This is the CourseUser you want to download received reviews for. 
        /// If it is team based, any course user in the preceding assignment team will yield the same results</param>
        /// <returns></returns>
        [OsbleAuthorize]
        [RequireActiveCourse]
        public ActionResult GetReviewsOfAuthor(int assignmentId, int receiverId)
        {

            Assignment assignment = db.Assignments.Find(assignmentId);
            CourseUser receiver = db.CourseUsers.Find(receiverId);
            AssignmentTeam previousAssignmentTeam = GetAssignmentTeam(assignment.PreceedingAssignment, receiver);

            if (ActiveCourseUser.AbstractRole.CanModify || (receiverId == ActiveCourseUser.ID && assignment.IsCriticalReviewPublished))
            {
                return GetReviewsOfAuthorHelper(assignment, receiverId);
            }
            return RedirectToAction("Index", "Home");
        }

        [OsbleAuthorize]
        [RequireActiveCourse]
        public ActionResult GetCriticalReviewDiscussionItems(int discussionTeamID)
        {
            DiscussionTeam dt = db.DiscussionTeams.Find(discussionTeamID);

            Assignment precedingAssignment = dt.Assignment.PreceedingAssignment;

            //Permission checking:
            // assert that the activeCourseUser is a member of the discussion team of discussionTeamID
            // and confirm that the critical review to be downloaded has been published
            // note: these contraints do not apply to instructors
            bool belongsToDT = false;

            foreach (TeamMember tm in dt.GetAllTeamMembers())
            {
                if (tm.CourseUserID == ActiveCourseUser.ID)
                {
                    belongsToDT = true;
                    break;
                }
            }

            if (ActiveCourseUser.AbstractRole.CanModify || (belongsToDT && precedingAssignment.IsCriticalReviewPublished))
            {
                return GetReviewsOfAuthorHelper(precedingAssignment, dt.AuthorTeam.TeamMembers.FirstOrDefault().CourseUserID);
            }
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// get a zip containing reviews that have been done 
        /// to the the author's (receiverId) preceding assignment
        /// </summary>
        /// <param name="assignment">assignment of the critical review</param>
        /// <param name="receiverId">This is the CourseUser you want to download received reviews for. 
        /// If it is team based, any course user in the preceding assignment team will yield the same results</param>
        /// <returns></returns>
        private ActionResult GetReviewsOfAuthorHelper(Assignment assignment, int receiverId)
        {
            CourseUser receiver = db.CourseUsers.Find(receiverId);
            AssignmentTeam previousAssignmentTeam = GetAssignmentTeam(assignment.PreceedingAssignment, receiver);

            //critical reviews of PDF documents aren't zipped.  Instead,
            //they are sent to annotate.  In this case, we need to redirect
            //to a different location
            if (assignment.Type == AssignmentTypes.CriticalReview)
            {
                Assignment previousAssignment = assignment.PreceedingAssignment;
                if (previousAssignment.HasDeliverables && previousAssignment.Deliverables[0].DeliverableType == DeliverableType.PDF)
                {
                    return RedirectToRoute(new { controller = "PdfCriticalReview", action = "Review", assignmentID = assignment.ID, authorTeamID = previousAssignmentTeam.TeamID });
                }
            }

            //REVIEW TODO: Explain the hack that you're using below.
            Stream stream = FileSystem.FindZipFile(ActiveCourseUser.AbstractCourse as Course, assignment, previousAssignmentTeam);
            string zipFileName = "Critical Review of " + previousAssignmentTeam.Team.Name + ".zip";

            //zip file already exists, no need to create a new one
            if (stream != null)
            {
                return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = zipFileName };
            }

            ZipFile zipfile = new ZipFile();
            string submissionFolder;

            List<int> teamsReviewingIds = (from rt in assignment.ReviewTeams
                                           where rt.AuthorTeamID == previousAssignmentTeam.TeamID
                                           select rt.ReviewTeamID).ToList();

            List<AssignmentTeam> ReviewingAssignmentTeams = (from at in assignment.AssignmentTeams
                                                             where teamsReviewingIds.Contains(at.TeamID)
                                                             select at).ToList();

            Dictionary<string, MemoryStream> parentStreams = new Dictionary<string, MemoryStream>();
            Dictionary<string, MemoryStream> outputStreams = new Dictionary<string, MemoryStream>();

            foreach (AssignmentTeam at in ReviewingAssignmentTeams)
            {
                submissionFolder = FileSystem.GetTeamUserSubmissionFolderForAuthorID(false, (ActiveCourseUser.AbstractCourse as Course), assignment.ID, at, previousAssignmentTeam.Team);
                if (new DirectoryInfo(submissionFolder).Exists)
                {
                    zipfile.AddDirectory(submissionFolder, at.Team.Name);

                    foreach (string filePath in Directory.EnumerateFiles(submissionFolder))
                    {
                        if (Path.GetExtension(filePath) == ".cpml")
                        {
                            //Step 1: Get original document as file stream
                            string originalFile = FileSystem.GetDeliverable((ActiveCourseUser.AbstractCourse as Course),
                                (int)assignment.PrecededingAssignmentID,
                                previousAssignmentTeam,
                                Path.GetFileName(filePath));

                            if (!parentStreams.ContainsKey(originalFile))
                            {
                                FileStream fs = System.IO.File.OpenRead(originalFile);
                                parentStreams[originalFile] = new MemoryStream();
                                fs.CopyTo(parentStreams[originalFile]);
                                fs.Close();
                            }

                            outputStreams[originalFile] = new MemoryStream();


                            //open student's review file
                            FileStream studentReview = System.IO.File.OpenRead(filePath);

                            //merge the file
                            ChemProV.Core.CommentMerger.Merge(parentStreams[originalFile], previousAssignmentTeam.Team.Name, studentReview, at.Team.Name, outputStreams[originalFile]);

                            //merge output back into the parent
                            parentStreams[originalFile] = new MemoryStream();
                            outputStreams[originalFile].Seek(0, SeekOrigin.Begin);
                            outputStreams[originalFile].CopyTo(parentStreams[originalFile]);
                            outputStreams[originalFile].Close();


                        }
                    }
                }
            }

            foreach (string file in parentStreams.Keys.ToList())
            {
                parentStreams[file].Seek(0, SeekOrigin.Begin);
                zipfile.AddEntry(Path.GetFileNameWithoutExtension(file) + "_MergedReview.cpml", parentStreams[file]);
            }

            FileSystem.CreateZipFolder((ActiveCourseUser.AbstractCourse as Course),
                                zipfile,
                                assignment,
                                previousAssignmentTeam);

            stream = FileSystem.GetDocumentForRead(zipfile.Name);
            return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = zipFileName };

            return RedirectToAction("Index", "Home");
        }

        private ActionResult GetSubmissionZipHelper(int assignmentID, int teamID, Team authorTeam = null)
        {
            Assignment assignment = db.Assignments.Find(assignmentID);

            try
            {
                AssignmentTeam assignmentTeam = (from a in db.AssignmentTeams
                                                 where a.TeamID == teamID &&
                                                 a.AssignmentID == assignment.ID
                                                 select a).FirstOrDefault();//db.AssignmentTeams.Find(teamID);

                if (assignment.CourseID == ActiveCourseUser.AbstractCourseID && assignment.AssignmentTeams.Contains(assignmentTeam))
                {

                    string zipFileName = assignment.AssignmentName + " by " + assignmentTeam.Team.Name + ".zip";

                    string submissionfolder;
                    using (ZipFile zipfile = new ZipFile())
                    {
                        if (assignment.Type == AssignmentTypes.CriticalReview && authorTeam != null)
                        {
                            submissionfolder = FileSystem.GetTeamUserSubmissionFolderForAuthorID(false, (ActiveCourseUser.AbstractCourse as Course), assignmentID, assignmentTeam, authorTeam);

                            if (new DirectoryInfo(submissionfolder).Exists)
                            {
                                zipfile.AddDirectory(submissionfolder);
                            }
                        }
                        else
                        {
                            //AC Note: work in progress
                            List<ReviewTeam> reviewTeams = (from rt in db.ReviewTeams
                                                            where rt.AssignmentID == assignmentID
                                                            && rt.ReviewTeamID == assignmentTeam.TeamID
                                                            select rt).ToList();
                            Dictionary<string, dynamic> reviewStreams = new Dictionary<string, dynamic>();
                            foreach (ReviewTeam reviewTeam in reviewTeams)
                            {
                                string key = reviewTeam.ReviewingTeam.Name;
                                OSBLE.Models.FileSystem.FileSystem fs = new Models.FileSystem.FileSystem();
                                OSBLE.Models.FileSystem.FileCollection fc = fs.Course(ActiveCourseUser.AbstractCourseID)
                                                                            .Assignment(assignmentID)
                                                                            .Review(reviewTeam.AuthorTeam, reviewTeam.ReviewingTeam)
                                                                            .AllFiles();
                                
                                //don't create a zip if we have have nothing to zip.
                                if (fc.Count > 0)
                                {
                                    var bytes = fc.ToBytes();
                                    reviewStreams[key] = bytes;
                                }
                            }
                            /*
                            
                             * */
                            submissionfolder = FileSystem.GetTeamUserSubmissionFolder(false, (ActiveCourseUser.AbstractCourse as Course), assignmentID, assignmentTeam);
                        }

                        using (MemoryStream stream = new MemoryStream())
                        {
                            zipfile.Save(stream);
                            stream.Position = 0;
                            return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = zipFileName };
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Error in GetSubmissionZip", e);
            }
            return RedirectToAction("Index", "Home");
        }


        [NotForCommunity]
        [OsbleAuthorize]
        [RequireActiveCourse]
        public ActionResult getCurrentUsersZip(int assignmentID)
        {
            Assignment assignment = db.Assignments.Find(assignmentID);

            AssignmentTeam at = GetAssignmentTeam(assignment, ActiveCourseUser);

            try
            {
                AssignmentTeam assignmentTeam = (from a in db.AssignmentTeams
                                                 where a.TeamID == at.TeamID &&
                                                 a.AssignmentID == assignment.ID
                                                 select a).FirstOrDefault();//db.AssignmentTeams.Find(teamID);
                if (assignment.CourseID == ActiveCourseUser.AbstractCourseID && assignment.AssignmentTeams.Contains(assignmentTeam))
                {
                    Stream stream = FileSystem.FindZipFile(ActiveCourseUser.AbstractCourse as Course, assignment, assignmentTeam);

                    string zipFileName = assignment.AssignmentName + " by " + assignmentTeam.Team.Name + ".zip";

                    if (stream != null)
                    {
                        return new FileStreamResult(stream, "application/octet-stream") { FileDownloadName = zipFileName };
                    }

                    string submissionfolder = FileSystem.GetTeamUserSubmissionFolder(false, (ActiveCourseUser.AbstractCourse as Course), assignmentID, assignmentTeam);

                    using (ZipFile zipfile = new ZipFile())
                    {
                        if (new DirectoryInfo(submissionfolder).Exists)
                        {
                            zipfile.AddDirectory(submissionfolder);
                        }
                        FileSystem.CreateZipFolder(ActiveCourseUser.AbstractCourse as Course, zipfile, assignment, assignmentTeam);

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
