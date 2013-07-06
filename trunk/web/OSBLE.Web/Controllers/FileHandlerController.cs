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
using OSBLE.Services;
using OSBLE.Models.FileSystem;

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
                //build the file path
                string[] pathPieces = filePath.Split('/');
                OSBLEDirectory fsPath = Models.FileSystem.Directories.GetCourseDocs(courseId);
                for (int i = 0; i < pathPieces.Length - 1; i++)
                {
                    fsPath = fsPath.GetDir(pathPieces[i]);
                }
                string fullPath = fsPath.File(pathPieces[pathPieces.Length - 1]).FirstOrDefault();
                string fileName = Path.GetFileName(fullPath);

                //if the file ends in a ".link", then we need to treat it as a web link
                if (Path.GetExtension(fileName).ToLower().CompareTo(".link") == 0)
                {
                    string url = "";

                    //open the file to get at the link stored inside
                    using (TextReader tr = new StreamReader(fullPath))
                    {
                        url = tr.ReadLine();
                    }
                    Response.Redirect(url);

                    //this will never be reached, but the function requires an actionresult to be returned
                    return Json("");
                }
                else
                {
                    Stream fileStream = null;
                    try
                    {
                        fileStream = fsPath.File(pathPieces[pathPieces.Length - 1]).ToStreams().FirstOrDefault().Value;
                    }
                    catch (Exception)
                    {
                        //file not found
                    }
                    if (fileStream == null)
                    {
                        return RedirectToAction("Index", "Home");
                    }

                    //else just return the file
                    if (Path.GetExtension(filePath).ToLower() == "pdf")
                    {
                        return new FileStreamResult(fileStream, "application/pdf") { FileDownloadName = fileName };
                    }
                    else
                    {
                        return new FileStreamResult(fileStream, "application/octet-stream") { FileDownloadName = fileName };
                    }
                }
            }
            return RedirectToAction("Index", "Home");
        }


        /// <summary>
        /// Returns all the submissions for the given assignmentId
        /// </summary>
        /// <param name="assignmentID"></param>
        /// <returns></returns>
        [OsbleAuthorize]
        [RequireActiveCourse]
        [CanGradeCourse]
        [NotForCommunity]
        public ActionResult GetAllSubmissionsForAssignment(int assignmentID)
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

        /// <summary>
        /// For the given team id and assignment id, it returns the submission. (Including critical reviews performed)
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="teamId"></param>
        /// <returns></returns>
        [OsbleAuthorize]
        [RequireActiveCourse]
        [CanGradeCourse]
        [NotForCommunity]
        public ActionResult GetSubmissionZip(int assignmentId, int teamId)
        {

            //basic assignments have the option of being annotatable.  In this case,
            //send off to annotate rather than creating a zip file.
            Assignment assignment = db.Assignments.Find(assignmentId);
            Team team = db.Teams.Find(teamId);
            if (assignment.Type == AssignmentTypes.Basic && assignment.IsAnnotatable == true)
            {
                if (assignment.HasDeliverables && assignment.Deliverables[0].DeliverableType == DeliverableType.PDF)
                {
                    return RedirectToRoute(new { controller = "PdfCriticalReview", action = "Grade", assignmentID = assignmentId, authorTeamID = teamId });
                }
            }

            Stream submission = null;
            if (assignment.Type == AssignmentTypes.CriticalReview)
            {
                //Critical Review: We want all the reviews this team was set to do. 

                ZipFile zipfile = new ZipFile();

                //List of all the teams who the current team was set to review
                List<ReviewTeam> reviewTeams = (from rt in db.ReviewTeams
                                                where rt.AssignmentID == assignmentId
                                                && rt.ReviewTeamID == teamId
                                                select rt).ToList();

                //Add each review into the zip
                Dictionary<string, dynamic> reviewStreams = new Dictionary<string, dynamic>();
                foreach (ReviewTeam reviewTeam in reviewTeams)
                {
                    string key = reviewTeam.AuthorTeam.Name;
                    OSBLE.Models.FileSystem.FileCollection fc =
                        Models.FileSystem.Directories.GetAssignment(
                            ActiveCourseUser.AbstractCourseID, assignmentId)
                        .Review(reviewTeam.AuthorTeam, reviewTeam.ReviewingTeam)
                        .AllFiles();

                    //don't create a zip if we don't have have anything to zip.
                    if (fc.Count > 0)
                    {
                        var bytes = fc.ToBytes();
                        reviewStreams[key] = bytes;
                    }
                }

                foreach (string author in reviewStreams.Keys)
                {
                    foreach (string file in reviewStreams[author].Keys)
                    {
                        string location = string.Format("{0}/{1}", "Review of " + author, file);
                        zipfile.AddEntry(location, reviewStreams[author][file]);
                    }
                }

                submission = new MemoryStream();
                zipfile.Save(submission);
                submission.Position = 0;
            }
            else //Basic Assigment, only need to get submissions
            {
                submission = 
                    OSBLE.Models.FileSystem.Directories.GetAssignmentSubmission(
                        ActiveCourseUser.AbstractCourseID, assignmentId, teamId)
                    .AllFiles()
                    .ToZipStream();
            }

            string ZipName = assignment.AssignmentName + " by " + team.Name + ".zip";
            return new FileStreamResult(submission, "application/octet-stream") { FileDownloadName = ZipName };
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
        /// Gets the documents for a critical review. (The author team's submission from the preceding assignment)
        /// Note: Based off Critical Review Settings, may return an anonymously named folder.
        /// </summary>
        /// <param name="assignmentId">The Critical Review Assignment.ID</param>
        /// <param name="authorTeamId">The AuthorTeam's TeamID</param>
        /// <returns></returns>
        [CanSubmitAssignments]
        [OsbleAuthorize]
        [RequireActiveCourse]
        public ActionResult GetDocumentsForCriticalReview(int assignmentId, int authorTeamId)
        {
            Assignment CRAssignment = db.Assignments.Find(assignmentId);
            AssignmentTeam CurrentUsersTeam = GetAssignmentTeam(CRAssignment, ActiveCourseUser);
            Team authorTeam = db.Teams.Find(authorTeamId);

            //Getting a list of all the Team Ids for the current user to review. 
            List<int> AllTeamsToReview = (from rt in CRAssignment.ReviewTeams
                                          where rt.ReviewTeamID == CurrentUsersTeam.TeamID
                                          select rt.AuthorTeamID).ToList();

            //If authorTeamId is not in the list of author teams being reviewed by current user, then permission is denied.
            if (AllTeamsToReview.Contains(authorTeamId))
            {
                //Send off to Annotate if we have exactly one deliverable and that deliverable is a PDF document
                if (CRAssignment.PreceedingAssignment.Deliverables.Count == 1 && CRAssignment.PreceedingAssignment.Deliverables[0].DeliverableType == DeliverableType.PDF)
                {
                    return RedirectToRoute(new { controller = "PdfCriticalReview", action = "Review", assignmentID = assignmentId, authorTeamID = authorTeamId });
                }

                //Document not handled by Annotate, must collect author teams preceding assignment's submission
                OSBLE.Models.FileSystem.FileCollection AuthorTeamSubmission =
                    Models.FileSystem.Directories.GetAssignment(
                        ActiveCourseUser.AbstractCourseID, CRAssignment.PrecededingAssignmentID.Value)
                    .Submission(authorTeamId)
                    .AllFiles();

                //Checking if author should be anonymized. 
                string displayName = authorTeam.Name;
                if (AnonymizeAuthor(CRAssignment, authorTeam))
                {
                    displayName = "Anonymous " + authorTeamId;
                }
                string zipFileName = string.Format("{0}'s submission for {1}.zip", displayName, CRAssignment.PreceedingAssignment.AssignmentName);

                return new FileStreamResult(AuthorTeamSubmission.ToZipStream(), "application/octet-stream") { FileDownloadName = zipFileName };
            }
            return RedirectToAction("Index", "Home");
        }



        /// <summary>
        /// This gets all the documents for a critical review discussion. This is used by students and instructors
        ///     This could be all the marked up documents, or it could be a link to annotate, or it could be a merged .cpml file.
        /// </summary>
        /// <param name="discussionTeamId">DiscussionTeam that needs the documents</param>
        /// <returns></returns>
        [OsbleAuthorize]
        [RequireActiveCourse]
        public ActionResult GetDocumentsForCriticalReviewDiscussion(int discussionTeamId)
        {
            DiscussionTeam dt = db.DiscussionTeams.Find(discussionTeamId);
            Assignment CRAssignment = dt.Assignment.PreceedingAssignment;
            Assignment CRDAssignment = dt.Assignment;
            Assignment BasicAssignment = CRAssignment.PreceedingAssignment;
            Team AuthorTeam = dt.AuthorTeam;
            //Permission checking:
            // assert that the activeCourseUser is a member of the discussion team of discussionTeamID
            bool belongsToDiscussionTeam = false;
            foreach (TeamMember tm in dt.GetAllTeamMembers())
            {
                if (tm.CourseUserID == ActiveCourseUser.ID)
                {
                    belongsToDiscussionTeam = true;
                    break;
                }
            }

            //If user does not belong to DiscussionTeam and is not an instructor, do not let them get the documents
            if (belongsToDiscussionTeam || ActiveCourseUser.AbstractRole.CanModify)
            {
                string zipFileName = "Critical Review Discussion Items for " + dt.TeamName + ".zip";
                return GetAllReviewedDocuments(CRAssignment, AuthorTeam, zipFileName, CRDAssignment.DiscussionSettings);
            }
            return RedirectToAction("Index", "Home");
        }


        /// <summary>
        /// Returns a boolean indicating if an Author's name should be anonymized. 
        /// </summary>
        /// <param name="CRAssignment">Critical Review Assignment</param>
        /// <param name="authorTeam">The Team of authors</param>
        /// <param name="discussionSettings">Critical Review Discussion's discussion settings. (if applicable)</param>
        /// <returns></returns>
        private bool AnonymizeAuthor(Assignment CRAssignment, Team authorTeam, DiscussionSetting discussionSettings = null)
        {
            //MG:authors are to be anonymous if any of the criteria are met:
            //From DiscsussionSettings:
            //ActiveCurrentUser is a Moderator and Students are anonymous to Moderators. (Could occur when a moderator is downloading files from a CRD)
            //ActiveCurrentUser is a Student and Students are anonymous to Students. 
            //From CriticalReviewSettings:
            //AnonymizeAuthor && ActiveCourseUser is not on Author team

            bool anonymize = false;

            //Instructors and TAs should never have anonymous authors.
            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor || ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
            {
                return false;
            }

            //Discussion Settings check
            if (discussionSettings != null)
            {
                if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student && discussionSettings.HasAnonymousStudentsToStudents)
                {
                    anonymize = true;
                }
                else if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator && discussionSettings.HasAnonymousStudentsToModerators)
                {
                    anonymize = true;
                }
            }

            //Critical Review settings check
            if (CRAssignment.CriticalReviewSettings != null && CRAssignment.CriticalReviewSettings.AnonymizeAuthor)
            {
                //Check if ActiveCourseUser is part of AuthorTeam. If not, anonymize.
                bool onTeam = authorTeam.TeamMembers.Where(tm => tm.CourseUserID == ActiveCourseUser.ID).Count() > 0;
                if (onTeam == false)
                {
                    anonymize = true;
                }
            }

            return anonymize;
        }

        /// <summary>
        /// Returns a boolean indicating if an Reviewer's name should be anonymized. 
        /// </summary>
        /// <param name="CRAssignment">Critical Review Assignment</param>
        /// <param name="authorTeam">The Team of reviewers</param>
        /// <param name="discussionSettings">Critical Review Discussion's discussion settings. (if applicable)</param>
        /// <returns></returns>
        private bool AnonymizeReviewer(Assignment CRAssignment, Team reviewTeam, DiscussionSetting discussionSettings = null)
        {
            //MG: reviewers are to be anonymous if any of the criteria are met:
            //From DiscsussionSettings:
            //ActiveCurrentUser is a Moderator and Students are anonymous to Moderators. (Could occur when a moderator is downloading files from a CRD)
            //ActiveCurrentUser is a Student and Students are anonymous to Students. 
            //From CriticalReviewSettings:
            //AnonymizeCommentsCommentsAfterPublish && Critical Review Assignment is published && AcitveCourseUser is not on Review Team
            //Anonymizecomments && Critical REview Assignment is not published && AcitveCourseUser is not on Review Team

            bool anonymize = false;

            //Instructors and TAs should never have anonymous reviewers.
            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor || ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
            {
                return false;
            }

            //Discussion Settings check
            if (discussionSettings != null)
            {
                if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Student && discussionSettings.HasAnonymousStudentsToStudents)
                {
                    anonymize = true;
                }
                else if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator && discussionSettings.HasAnonymousStudentsToModerators)
                {
                    anonymize = true;
                }
            }

            //Critical Review settings check
            if (CRAssignment.CriticalReviewSettings != null)
            {
                bool onTeam = reviewTeam.TeamMembers.Where(tm => tm.CourseUserID == ActiveCourseUser.ID).Count() > 0;

                //comments are made by reviewers, so anonymize comments translates to anonymizing reviewers. 
                //Only considering anonymization if user is not on the review team.
                if (onTeam == false && CRAssignment.CriticalReviewSettings.AnonymizeCommentsAfterPublish && CRAssignment.IsCriticalReviewPublished == true)
                {
                    anonymize = true;
                }
                else if (onTeam == false && CRAssignment.CriticalReviewSettings.AnonymizeComments && CRAssignment.IsCriticalReviewPublished == false)
                {
                    anonymize = true;
                }
            }

            return anonymize;
        }

        /// <summary>
        /// Gets all the reviewed documents that belong to AuthorTeam. This function is used by other FileHandler function
        /// </summary>
        /// <param name="CRAssignment">The critical review assignment to fetch documents from</param>
        /// <param name="authorTeam">The Team that was reviewed</param>
        /// <param name="zipFileName">The Team that was reviewed</param>
        /// <param name="discussionSetting"></param>
        /// <returns></returns>
        private ActionResult GetAllReviewedDocuments(Assignment CRAssignment, Team authorTeam, string zipFileName, DiscussionSetting discussionSetting = null)
        {
            Assignment basicAssignment = CRAssignment.PreceedingAssignment;
            //If the BasicAssignment was a PDF, then use annotate to view discussion items
            if (basicAssignment.HasDeliverables && basicAssignment.Deliverables[0].DeliverableType == DeliverableType.PDF)
            {
                return RedirectToRoute(new { controller = "PdfCriticalReview", action = "Review", assignmentID = CRAssignment.ID, authorTeamID = authorTeam.ID });
            }

            //Gathering list of all TeamIDs who reviewed this Author
            List<int> reviewingTeamIds = (from rt in CRAssignment.ReviewTeams
                                          where rt.AuthorTeamID == authorTeam.ID
                                          select rt.ReviewTeamID).ToList();

            //Gathering all the assignment teams who reviewed DiscussionTeam's Author
            List<AssignmentTeam> reviewingTeams = (from at in CRAssignment.AssignmentTeams
                                                   where reviewingTeamIds.Contains(at.TeamID)
                                                   select at).ToList();

            //Streams used for ".cpml" Merging.
            MemoryStream parentStream = new MemoryStream();
            MemoryStream outputStream = new MemoryStream();
            bool FirstTime = true;  //Bool used to determine if original file was added to stream

            //ZipFile for all Reviews
            ZipFile zipFile = new ZipFile();

            //Determining displayname for the author team. 
            string authorDisplayName = authorTeam.Name;
            if (AnonymizeAuthor(CRAssignment, authorTeam, discussionSetting))
            {
                authorDisplayName = "Anonymous " + authorTeam.ID;
            }

            //Potentially multiple review teams are merged onto 1 team for the Critical Review Discussion
            //so each Review Teams documents must be within the Critical Review Discussion Documents.
            foreach (AssignmentTeam reviewTeam in reviewingTeams)
            {
                //Get Reviews for AutuhorTeam from ReviewTeam.
                string reviewTeamSubmissionPath = 
                    Models.FileSystem.Directories.GetAssignment(
                        ActiveCourseUser.AbstractCourseID, CRAssignment.ID)
                    .Review(authorTeam.ID, reviewTeam.TeamID)
                    .GetPath();


                //Directory might not exist, check to avoid runtime error.
                if (new DirectoryInfo(reviewTeamSubmissionPath).Exists)
                {

                    //Checking anonmous settings to determine name of folder
                    string reviewerDisplayName = reviewTeam.Team.Name;
                    if (AnonymizeReviewer(CRAssignment, reviewTeam.Team, discussionSetting))
                    {
                        //Change displayName if Reviewer is to be anonymized
                        reviewerDisplayName = "Anonymous " + reviewTeam.Team.ID;
                    }
                    string folderName = "Review from " + reviewerDisplayName;

                    zipFile.AddDirectory(reviewTeamSubmissionPath, folderName);

                    //Check each file to see it s a .cpml. If it is, handle merging them into one .cpml
                    foreach (string filename in Directory.EnumerateFiles(reviewTeamSubmissionPath))
                    {
                        if (Path.GetExtension(filename) == ".cpml")
                        {
                            //Only want to add the original file to the stream once.
                            if (FirstTime == true)
                            {
                                string originalFile =
                                    OSBLE.Models.FileSystem.Directories.GetAssignment(
                                        ActiveCourseUser.AbstractCourseID, basicAssignment.ID)
                                    .Submission(authorTeam)
                                    .GetPath();
                                FileStream filestream = System.IO.File.OpenRead(originalFile + "\\" + basicAssignment.Deliverables[0].Name + ".cpml");
                                filestream.CopyTo(parentStream);
                                FirstTime = false;
                            }

                            //Merge the FileStream from filename + parentStream into outputStream.
                            ChemProV.Core.CommentMerger.Merge(parentStream, authorDisplayName, System.IO.File.OpenRead(filename), reviewerDisplayName, outputStream);

                            //close old parent stream before writing over it
                            parentStream.Close();

                            //Copy outputStream to parentStream, creating new outputStream
                            parentStream = new MemoryStream();
                            outputStream.Seek(0, SeekOrigin.Begin);
                            outputStream.CopyTo(parentStream);
                            outputStream = new MemoryStream();
                        }
                    }
                }

            }

            //Adding merged document to Zip if there was every a .cpml
            if (FirstTime == false)
            {
                parentStream.Seek(0, SeekOrigin.Begin);
                zipFile.AddEntry("MergedReview.cpml", parentStream);
            }

            //Saving zip to a stream
            MemoryStream returnValue = new MemoryStream();
            zipFile.Save(returnValue);
            returnValue.Position = 0;

            //Returning zip
            return new FileStreamResult(returnValue, "application/octet-stream") { FileDownloadName = zipFileName };
        }


        /// <summary>
        /// get a zip containing reviews that the current user has performed on the 
        /// author (authorTeamId) for a speicific assignment.
        /// </summary>
        /// <param name="assignmentId">Assignment to fetch reviews from</param>
        /// <param name="authorTeamId">Authorteam that was reviewed</param>
        /// <returns></returns>
        [CanSubmitAssignments]
        [OsbleAuthorize]
        [RequireActiveCourse]
        public ActionResult GetReviewForAuthor(int assignmentId, int authorTeamId)
        {
            Assignment CRAssignment = db.Assignments.Find(assignmentId);
            Team authorTeam = db.Teams.Find(authorTeamId);
            Team currentUsersTeam = GetAssignmentTeam(CRAssignment, ActiveCourseUser).Team;

            Stream returnValue = 
                OSBLE.Models.FileSystem.Directories.GetAssignment(
                    ActiveCourseUser.AbstractCourseID, CRAssignment.ID)
                .Review(authorTeam, currentUsersTeam)
                .AllFiles()
                .ToZipStream();

            //Checking if author should be anonymized. 
            string displayName = authorTeam.Name;
            if (AnonymizeAuthor(CRAssignment, authorTeam))
            {
                displayName = "Anonymous " + authorTeamId;
            }
            string zipFileName = string.Format("Review of {0}.zip", displayName);

            return new FileStreamResult(returnValue, "application/octet-stream") { FileDownloadName = zipFileName };
        }

        /// <summary>
        /// get a zip containing reviews that have been done 
        /// to the the author's (receiverId) preceding assignment.
        /// </summary>
        /// <param name="assignmentId">assignment ID of the critical review</param>
        /// <param name="receiverId">This is the CourseUser you want to download received reviews for. 
        /// If it is team based, any course user in the preceding assignment team will yield the same results</param>
        /// <returns></returns>
        [OsbleAuthorize]
        [RequireActiveCourse]
        public ActionResult GetReviewsOfAuthor(int assignmentId, int receiverId)
        {
            Assignment CRAssignment = db.Assignments.Find(assignmentId);
            CourseUser receiver = db.CourseUsers.Find(receiverId);
            AssignmentTeam AuthorTeam = GetAssignmentTeam(CRAssignment.PreceedingAssignment, receiver);

            if (ActiveCourseUser.AbstractRole.CanModify || (receiverId == ActiveCourseUser.ID))
            {
                //No need to anonymize name AuthorTeam here as this function call is only made by Instructors and the author team.
                return GetAllReviewedDocuments(CRAssignment, AuthorTeam.Team, "Critical Reviews for " + AuthorTeam.Team.Name + ".zip");
            }
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Returns all the submissions from the ActiveCourseUser for the given assignmentID
        /// </summary>
        /// <param name="assignmentID"></param>
        /// <returns></returns>
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
