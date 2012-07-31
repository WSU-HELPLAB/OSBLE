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

            //get all non-draft assignments
            var query = from assignment in _db.Assignments
                        where assignment.CourseID == courseId
                        && assignment.IsDraft == false
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
                                              select rt).ToList();

            if (teamsToReview == null)
            {
                return new byte[0];
            }

            //Find all review documents
            OSBLE.Models.FileSystem.FileSystem fs = new Models.FileSystem.FileSystem();
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
                FileCollection fc = fs.Course(courseUser.AbstractCourseID)
                                    .Assignment(submissionAssignment.ID)
                                    .Submission(teamToReview.AuthorTeamID)
                                    .AllFiles();
                
                //don't create a zip if we have have nothing to zip.
                if (fc.Count > 0)
                {
                    var bytes = fc.ToBytes();
                    originalStreams[key] = bytes;
                }

                //get any user modified document
                fc = fs.Course(courseUser.AbstractCourseID)
                       .Assignment(criticalReviewAssignment.ID)
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
                OSBLE.Models.FileSystem.FileSystem fs = new Models.FileSystem.FileSystem();
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

                            //delete existing
                            fs.Course((int)assignment.CourseID)
                                            .Assignment(assignmentId)
                                            .Review(authorId, reviewTeam.ID)
                                            .File(entry.FileName)
                                            .Delete();

                            //add the extracted file to the file system
                            bool result = fs.Course((int)assignment.CourseID)
                                            .Assignment(assignmentId)
                                            .Review(authorId, reviewTeam.ID)
                                            .AddFile(entry.FileName, extractStream);
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


            OSBLE.Models.FileSystem.FileSystem fs = new Models.FileSystem.FileSystem();
            Stream stream = fs.Course(courseUser.AbstractCourseID)
                              .Assignment(assignmentId)
                              .Submission(team.ID)
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
                OSBLE.Models.FileSystem.FileSystem fs = new Models.FileSystem.FileSystem();

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
                        fs.Course((int)assignment.CourseID)
                            .Assignment(assignmentId)
                            .Submission(team.ID)
                            .File(entry.FileName)
                            .Delete();

                        //add the extracted file to the file system
                        bool result = fs.Course((int)assignment.CourseID)
                                        .Assignment(assignmentId)
                                        .Submission(team.ID)
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
