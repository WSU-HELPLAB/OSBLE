using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Models;
using OSBLE.Models.Assignments;
using System.IO;
using Ionic.Zip;
using OSBLE.Models.FileSystem;
using OSBLE.Controllers;

namespace OSBLE.Services
{
    [ServiceContract(Namespace = "")]
    [SilverlightFaultBehavior]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class OsbleService
    {
        private AuthenticationService _authService = new AuthenticationService();
        private OSBLEContext _db = new OSBLEContext();

        /// <summary>
        /// Returns a list of Courses associated with the provided auth token
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public Course[] GetCourses(string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new Course[0];
            }
            UserProfile profile = _authService.GetActiveUser(authToken);
            List<Course> efCourses = (from cu in _db.CourseUsers
                                      where cu.UserProfileID == profile.ID
                                      &&
                                      cu.AbstractCourse is Course
                                      select cu.AbstractCourse as Course).ToList();

            //convert entity framework-based course to normal course for easier wire
            //transfer
            List<Course> nonEfCourses = new List<Course>(efCourses.Count);
            foreach (Course course in efCourses)
            {
                //use copy constructor to remove crud
                nonEfCourses.Add(new Course(course));
            }
            return nonEfCourses.ToArray();
        }

        /// <summary>
        /// Returns a list of courses that the supplied user can grade
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public Course[] GetCoursesTaught(string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new Course[0];
            }
            UserProfile profile = _authService.GetActiveUser(authToken);
            List<Course> efCourses = (
                                      from cu in _db.CourseUsers
                                      where cu.UserProfileID == profile.ID
                                      &&
                                      cu.AbstractCourse is Course
                                      &&
                                      cu.AbstractRole.CanGrade == true
                                      select cu.AbstractCourse as Course
                                      ).ToList();

            //convert entity framework-based course to normal course for easier wire
            //transfer
            List<Course> nonEfCourses = new List<Course>(efCourses.Count);
            foreach (Course course in efCourses)
            {
                //use copy constructor to remove crud
                nonEfCourses.Add(new Course(course));
            }
            return nonEfCourses.ToArray();
        }

        [OperationContract]
        public int UploadCourseGradebook(int courseID, byte[] zipData, string authToken)
        {
            int serviceResult = -1;

            //validate key
            if (!_authService.IsValidKey(authToken))
            {
                return serviceResult;
            }

            //make sure the supplied user can modify the desired course
            UserProfile profile = _authService.GetActiveUser(authToken);
            CourseUser courseUser = (
                                      from cu in _db.CourseUsers
                                      where cu.UserProfileID == profile.ID
                                      &&
                                      cu.AbstractCourse is Course
                                      &&
                                      cu.AbstractCourseID == courseID
                                      select cu
                                      ).FirstOrDefault();
            if (courseUser == null || courseUser.AbstractRole.CanGrade == false)
            {
                //user can't grade that course
                return serviceResult;
            }

            //upload the gradebook zip and return the result
            GradebookFilePath gfp = Models.FileSystem.Directories.GetGradebook(courseUser.AbstractCourseID);
            GradebookController gc = new GradebookController();
            try
            {
                serviceResult = gc.UploadGradebookZip(zipData, gfp);
            }
            catch (Exception)
            {
            }
            return serviceResult;
        }

        /// <summary>
        /// Returns all assignments associated with the given course
        /// </summary>
        /// <param name="courseId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public Assignment[] GetCourseAssignments(int courseId, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new Assignment[0];
            }
            UserProfile profile = _authService.GetActiveUser(authToken);

            //verify that the provided user is in this course
            CourseUser courseUser = _db.CourseUsers
                                       .Where(cu => cu.UserProfileID == profile.ID)
                                       .Where(cu => cu.AbstractCourseID == courseId)
                                       .FirstOrDefault();
            if (courseUser == null)
            {
                return new Assignment[0];
            }

            //get all non-draft assignments that have started
            DateTime today = DateTime.UtcNow;
            var query = from assignment in _db.Assignments
                        where assignment.CourseID == courseId
                        && assignment.IsDraft == false
                        && assignment.ReleaseDate <= today
                        orderby assignment.DueDate ascending
                        select assignment;
            List<Assignment> efAssignments = query.ToList();
            List<Assignment> nonEfAssignments = new List<Assignment>(efAssignments.Count);
            foreach (Assignment assignment in efAssignments)
            {
                nonEfAssignments.Add(new Assignment(assignment));
            }
            return nonEfAssignments.ToArray();
        }

        /// <summary>
        /// For internal OSBLE use.  Do not expose directly as a web service.  Instead, use
        /// GetMergedReviewDocument(int criticalReviewAssignmentId, string authToken) for
        /// that purpose.
        /// </summary>
        /// <param name="criticalReviewAssignmentId"></param>
        /// <returns></returns>
        public byte[] GetMergedReviewDocument(int criticalReviewAssignmentId, int userProfileId)
        {
            UserProfile profile = _db.UserProfiles.Find(userProfileId);
            Assignment criticalReviewAssignment = _db.Assignments.Find(criticalReviewAssignmentId);
            if (criticalReviewAssignment == null)
            {
                return new byte[0];
            }

            //were we handed a discussion assignment ID by accident?
            if (criticalReviewAssignment.Type == AssignmentTypes.CriticalReviewDiscussion)
            {
                if (criticalReviewAssignment.PreceedingAssignment.Type == AssignmentTypes.CriticalReview)
                {
                    criticalReviewAssignment = criticalReviewAssignment.PreceedingAssignment;
                    criticalReviewAssignmentId = criticalReviewAssignment.ID;
                }
                else
                {
                    return new byte[0];
                }
            }

            //AC: The rules listed below are too convoluted for the average instructor to
            //remember.  Commenting out for now.
            /*
            if (criticalReviewAssignment.Type != AssignmentTypes.CriticalReviewDiscussion)
            {
            //only continue if:
            // a: the assignment's due date has passed
            // b: the assignment is set up to release critical reviews to students after
            //    the due date.
            // c: the instructor has clicked the "Publish All Reviews" link on the 
            //    assignment details page. (turned off for now)
            if (criticalReviewAssignment.DueDate > DateTime.UtcNow
                || 
                (criticalReviewAssignment.CriticalReviewSettings != null && criticalReviewAssignment.CriticalReviewSettings.AllowDownloadAfterPublish == false)
                )
            {
                return new byte[0];
            }
            }
             * */

            //make sure that the user is enrolled in the course
            CourseUser courseUser = (from cu in _db.CourseUsers
                                     where cu.AbstractCourseID == criticalReviewAssignment.CourseID
                                     &&
                                     cu.UserProfileID == profile.ID
                                     select cu).FirstOrDefault();
            if (courseUser == null)
            {
                return new byte[0];
            }

            //Two possible scenarios:
            //1: we're dealing with a non-discussion based critical review.  In this case, pull all
            //   teams from the "review team" table
            //2: we're dealing with a discussion-based critical review.  In this case, pull from the 
            //   "discussion team" table.  Note that discussion teams are a superset of review teams 
            //   so that everything in a review team will also be inside a discussion team.  Pulling 
            //   from discussion teams allows us to capture moderators and other observer-type
            //   people that have been attached to the review process.
            
            //step 1: check to see if the critical review assignment has any discussion assignments attached
            Assignment discussionAssignment = (from a in _db.Assignments
                                              where a.PrecededingAssignmentID == criticalReviewAssignmentId
                                              && a.AssignmentTypeID == (int)AssignmentTypes.CriticalReviewDiscussion
                                              select a
                                              ).FirstOrDefault();

            IList<IReviewTeam> authorTeams = new List<IReviewTeam>();
            if (discussionAssignment != null)
            {
                //pull the more inclusive discussion teams
                List<DiscussionTeam> discussionTeams = (from dt in _db.DiscussionTeams
                                                        join team in _db.Teams on dt.TeamID equals team.ID
                                                        join member in _db.TeamMembers on team.ID equals member.TeamID
                                                        where member.CourseUserID == courseUser.ID
                                                        && dt.AssignmentID == discussionAssignment.ID
                                                        select dt).ToList();
                authorTeams = TeamConverter.DiscussionToReviewTeam(discussionTeams);
            }
            else
            {
                //pull critical review teams
                authorTeams = (from rt in _db.ReviewTeams
                               join team in _db.Teams on rt.ReviewTeamID equals team.ID
                               join member in _db.TeamMembers on team.ID equals member.TeamID
                               where member.CourseUserID == courseUser.ID
                               && rt.AssignmentID == criticalReviewAssignmentId
                               select rt).ToList().Cast<IReviewTeam>().ToList();
            }


            //no author team means that the current user isn't assigned to review anyone
            if (authorTeams.Count == 0)
            {
                return new byte[0];
            }

            using (ZipFile finalZip = new ZipFile())
            {
                foreach (ReviewTeam authorTeam in authorTeams)
                {

                    //get all reviewers (not just the current user's review team)
                    List<ReviewTeam> reviewers = (from rt in _db.ReviewTeams
                                                  where rt.AuthorTeamID == authorTeam.AuthorTeamID
                                                  select rt).ToList();

                    //get original document
                    MemoryStream finalStream = new MemoryStream();
                    string originalFile =
                        OSBLE.Models.FileSystem.Directories.GetAssignmentSubmission(
                            (int)criticalReviewAssignment.CourseID,
                            (int)criticalReviewAssignment.PrecededingAssignmentID,
                            authorTeam.AuthorTeamID).AllFiles().FirstOrDefault();
                    if (originalFile == null)
                    {
                        //author didn't submit a document to be reviewed.  Skip the rest.
                        continue;
                    }
                    FileStream originalFileStream = File.OpenRead(originalFile);
                    originalFileStream.CopyTo(finalStream);
                    originalFileStream.Close();
                    finalStream.Position = 0;

                    //loop through each review team, merging documents
                    foreach (ReviewTeam reviewer in reviewers)
                    {
                        string teamName = reviewer.ReviewingTeam.Name;
                        if (criticalReviewAssignment.CriticalReviewSettings.AnonymizeCommentsAfterPublish == true)
                        {
                            teamName = string.Format("Anonymous {0}", reviewer.ReviewTeamID);
                        }
                        FileCollection allFiles =
                            OSBLE.Models.FileSystem.Directories.GetReview(
                                (int)criticalReviewAssignment.CourseID,
                                criticalReviewAssignmentId,
                                authorTeam.AuthorTeamID, reviewer.ReviewTeamID)
                                                    .AllFiles();
                        foreach (string file in allFiles)
                        {
                            MemoryStream mergedStream = new MemoryStream();
                            FileStream studentReview = System.IO.File.OpenRead(file);

                            //merge
                            ChemProV.Core.CommentMerger.Merge(finalStream, "", studentReview, teamName, mergedStream);

                            //rewind merged stream and copy over to final stream
                            mergedStream.Position = 0;
                            finalStream = new MemoryStream();
                            mergedStream.CopyTo(finalStream);
                            finalStream.Position = 0;
                            mergedStream.Close();
                            studentReview.Close();
                        }
                    }

                    //finally, zip up and add to our list
                    string documentName = Path.GetFileName(originalFile);
                    string authorTeamName = authorTeam.AuthorTeam.Name;
                    if (criticalReviewAssignment.CriticalReviewSettings.AnonymizeAuthor == true)
                    {
                        authorTeamName = string.Format("Anonymous {0}", authorTeam.ReviewTeamID);
                    }
                    string filePath = string.Format("{0};{1}/{2}",
                                                    authorTeam.AuthorTeamID,
                                                    authorTeamName,
                                                    documentName
                                                    );
                    finalZip.AddEntry(filePath, finalStream);
                }
                MemoryStream zipStream = new MemoryStream();
                finalZip.Save(zipStream);
                zipStream.Position = 0;
                byte[] zipBytes = zipStream.ToArray();
                zipStream.Close();
                return zipBytes;
            }
        }

        /// <summary>
        /// For getting merged ChemProV documents
        /// </summary>
        /// <param name="criticalReviewAssignmentId"></param>
        /// <param name="authorId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public byte[] GetMergedReviewDocument(int criticalReviewAssignmentId, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new byte[0];
            }
            UserProfile profile = _authService.GetActiveUser(authToken);
            return GetMergedReviewDocument(criticalReviewAssignmentId, profile.ID);
        }

        /// <summary>
        /// Will return all items needing to be reviewed by the user for the given
        /// critical review assignment.
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public byte[] GetReviewItems(int assignmentId, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new byte[0];
            }
            UserProfile profile = _authService.GetActiveUser(authToken);
            Assignment criticalReviewAssignment = _db.Assignments.Find(assignmentId);

            //because the user is doing a critical review, the actual review items
            //are stored on the current assignment's "preceeding assignment".  Therefore,
            //we must use this preceeding assignment to find all of the review items
            Assignment submissionAssignment = criticalReviewAssignment.PreceedingAssignment;

            //no submission assignment = something isn't right
            if (submissionAssignment == null)
            {
                return new byte[0];
            }

            //make sure that the user is enrolled in the course
            CourseUser courseUser = (from cu in _db.CourseUsers
                                     where cu.AbstractCourseID == criticalReviewAssignment.CourseID
                                     &&
                                     cu.UserProfileID == profile.ID
                                     select cu).FirstOrDefault();
            if (courseUser == null)
            {
                return new byte[0];
            }

            //users are attached to assignments through teams, so we have to find the correct team
            //that is doing the critical review
            List<ReviewTeam> teamsToReview = (from rt in _db.ReviewTeams
                                              join team in _db.Teams on rt.ReviewTeamID equals team.ID
                                              join member in _db.TeamMembers on team.ID equals member.TeamID
                                              where member.CourseUserID == courseUser.ID
                                              && rt.AssignmentID == assignmentId
                                              select rt).ToList();

            if (teamsToReview == null)
            {
                return new byte[0];
            }

            //Find all review documents
            Dictionary<string, dynamic> originalStreams = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> reviewStreams = new Dictionary<string, dynamic>();
            foreach (ReviewTeam teamToReview in teamsToReview)
            {
                string zipName = teamToReview.AuthorTeam.Name;

                //check anonymity settings
                if (criticalReviewAssignment.CriticalReviewSettings.AnonymizeAuthor)
                {
                    zipName = string.Format("Anonymous {0}", teamToReview.AuthorTeamID);
                }
                string key = string.Format("{0};{1}", teamToReview.AuthorTeamID, zipName);

                //get the original, unedited document
                FileCollection fc =
                    OSBLE.Models.FileSystem.Directories.GetAssignment(
                        courseUser.AbstractCourseID, submissionAssignment.ID)
                                    .Submission(teamToReview.AuthorTeamID)
                                    .AllFiles();
                
                //don't create a zip if we have have nothing to zip.
                if (fc.Count > 0)
                {
                    var bytes = fc.ToBytes();
                    originalStreams[key] = bytes;
                }

                //get any user modified document
                fc = OSBLE.Models.FileSystem.Directories.GetAssignment(
                        courseUser.AbstractCourseID, criticalReviewAssignment.ID)
                       .Review(teamToReview.AuthorTeamID, teamToReview.ReviewTeamID)
                       .AllFiles();

                //don't create a zip if we have have nothing to zip.
                if (fc.Count > 0)
                {
                    var bytes = fc.ToBytes();
                    reviewStreams[key] = bytes;
                }
            }
            try
            {
                //put everything into a zip file
                ZipFile final = new ZipFile();

                foreach (string author in originalStreams.Keys)
                {
                    foreach (string file in originalStreams[author].Keys)
                    {
                        string location = string.Format("{0}/{1}/{2}", "originals", author, file);
                        final.AddEntry(location, originalStreams[author][file]);
                    }
                }

                foreach (string author in reviewStreams.Keys)
                {
                    foreach (string file in reviewStreams[author].Keys)
                    {
                        string location = string.Format("{0}/{1}/{2}", "reviews", author, file);
                        final.AddEntry(location, reviewStreams[author][file]);
                    }
                }

                MemoryStream finalZipStream = new MemoryStream();
                final.Save(finalZipStream);
                finalZipStream.Position = 0;
                byte[] bytes = finalZipStream.ToArray();
                finalZipStream.Close();
                return bytes;
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }

        /// <summary>
        /// Submits a review of the provided author.
        /// </summary>
        /// <param name="authorId"></param>
        /// <param name="assignmentId"></param>
        /// <param name="zippedReviewData"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public bool SubmitReview(int authorId, int assignmentId, byte[] zippedReviewData, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return false;
            }

            try
            {
                UserProfile profile = _authService.GetActiveUser(authToken);
                Assignment assignment = _db.Assignments.Find(assignmentId);
                CourseUser courseUser = (from cu in _db.CourseUsers
                                         where cu.AbstractCourseID == assignment.CourseID
                                         &&
                                         cu.UserProfileID == profile.ID
                                         select cu).FirstOrDefault();
                Team reviewTeam = (from tm in _db.TeamMembers
                                   join rt in _db.ReviewTeams on tm.TeamID equals rt.ReviewTeamID
                                   where tm.CourseUserID == courseUser.ID
                                   && rt.AssignmentID == assignmentId
                                   select tm.Team).FirstOrDefault();
                MemoryStream ms = new MemoryStream(zippedReviewData);
                ms.Position = 0;
                using (ZipFile file = ZipFile.Read(ms))
                {
                    foreach (ZipEntry entry in file.Entries)
                    {
                        using (MemoryStream extractStream = new MemoryStream())
                        {
                            entry.Extract(extractStream);
                            extractStream.Position = 0;

                            // Get the storage object for the review
                            OSBLEDirectory fs = Models.FileSystem.Directories.GetReview(
                                (int)assignment.CourseID, assignmentId, authorId, reviewTeam.ID);

                            // Delete existing first
                            fs.DeleteContents(true, true);

                            //add the extracted file to the file system
                            bool result = fs.AddFile(entry.FileName, extractStream);
                            if (result == false)
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns any documents submitted by the current user for the supplied assignment.
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public byte[] GetAssignmentSubmission(int assignmentId, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new byte[0];
            }
            UserProfile profile = _authService.GetActiveUser(authToken);
            Assignment assignment = _db.Assignments.Find(assignmentId);

            //make sure that the user is enrolled in the course
            CourseUser courseUser = (from cu in _db.CourseUsers
                                     where cu.AbstractCourseID == assignment.CourseID
                                     &&
                                     cu.UserProfileID == profile.ID
                                     select cu).FirstOrDefault();
            if (courseUser == null)
            {
                return new byte[0];
            }

            //users are attached to assignments through teams, so we have to find the correct team
            Team team = (from tm in _db.TeamMembers
                         join at in _db.AssignmentTeams on tm.TeamID equals at.TeamID
                         where tm.CourseUserID == courseUser.ID
                         && at.AssignmentID == assignmentId
                         select tm.Team).FirstOrDefault();

            if (team == null)
            {
                return new byte[0];
            }


            OSBLE.Models.FileSystem.AssignmentFilePath fs =
                Models.FileSystem.Directories.GetAssignment(
                    courseUser.AbstractCourseID, assignmentId);
            Stream stream = fs.Submission(team.ID)
                              .AllFiles()
                              .ToZipStream();
            MemoryStream ms = new MemoryStream();
            try
            {
                stream.CopyTo(ms);
            }
            catch (Exception)
            {
            }
            byte[] bytes = ms.ToArray();
            stream.Close();
            ms.Close();
            return bytes;
        }

        /// <summary>
        /// Returns role information for the given course and authToken
        /// </summary>
        /// <param name="authToken"></param>
        /// <param name="courseUserId"></param>
        /// <returns></returns>
        [OperationContract]
        public CourseRole GetCourseRole(int courseId, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return new CourseRole();
            }
            UserProfile profile = _authService.GetActiveUser(authToken);
            CourseUser courseUser = _db.CourseUsers
                                    .Where(cu => cu.AbstractCourseID == courseId)
                                    .Where(cu => cu.UserProfileID == profile.ID)
                                    .FirstOrDefault();

            //trying to access course data for the wrong person
            if (courseUser == null)
            {
                return new CourseRole();
            }

            return new CourseRole(courseUser.AbstractRole);
        }

        /// <summary>
        /// Allows users to submit homework assignments.
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="zipData"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        [OperationContract]
        public bool SubmitAssignment(int assignmentId, byte[] zipData, string authToken)
        {
            if (!_authService.IsValidKey(authToken))
            {
                return false;
            }
            try
            {
                Assignment assignment = _db.Assignments.Find(assignmentId);
                UserProfile user = _authService.GetActiveUser(authToken);
                CourseUser courseUser = _db.CourseUsers
                                    .Where(cu => cu.AbstractCourseID == assignment.CourseID)
                                    .Where(cu => cu.UserProfileID == user.ID)
                                    .FirstOrDefault();
                Team team = (from tm in _db.TeamMembers
                             join at in _db.AssignmentTeams on tm.TeamID equals at.TeamID
                             where tm.CourseUserID == courseUser.ID
                             && at.AssignmentID == assignmentId
                             select tm.Team).FirstOrDefault();

                OSBLE.Models.FileSystem.AssignmentFilePath fs =
                    Models.FileSystem.Directories.GetAssignment(
                        (int)assignment.CourseID, assignmentId);

                MemoryStream ms = new MemoryStream(zipData);
                ms.Position = 0;
                using (ZipFile file = ZipFile.Read(ms))
                {
                    foreach (ZipEntry entry in file.Entries)
                    {
                        MemoryStream extractStream = new MemoryStream();
                        entry.Extract(extractStream);
                        extractStream.Position = 0;

                        //delete existing
                        fs.Submission(team.ID)
                            .File(entry.FileName)
                            .Delete();

                        //add the extracted file to the file system
                        bool result = fs.Submission(team.ID)
                                        .AddFile(entry.FileName, extractStream);
                        if (result == false)
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
    }
}
