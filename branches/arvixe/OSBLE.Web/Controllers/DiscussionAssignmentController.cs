using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Models.FileSystem;
using Ionic.Zip;
using OSBLE.Utility;
using System.Net.Mail;
using System.Text;

namespace OSBLE.Controllers
{
    public class DiscussionAssignmentController : OSBLEController
    {
        //This enum will be used to organize the parameter for TeacherIndex related to which posts should be highlighted.
        //The following is from TeacherIndexFunction, for refernce. Remove later
        //<param name="postOrReply">postOrReply is used as enumerable. 0 = Posts, 1 = Replies, 2 = Both, 3 = No Selector</param>
        public enum HighlightValue
        {
            Posts = 0,
            Replies,
            PostsAndReplies,
            None,
            NewPosts
        }

        /// <summary>
        /// This function will return the DateTime of the last visit to the discussion assignment. If the user has not visited this discussion assignment, the value will be DateTime.Min.
        /// In addition to returning the last visit time, this function will update the last visit time to the current time. Note: The returned dateTime will not have the new assigned value.
        /// </summary>
        /// <param name="discussionTeamId"></param>
        /// <returns></returns>
        private DateTime? GetAndUpdateLastVisit(int discussionTeamId)
        {
            DateTime? returnVal = DateTime.MinValue;
            DiscussionAssignmentMetaInfo lastVisited = (from metaInfo in db.DiscussionAssignmentMetaTable
                                                        where metaInfo.DiscussionTeamID == discussionTeamId &&
                                                        metaInfo.CourseUserID == ActiveCourseUser.ID
                                                        select metaInfo).FirstOrDefault();
            if (lastVisited == null) //create new table entry if it does not exist
            {
                lastVisited = new DiscussionAssignmentMetaInfo();
                lastVisited.DiscussionTeamID = discussionTeamId;
                lastVisited.CourseUserID = ActiveCourseUser.ID;
                db.DiscussionAssignmentMetaTable.Add(lastVisited);
            }
            else //set return value since one exists
            {
                returnVal = lastVisited.LastVisit;
                
            }
            lastVisited.LastVisit = DateTime.UtcNow; //update LastVisit time & save changes
            db.SaveChanges();

            return returnVal;

        }

        [CanGradeCourse]
        public ActionResult GetAllDiscussionItems(int assignmentId)
        {
            Assignment currentAssignment = db.Assignments.Find(assignmentId);
            if (currentAssignment == null)
            {
                //bad assignment: redirect to home page
                return RedirectToAction("Index", "Home", new { area = "AssignmentDetails", assignmentId = assignmentId });
            }

            //we are assuming that we're working with a CRD assignment
            if (currentAssignment.Type != AssignmentTypes.CriticalReviewDiscussion)
            {
                return RedirectToAction("Index", "Home", new { area = "AssignmentDetails", assignmentId = assignmentId });
            }

            Assignment criticalReviewAssignment = currentAssignment.PreceedingAssignment;
            Assignment basicAssignment = criticalReviewAssignment.PreceedingAssignment;

            ZipFile zipFile = new ZipFile();

            Dictionary<int, MemoryStream> parentStreams = new Dictionary<int, MemoryStream>();
            Dictionary<int, string> parentStreamNames = new Dictionary<int,string>();

            //loop through all review teams
            foreach (ReviewTeam reviewTeam in criticalReviewAssignment.ReviewTeams)
            {
                string zipPath = string.Format("{0}/{1}", reviewTeam.AuthorTeam.Name, reviewTeam.ReviewingTeam.Name);
                string reviewerDisplayName = reviewTeam.ReviewingTeam.Name;
                string authorDisplayName = reviewTeam.AuthorTeam.Name;

                //get all files
                FileCollection files =
                    Models.FileSystem.Directories.GetAssignment(
                        (int)currentAssignment.CourseID, criticalReviewAssignment.ID)
                    .Review(reviewTeam.AuthorTeam, reviewTeam.ReviewingTeam)
                    .AllFiles();
                foreach (string file in files)
                {
                    //cpml files need to be merged and not added individually
                    if (Path.GetExtension(file) == ".cpml")
                    {
                        //temporary stream used for ".cpml" Merging.
                        MemoryStream outputStream = new MemoryStream();

                        //Only want to add the original file to the stream once.
                        if(parentStreams.ContainsKey(reviewTeam.AuthorTeamID) == false)
                        {
                            string originalFile =
                                Models.FileSystem.Directories.GetAssignment(
                                    ActiveCourseUser.AbstractCourseID, basicAssignment.ID)
                                .Submission(reviewTeam.AuthorTeam)
                                .GetPath();
                            FileStream filestream = System.IO.File.OpenRead(originalFile + "\\" + basicAssignment.Deliverables[0].Name + ".cpml");
                            parentStreams.Add(reviewTeam.AuthorTeamID, new MemoryStream());
                            parentStreamNames.Add(reviewTeam.AuthorTeamID, reviewTeam.AuthorTeam.Name);
                            filestream.CopyTo(parentStreams[reviewTeam.AuthorTeamID]);
                        }

                        //Merge the FileStream from filename + parentStream into outputStream.
                        ChemProV.Core.CommentMerger.Merge(parentStreams[reviewTeam.AuthorTeamID], authorDisplayName, System.IO.File.OpenRead(file), reviewerDisplayName, outputStream);

                        //close old parent stream before writing over it
                        parentStreams[reviewTeam.AuthorTeamID].Close();

                        //Copy outputStream to parentStream, creating new outputStream
                        parentStreams[reviewTeam.AuthorTeamID] = new MemoryStream();
                        outputStream.Seek(0, SeekOrigin.Begin);
                        outputStream.CopyTo(parentStreams[reviewTeam.AuthorTeamID]);
                        outputStream = new MemoryStream();
                    }
                    else
                    {
                        zipFile.AddFile(file, zipPath);
                    }
                }
            }

            //if we had a cpml document
            if (parentStreams.Count > 0)
            {
                foreach (int key in parentStreams.Keys)
                {
                    string mergedPath = string.Format("{0}_merged.cpml", parentStreamNames[key]);
                    parentStreams[key].Seek(0, SeekOrigin.Begin);
                    zipFile.AddEntry(mergedPath, parentStreams[key]);
                }
            }

            MemoryStream returnValue = new MemoryStream();
            zipFile.Save(returnValue);
            returnValue.Position = 0;

            //Returning zip
            string zipName = string.Format("{0}.zip", currentAssignment.AssignmentName);
            return new FileStreamResult(returnValue, "application/octet-stream") { FileDownloadName = zipName };
        }

        /// <summary>
        /// This is the discussion view used by non-Instructor/non-TA users. It displays a discussion assignment for discussionTeamId. 
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="discussionTeamId"></param>
        /// <param name="displayNewPosts">If true, any new posts made since the current users last visit will be highlighted.</param>
        /// <returns></returns>
        public ActionResult Index(int assignmentId, int discussionTeamId, bool? displayNewPosts = false)
        {
            
            Assignment assignment = null;
            DiscussionTeam discussionTeam = null;

            //checking if ids are good
            if (discussionTeamId > 0 && assignmentId > 0)
            {

                assignment = db.Assignments.Find(assignmentId);
                discussionTeam = (from dt in assignment.DiscussionTeams
                                  where dt.ID == discussionTeamId
                                  select dt).FirstOrDefault();
            }

            //Make sure ActiveCourseUser is a valid discussion member
                //Valid discussion members are in the discussion team, or in the class of a classwide discussion assignment
            bool allowedInDiscussion = false;
            if (assignment != null && assignment.HasDiscussionTeams == false)//Classwide discussion
            {
                //make sure user is in course
                if (ActiveCourseUser.AbstractCourseID == assignment.CourseID)
                {
                    allowedInDiscussion = true;
                }
            }
            else if (assignment != null && discussionTeam != null)//Assignment has discussion teams
            {
                //make sure user is part of team.
                foreach (TeamMember tm in discussionTeam.GetAllTeamMembers())
                {
                    if (tm.CourseUserID == ActiveCourseUser.ID)
                    {
                        allowedInDiscussion = true;
                        break;
                    }
                }
            }


            //if ActiveCourseUser belongs to the discussionTeam, continue. Otherwise kick them out.
            if (allowedInDiscussion)
            {
                DiscussionViewModel dvm = new DiscussionViewModel(discussionTeam, ActiveCourseUser);


                //Checking if its users first post
                ViewBag.IsFirstPost = (from dpvm in dvm.DiscussionPostViewModels
                                       where dpvm.poster.CourseUser.ID == ActiveCourseUser.ID
                                       select dpvm).Count() == 0;


                //assigning a header value.
                if (assignment.HasDiscussionTeams)
                {
                    ViewBag.DiscussionHeader = assignment.AssignmentName + "- " + discussionTeam.TeamName;
                }
                else
                {
                    ViewBag.DiscussionHeader = assignment.AssignmentName;
                }

                //for CRD assignment types we need a list of all discussions they can participate in for navigation.
                if (assignment.Type == AssignmentTypes.CriticalReviewDiscussion)
                {
                    List<DiscussionTeam> DiscussionTeamList = new List<DiscussionTeam>();
                    //Generating a list of discussion assignments that the current user belongs to
                    foreach (DiscussionTeam dt in assignment.DiscussionTeams)
                    {
                        foreach (TeamMember tm in dt.GetAllTeamMembers())
                        {
                            if (tm.CourseUserID == ActiveCourseUser.ID)
                            {
                                DiscussionTeamList.Add(dt);
                                break;
                            }
                        }
                    }
                    ViewBag.DiscussionTeamList = DiscussionTeamList.OrderBy(dt => dt.TeamName).ToList();
                }
                if (displayNewPosts.HasValue && displayNewPosts.Value)
                {
                    ViewBag.HighlightValue = HighlightValue.NewPosts;
                }

                //Allow Moderators to post w/o word count restriction
                if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator)
                {
                    ViewBag.IsFirstPost = false;
                }

                ViewBag.LastVisit = GetAndUpdateLastVisit(discussionTeamId);
                ViewBag.CanPost = assignment.DueDate > DateTime.UtcNow;
                ViewBag.DiscussionPostViewModelList = dvm.DiscussionPostViewModels.OrderBy(dpvm => dpvm.Posted).ToList();
                ViewBag.ActiveCourse = ActiveCourseUser;
                ViewBag.Assignment = assignment;
                ViewBag.DiscussionTeamID = discussionTeam.ID;
                ViewBag.DiscussionTeam = discussionTeam;
                return View();
            }
            else //User is not part of discussion, kick them to assignment details.
            {
                return RedirectToAction("Index", "Home", new { area = "AssignmentDetails", assignmentId = assignmentId });
            }
        }
        /// <summary>
        /// Displays the Discussion view for Teachers
        /// </summary>
        /// <param name="assignmentId"></param>
        /// <param name="courseUserId">the CourseUser.ID of the student you want to "Highlight". 0 may be passed in highlighting is unnecessary</param>

        /// <param name="discussionTeamID">The discussion team id for discussion to be viewed. If it is a classwide discussion, then any dt in that assignment can be sent.</param>
        /// <returns></returns>
        [CanGradeCourse]
        public ActionResult TeacherIndex(int assignmentId, int discussionTeamID, int courseUserId = 0, HighlightValue hightlightValue = HighlightValue.None, bool anonymous = false)
        {
            Assignment assignment = db.Assignments.Find(assignmentId);
            if (assignment.CourseID == ActiveCourseUser.AbstractCourseID && ActiveCourseUser.AbstractRole.CanGrade)
            {


                List<DiscussionPost> posts = null;
                CourseUser student;
                DiscussionTeam discussionTeam = (from dt in assignment.DiscussionTeams
                                                 where dt.ID == discussionTeamID
                                                 select dt).FirstOrDefault();

                DiscussionViewModel dvm = new DiscussionViewModel(discussionTeam, ActiveCourseUser);
                
                //anonymize if requested
                if (anonymous == true)
                {
                    foreach (DiscussionPostViewModel vm in dvm.DiscussionPostViewModels)
                    {
                        vm.poster.Anonymize = true;
                        foreach (ReplyViewModel rvm in vm.Replies)
                        {
                            rvm.poster.Anonymize = true;
                        }
                    }
                }

                if (hightlightValue == HighlightValue.None || courseUserId <= 0)  //If hightlightValue == None, we want no selections - so setting student to null.
                {
                    student = null;
                }
                else
                {
                    student = db.CourseUsers.Find(courseUserId);
                }

                if (assignment.HasDiscussionTeams)
                {
                    ViewBag.DiscussionHeader = assignment.AssignmentName + " - " + discussionTeam.TeamName;
                }
                else
                {
                    ViewBag.DiscussionHeader = assignment.AssignmentName;
                }

                bool canPost = true;
                //If the user is a TA and TAs can only participate in some discussions, then we must confirm the TA
                //is in the team before we give them permission to post
                if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA && !assignment.DiscussionSettings.TAsCanPostToAllDiscussions
                    && discussionTeam.Team.TeamMembers.Where(tm => tm.CourseUserID == ActiveCourseUser.ID).ToList().Count == 0)
                {
                    canPost = false;
                }

                //for CRD assignment types we need a list of all discussions they can participate in for navigation.
                //additionally for CRD assignments we want to display all teammates invovled in the discussion
                if (assignment.HasDiscussionTeams)
                {
                    ViewBag.DiscussionTeamList = assignment.DiscussionTeams;

                }

                ViewBag.LastVisit = GetAndUpdateLastVisit(discussionTeamID);
                ViewBag.CanPost = canPost;
                ViewBag.DiscussionPostViewModelList = dvm.DiscussionPostViewModels;
                ViewBag.HighlightValue = hightlightValue;
                ViewBag.Posts = posts;
                ViewBag.Student = student;
                ViewBag.Assignment = assignment;
                ViewBag.IsFirstPost = false;
                ViewBag.ActiveCourse = ActiveCourseUser;
                ViewBag.DiscussionTeamID = discussionTeam.ID;
                ViewBag.IsAnonymous = anonymous;
                ViewBag.DiscussionTeam = discussionTeam;
                return View("Index");
            }
            return RedirectToAction("Index", "Home", new { area = "AssignmentDetails", assignmentId = assignmentId });
        }

        [HttpPost]
        public ActionResult NewPost(DiscussionPost newPost)
        {
            if (newPost != null && newPost.Content != null && newPost.AssignmentID > 0 && newPost.DiscussionTeamID > 0)
            {
                Assignment assignment = db.Assignments.Find(newPost.AssignmentID);
                newPost.CourseUserID = ActiveCourseUser.ID;
                db.DiscussionPosts.Add(newPost);
                db.SaveChanges();
                SendModeratorEmail(assignment, newPost);
                SendUserEmail(assignment, newPost);
            }
            return Redirect(Request.UrlReferrer.ToString());
        }

        [HttpPost]
        public ActionResult NewReply(DiscussionPost reply)
        {
            if (reply.Content != null && reply.ParentPostID > 0 && reply.DiscussionTeamID > 0 && reply.AssignmentID > 0)
            {
                Assignment assignment = db.Assignments.Find(reply.AssignmentID);
                reply.CourseUserID = ActiveCourseUser.ID;
                db.DiscussionPosts.Add(reply);
                db.SaveChanges();
                SendModeratorEmail(assignment, reply);
                SendUserEmail(assignment, reply);
            }
            return Redirect(Request.UrlReferrer.ToString());
        }

        private void SendModeratorEmail(Assignment assignment, DiscussionPost newPost)
        {
            if (assignment.DiscussionSettings != null)
            {
                if (assignment.DiscussionSettings.WillEmailInstructorsOnModeratorPost == true)
                {
                    if (newPost.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA || newPost.CourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Moderator)
                    {
                        //mail all instructors
                        List<MailAddress> to = new List<MailAddress>();
                        List<string> emailAddresses = db.CourseUsers
                            .Where(cu => cu.AbstractRole.CanGrade == true)
                            .Where(cu => cu.AbstractCourseID == assignment.CourseID)
                            .Select(cu => cu.UserProfile.UserName)
                            .ToList();
                        foreach (string address in emailAddresses)
                        {
                            //AC: sometimes this fails.  Not sure why
                            try
                            {
                                to.Add(new MailAddress(address));
                            }
                            catch (Exception)
                            {
                            }
                        }
                        string subject = "[OSBLE][Moderator] - New Post";
                        string linkUrl = string.Format("http://osble.org{0}", Url.Action("TeacherIndex", "DiscussionAssignment", new { assignmentID = assignment.ID, discussionTeamID = newPost.DiscussionTeamID }));
                        string body = @"
Greetings,

{0} has posted the following message on the discussion assignment ""{1}."":
{2}

You may view the discussion on OSBLE by visiting the following link: <a href=""{3}"">{4}</a>.

Thanks,
The OSBLE Team
";
                        body = string.Format(body,
                            ActiveCourseUser.UserProfile.DisplayName((int)CourseRole.CourseRoles.Instructor, true),
                            assignment.AssignmentName,
                            newPost.Content,
                            linkUrl,
                            linkUrl
                            );
                        Email.Send(subject, body, to);
                    }
                }
            }
        }

        private void SendUserEmail(Assignment assignment, DiscussionPost newPost)
        {
            //mail all users in the course with mail new discussion post setting set as true
            List<MailAddress> to = new List<MailAddress>();
            List<string> emailAddresses = new List<string>();
            Dictionary<int, string> teamMembers = new Dictionary<int, string>();

            List<Team> teams = db.DiscussionTeams
               .Where(dt => dt.AssignmentID == assignment.ID)
               .Where(dt => dt.ID == newPost.DiscussionTeamID)
               .Select(dt => dt.Team)
               .ToList();

            if (teams != null)
            {
                foreach (TeamMember member in teams[0].TeamMembers)
                {
                    teamMembers.Add(member.CourseUserID, member.CourseUser.UserProfile.UserName.ToString());
                }
            }

            if (assignment.DiscussionSettings.RequiresPostBeforeView == true)
            {
                Dictionary<int, string> PossibleUsers = db.CourseUsers
                    .Where(cu => cu.AbstractCourseID == assignment.CourseID)
                    .Where(cu => cu.UserProfileID != ActiveCourseUser.UserProfileID)
                    .Where(cu => cu.AbstractRoleID == 3)
                    .Where(cu => cu.UserProfile.EmailNewDiscussionPosts == true)
                    .Select(cu => new { cu.ID, cu.UserProfile.UserName })
                    .ToDictionary(cu => cu.ID, cu => cu.UserName);

                Dictionary<int, String> RealTeammembers = new Dictionary<int, string>();

                if (teams != null)
                {
                    foreach (KeyValuePair<int, string> possible_user in PossibleUsers)
                    {
                        if (!(teamMembers.ContainsKey(possible_user.Key)))
                        {

                        }
                        else
                        {
                            RealTeammembers.Add(possible_user.Key, possible_user.Value);
                        }
                    }

                    PossibleUsers = RealTeammembers;
                }

                foreach (DiscussionPost post in db.DiscussionPosts)
                {
                    if (PossibleUsers.ContainsKey(post.CourseUserID) && !(emailAddresses.Contains(PossibleUsers[post.CourseUserID].ToString())))
                    {                      
                        emailAddresses.Add(PossibleUsers[post.CourseUserID].ToString());
                    }
                }

            }
            else
            {

                if (teams != null)
                {
                    Dictionary<int, string> PossibleUsers = db.CourseUsers
                        .Where(cu => cu.AbstractCourseID == assignment.CourseID)
                        .Where(cu => cu.UserProfileID != ActiveCourseUser.UserProfileID)
                        .Where(cu => cu.AbstractRoleID == 3)
                        .Where(cu => cu.UserProfile.EmailNewDiscussionPosts == true)
                        .Select(cu => new { cu.ID, cu.UserProfile.UserName })
                        .ToDictionary(cu => cu.ID, cu => cu.UserName);

                    Dictionary<int, String> RealTeammembers = new Dictionary<int, string>();

                    foreach (KeyValuePair<int, string> possible_user in PossibleUsers)
                    {
                        if (!(teamMembers.ContainsKey(possible_user.Key)))
                        {

                        }
                        else
                        {
                            RealTeammembers.Add(possible_user.Key, possible_user.Value);
                        }
                    }

                    foreach (KeyValuePair<int, string> possible_user in RealTeammembers)
                    {
                        emailAddresses.Add(possible_user.Value);
                    }

                }
                else
                {
                    emailAddresses = db.CourseUsers
                        .Where(cu => cu.AbstractCourseID == assignment.CourseID)
                        .Where(cu => cu.UserProfileID != ActiveCourseUser.UserProfileID)
                        .Where(cu => cu.AbstractRoleID == 3)
                        .Where(cu => cu.UserProfile.EmailNewDiscussionPosts == true)
                        .Select(cu => cu.UserProfile.UserName)
                        .ToList();
                }
            }

            foreach (string address in emailAddresses)
            {
            
                    //AC: sometimes this fails.  Not sure why
                    try
                    {
                        to.Add(new MailAddress(address));
                    }
                    catch (Exception)
                    {
                    }
                
            }

            bool anonSettings = false;
            if (assignment.DiscussionSettings.AnonymitySettings > 0)
            {
                anonSettings = true;
            }

            string subject = "[OSBLE][Discussion] - New Post";
            string linkUrl = string.Format("http://osble.org{0}", Url.Action("TeacherIndex", "DiscussionAssignment", new { assignmentID = assignment.ID, discussionTeamID = newPost.DiscussionTeamID }));
            string body = @"
Greetings,

{0} has posted the following message on the discussion assignment ""{1}."":
{2}

You may view the discussion on OSBLE by visiting the following link: <a href=""{3}"">{4}</a>.

Thanks,
The OSBLE Team
";

            body = string.Format(body,
    ActiveCourseUser.UserProfile.DisplayName(ActiveCourseUser.AbstractRoleID, true, anonSettings),
    assignment.AssignmentName,
    newPost.Content,
    linkUrl,
    linkUrl
    );
            Email.Send(subject, body, to);

        }

        [HttpGet, FileCache(Duration = 3600)]
        public FileStreamResult ProfilePictureForDiscussion(int course, int userProfile)
        {
            // File Stream that will ultimately contain profile picture.
            Stream pictureStream;

            // User Profile object of user we are trying to get a picture of
            UserProfile u = db.UserProfiles.Find(userProfile);

            // A role for both our current user and
            // the one we're trying to see
            AbstractRole ourRole = currentCourses.Where(c => c.AbstractCourseID == course).Select(c => c.AbstractRole).FirstOrDefault();
            AbstractRole theirRole = db.CourseUsers.Where(c => (c.AbstractCourseID == course) && (c.UserProfileID == userProfile)).Select(c => c.AbstractRole).FirstOrDefault();

            // Show picture if user is requesting their own profile picture or they have the right to view the profile picture
            if (userProfile == CurrentUser.ID ||
                // Current user's CourseRole
                ourRole != null &&
                // Target user's CourseRole
                theirRole != null &&
                // If current user is not anonymous or other user is instructor/TA, show picture
                (!(ourRole.Anonymized) || theirRole.CanGrade)
               )
            {
                pictureStream = FileSystem.GetProfilePictureOrDefault(u);
            }
            else
            {
                // Default to blue OSBLE guy picture.
                pictureStream = FileSystem.GetDefaultProfilePicture();
            }

            return new FileStreamResult(pictureStream, "image/jpeg");
        }
    }
}
