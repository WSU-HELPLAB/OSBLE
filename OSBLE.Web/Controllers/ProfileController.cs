using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Dapper;
using Microsoft.Win32.SafeHandles;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.Queries;
using OSBLE.Models.Users;
using OSBLE.Utility;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility.Lookups;
using OSBLEPlus.Services.Models;
using Image = System.Drawing.Image;
using OSBLE.Models.Courses;

namespace OSBLE.Controllers
{
    public class ProfileController : OSBLEController
    {
        //
        // GET: /Profile/
        [OsbleAuthorize]
        public ActionResult Index(int? id, int timestamp = -1)
        {
            if (null != ActiveCourseUser)
            {
                ViewBag.HideMail = OSBLE.Utility.DBHelper.GetAbstractCourseHideMailValue(ActiveCourseUser.AbstractCourseID);
            }
            else
            {
                ViewBag.HideMail = true;
                ViewBag.errorMessage = "No user information yet available. Please sign up for a course first.";
                ViewBag.errorName = "No Courses";
                return View("Error");
            } 

            try
            {
                var query = new ActivityFeedQuery(ActiveCourseUser.AbstractCourseID);
                var subscriptionsQuery = new ActivityFeedQuery(ActiveCourseUser.AbstractCourseID);
                ProfileViewModel vm = new ProfileViewModel();
                vm.User = CurrentUser;
                if (id != null)
                {
                    UserProfile user = DBHelper.GetUserProfile((int)id);
                    if (user != null && ActiveCourseUser.AbstractCourseID != (int)CourseRole.CourseRoles.Observer)
                    {
                        vm.User = user;
                    }
                }

                if (timestamp > 0)
                {
                    DateTime pullDate = new DateTime(timestamp);
                    query.StartDate = pullDate;
                }

                //Only show social events
                foreach (var evt in ActivityFeedQuery.GetSocialEvents())
                {
                    query.AddEventType(evt);
                    subscriptionsQuery.AddEventType(evt);
                }

                //add in the list of users that the current person cares about
                query.AddSubscriptionSubject(vm.User);

                //build the feed view model
                vm.Feed = new FeedViewModel();
                vm.Feed.Feed = AggregateFeedItem.FromFeedItems(query.Execute().ToList());
                vm.Feed.LastLogId = -1;
                vm.Feed.SingleUserId = vm.User.ID;
                vm.Feed.LastPollDate = query.StartDate;
                //vm.Score = Db.UserScores.Where(s => s.UserId == vm.User.Id).FirstOrDefault();

                using (SqlConnection conn = DBHelper.GetNewConnection())
                {
                    int AskForHelpValue = conn.Query<int>("SELECT e.EventTypeId " +
                                                          "FROM EventTypes e " +
                                                          "WHERE e.EventTypeName = 'AskForHelpEvent' ").FirstOrDefault();
                    int FeedPostValue = conn.Query<int>("SELECT e.EventTypeId " +
                                                        "FROM EventTypes e " +
                                                        "WHERE e.EventTypeName = 'FeedPostEvent'").FirstOrDefault();
                    int LogCommentValue = conn.Query<int>("SELECT e.EventTypeId " +
                                                          "FROM EventTypes e " +
                                                          "WHERE e.EventTypeName = 'LogCommentEvent'").FirstOrDefault();

                    var posts = conn.Query<EventLog>(
                        "SELECT * " +
                        "FROM EventLogs " +
                        "WHERE EventTypeId = @fpe " +
                        "AND SenderId = @UserId " +
                        "ORDER BY EventDate DESC",
                        new { fpe = FeedPostValue, afhe = AskForHelpValue, UserId = vm.User.ID }).ToList();
                    vm.NumberOfPosts = posts.Count;

                    var comments = DBHelper.GetCommentsForUserID(vm.User.ID, conn);
                    //var comments = conn.Query<EventLog>(
                    //    "SELECT * " +
                    //    "FROM EventLogs e " +
                    //    "WHERE (e.EventTypeId = @lcv " +
                    //    "AND e.SenderId = @UserId) " +
                    //    "ORDERBY e.EventDate DESCENDING", new { lcv = LogCommentValue, UserId = CurrentUser.ID }).ToList();
                    vm.NumberOfComments = comments.Count();

                    //vm.NumberOfPosts = (from e in Db.EventLogs
                    //                    where (e.LogType == FeedPostEvent.Name || e.LogType == AskForHelpEvent.Name)
                    //                    && e.SenderId == vm.User.Id
                    //                    select e
                    //                    ).Count();
                    //vm.NumberOfComments = Db.LogCommentEvents.Where(c => c.EventLog.SenderId == vm.User.Id).Count();
                    //if (vm.Score == null)
                    //{
                    //    vm.Score = new UserScore();
                    //}

                    ////// need to figure this out
                    //////var maxQuery = Db.EventLogs.Where(e => e.SenderId == vm.User.Id).Select(e => e.Id);
                    //////if (maxQuery.Count() > 0)
                    //////{
                    //////    vm.Feed.LastLogId = maxQuery.Max();
                    //////}

                    // Build a catalog of recent commenting activity:
                    // 1. Find all comments that the user has made
                    // 2. Find all comments made by others on posts authored by the current user
                    // 3. Find all comments made by others on posts on which the current user has written a comment

                    DateTime maxLookback = DateTime.UtcNow.AddDays(-14);

                    //1. find recent comments
                    //c.LogCommentEvent
                    //c.Id
                    //c.LogCommentEventId
                    //c.UserProfile
                    //c.UserProfileId

                    int i = 0;
                    foreach (LogCommentEvent lce in comments)
                    {
                        lce.SourceEvent = DBHelper.GetActivityEvent(lce.SourceEventLogId, conn);
                        lce.Sender = vm.User;

                        CommentActivityLog cal = new CommentActivityLog()
                        {
                            Id = i,
                            LogCommentEvent = lce,
                            LogCommentEventId = lce.EventLogId,
                            UserProfile = vm.User,
                            UserProfileId = vm.User.ID
                        };
                        if (ActiveCourseUser.AbstractCourseID == (int)CourseRole.CourseRoles.Observer)
                        {
                            cal.UserProfileId = 0;
                        }

                        i++;
                        vm.SocialActivity.AddLog(cal);
                    }

                    //List<CommentActivityLog> socialLogs = (from social in Db.CommentActivityLogs
                    //    .Include("TargetUser")
                    //    .Include("LogCommentEvent")
                    //    .Include("LogCommentEvent.SourceEventLog")
                    //    .Include("LogCommentEvent.SourceEventLog.Sender")
                    //    .Include("LogCommentEvent")
                    //    .Include("LogCommentEvent.EventLog")
                    //    .Include("LogCommentEvent.EventLog.Sender")
                    //    where 1 == 1
                    //          && social.LogCommentEvent.EventDate >= maxLookback
                    //          &&
                    //          (social.TargetUserId == vm.User.Id ||
                    //           social.LogCommentEvent.SourceEventLog.SenderId == vm.User.Id)
                    //    orderby social.LogCommentEvent.EventDate descending
                    //    select social
                    //    ).ToList();

                }
                //show subscriptions only if the user is accessing his own page
                //if (vm.User.Id == CurrentUser.Id)
                //{
                //    List<int> eventLogIds = Db.EventLogSubscriptions.Where(s => s.UserId == vm.User.Id).Select(s => s.LogId).ToList();
                //    if (eventLogIds.Count > 0)
                //    {
                //        foreach (int logId in eventLogIds)
                //        {
                //            subscriptionsQuery.AddEventId(logId);
                //        }
                //        vm.EventLogSubscriptions = AggregateFeedItem.FromFeedItems(subscriptionsQuery.Execute().ToList());
                //    }

                //}

                ViewBag.IsInstructor = ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor;
                ViewBag.IsSelf = ActiveCourseUser != null ? ActiveCourseUser.UserProfileID == id || id == null : false;
                ViewBag.UploadedImages = GetImageFilesForCurrentUser();

                return View(vm);
            }
            catch (Exception ex)
            {
                //LogErrorMessage(ex);
                ViewBag.errorMessage = ex.Message;
                ViewBag.errorName = ex.GetType().ToString();
                return View("Error");
            }
        }

        //[OsbleAuthorize]
        //public ActionResult Edit()
        //{
        //    try
        //    {
        //        return System.Web.UI.WebControls.View(BuildEditViewModel());
        //    }
        //    catch (Exception ex)
        //    {
        //        //LogErrorMessage(ex);
        //        //return RedirectToAction("Index", "Error");
        //    }
        //}

        //[OsbideAuthorize]
        //[HttpPost]
        //public ActionResult Edit(EditProfileViewModel vm)
        //{
        //    try
        //    {
        //        if (ModelState.IsValid)
        //        {
        //            // We can determine which is desired by checking which button was pressed
        //            if (Request.Form["updateBasic"] != null)
        //            {
        //                UpdateBasicSettings(vm);
        //            }
        //            else if (Request.Form["updateEmail"] != null)
        //            {
        //                UpdateEmail(vm);
        //            }
        //            else if (Request.Form["updatePassword"] != null)
        //            {
        //                UpdatePassword(vm);
        //            }
        //            else if (Request.Form["updateSubscriptions"] != null)
        //            {
        //                UpdateSubscriptions(vm);
        //            }
        //            else if (Request.Form["changeEmailNotifications"] != null)
        //            {
        //                UpdateEmailNotificationSettings(vm);
        //            }
        //            else if (Request.Form["CourseToRemove"] != null)
        //            {
        //                int courseId = -1;
        //                Int32.TryParse(Request.Form["CourseToRemove"].ToString(), out courseId);
        //                RemoveUserFromCourse(courseId, vm);
        //            }

        //        }
        //        return System.Web.UI.WebControls.View("Edit", BuildEditViewModel(vm));
        //    }
        //    catch (Exception ex)
        //    {
        //        //LogErrorMessage(ex);
        //        //return RedirectToAction("Index", "Error");
        //    }
        //}

        //[OsbideAuthorize]
        //public ActionResult RemoveCourse(int courseId)
        //{
        //    EditProfileViewModel vm = BuildEditViewModel();
        //    RemoveUserFromCourse(courseId, vm);
        //    return RedirectToAction("Edit");
        //}

        //#region Edit helper methods

        //private EditProfileViewModel BuildEditViewModel(EditProfileViewModel oldVm = null)
        //{
        //    EditProfileViewModel vm = new EditProfileViewModel();
        //    if (oldVm != null)
        //    {
        //        vm = oldVm;
        //    }
        //    vm.User = CurrentUser;
        //    vm.ReceiveEmailNotificationsForPosts = CurrentUser.ReceiveNotificationEmails;
        //    vm.ReceiveEmailsOnFeedPost = CurrentUser.ReceiveEmailOnNewFeedPost;
        //    vm.ReceiveEmailsOnNewAskForHelp = CurrentUser.ReceiveEmailOnNewAskForHelp;
        //    vm.UsersInCourse = Db.Users.Where(u => u.SchoolId == CurrentUser.SchoolId).ToList();
        //    StudentSubscriptionsQuery subs = new StudentSubscriptionsQuery(Db, CurrentUser);
        //    List<OsbideUser> subscriptionsAsUsers = subs.Execute().ToList();
        //    List<UserSubscription> subscriptions = Db
        //        .UserSubscriptions.Where(s => s.ObserverSchoolId == CurrentUser.SchoolId)
        //        .Where(s => s.ObserverInstitutionId == CurrentUser.InstitutionId)
        //        .ToList();
        //    foreach (OsbideUser user in subscriptionsAsUsers)
        //    {
        //        UserSubscription us = subscriptions.Where(s => s.SubjectInstitutionId == user.InstitutionId).FirstOrDefault();
        //        if (us != null)
        //        {
        //            vm.UserSubscriptions[user.Id] = us;
        //        }
        //    }

        //    //set up school choices
        //    List<School> schools = Db.Schools.ToList();
        //    ViewBag.Schools = schools;

        //    return vm;
        //}

        ///// <summary>
        ///// Removes a student from a course
        ///// </summary>
        ///// <param name="courseId"></param>
        ///// <param name="vm"></param>
        //private void RemoveUserFromCourse(int courseId, EditProfileViewModel vm)
        //{
        //    //set last active pane
        //    vm.LastActivePane = 2;

        //    CourseUserRelationship toRemove = Db.CourseUserRelationships
        //        .Where(c => c.CourseId == courseId)
        //        .Where(c => c.UserId == CurrentUser.Id)
        //        .FirstOrDefault();
        //    List<CourseUserRelationship> allRelationships = Db.CourseUserRelationships.Where(cur => cur.UserId == CurrentUser.Id).ToList();

        //    //does the current user only have one course?
        //    if (allRelationships.Count() <= 1)
        //    {
        //        vm.RemoveCourseMessage = "You must always be enrolled in at least one course.  To remove this course, please first add an additional course.";
        //    }
        //    else
        //    { //ELSE: user has at least two courses
        //        if (toRemove != null)
        //        {
        //            //is the course being removed the current default course?
        //            if (CurrentUser.DefaultCourseId == toRemove.CourseId)
        //            {
        //                //switch default course to another course
        //                CourseUserRelationship other = allRelationships.Where(cur => cur.CourseId != toRemove.CourseId).FirstOrDefault();
        //                CurrentUser.DefaultCourseId = other.CourseId;
        //            }

        //            vm.RemoveCourseMessage = string.Format("You have been removed from {0}.", toRemove.Course.Name);
        //            Db.Entry(toRemove).State = System.Data.Entity.EntityState.Deleted;
        //            Db.SaveChanges();
        //        }
        //        else
        //        {
        //            vm.RemoveCourseMessage = "An error occurred when I attempted to remove you from the course.  Please try again.  If the problem persists, please contact support at \"support@osbide.com\".";
        //        }
        //    }

        //}

        //private void UpdateBasicSettings(EditProfileViewModel vm)
        //{
        //    //make sure that the specified school ID / institution ID isn't already daken
        //    OsbideUser dbUser = Db.Users
        //                          .Where(u => u.SchoolId == vm.User.SchoolId)
        //                          .Where(u => u.InstitutionId == vm.User.InstitutionId)
        //                          .FirstOrDefault();
        //    if (dbUser != null)
        //    {
        //        if (dbUser.Id != CurrentUser.Id)
        //        {
        //            vm.UpdateBasicSettingsMessage = "The specified school / institution ID is already taken";
        //            return;
        //        }
        //    }
        //    CurrentUser.FirstName = vm.User.FirstName;
        //    CurrentUser.LastName = vm.User.LastName;
        //    CurrentUser.SchoolId = vm.User.SchoolId;
        //    CurrentUser.InstitutionId = vm.User.InstitutionId;
        //    CurrentUser.Gender = vm.User.Gender;
        //    vm.UpdateBasicSettingsMessage = "Your settings have been updated.";
        //}

        //private void UpdateEmailNotificationSettings(EditProfileViewModel vm)
        //{
        //    CurrentUser.ReceiveNotificationEmails = vm.ReceiveEmailNotificationsForPosts;
        //    CurrentUser.ReceiveEmailOnNewAskForHelp = vm.ReceiveEmailsOnNewAskForHelp;
        //    CurrentUser.ReceiveEmailOnNewFeedPost = vm.ReceiveEmailsOnFeedPost;
        //    vm.UpdateEmailSettingsMessage = "Your email settings have been updated.";
        //}

        //private void UpdateSubscriptions(EditProfileViewModel vm)
        //{
        //    //remove all current subscriptions that are not required
        //    List<UserSubscription> nonEssentialSubscriptions = Db.UserSubscriptions
        //        .Where(s => s.ObserverInstitutionId == CurrentUser.InstitutionId)
        //        .Where(s => s.ObserverSchoolId == CurrentUser.SchoolId)
        //        .Where(s => s.IsRequiredSubscription == false)
        //        .ToList();
        //    foreach (UserSubscription subscription in nonEssentialSubscriptions)
        //    {
        //        Db.UserSubscriptions.Remove(subscription);
        //    }
        //    Db.SaveChanges();

        //    //add in requested subscriptions
        //    foreach (string key in Request.Form.Keys)
        //    {
        //        if (key.StartsWith("subscription_") == true)
        //        {
        //            int userId = -1;
        //            string[] pieces = key.Split('_');
        //            if (pieces.Length == 2)
        //            {
        //                if (Int32.TryParse(pieces[1], out userId) == true)
        //                {
        //                    OsbideUser user = Db.Users.Where(u => u.Id == userId).FirstOrDefault();
        //                    if (user != null)
        //                    {
        //                        UserSubscription sub = new UserSubscription()
        //                        {
        //                            IsRequiredSubscription = false,
        //                            ObserverSchoolId = CurrentUser.SchoolId,
        //                            ObserverInstitutionId = CurrentUser.InstitutionId,
        //                            SubjectSchoolId = user.SchoolId,
        //                            SubjectInstitutionId = user.InstitutionId
        //                        };
        //                        Db.UserSubscriptions.Add(sub);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    Db.SaveChanges();

        //}

        //private void UpdateEmail(EditProfileViewModel vm)
        //{
        //    //Attempt to update email address.
        //    //Check to make sure email address isn't in use
        //    OsbideUser user = Db.Users.Where(u => u.Email.CompareTo(vm.NewEmail) == 0).FirstOrDefault();
        //    if (user == null && string.IsNullOrEmpty(vm.NewEmail) == false)
        //    {
        //        //update email address
        //        CurrentUser.Email = vm.NewEmail;

        //        //the email address acts as the hash for the user's password so we've got to change that as well
        //        UserPassword up = Db.UserPasswords.Where(p => p.UserId == CurrentUser.Id).FirstOrDefault();
        //        up.Password = UserPassword.EncryptPassword(vm.OldPassword, CurrentUser);
        //        Db.SaveChanges();

        //        vm.UpdateEmailSuccessMessage = string.Format("Your email has been successfully updated to \"{0}.\"", CurrentUser.Email);
        //    }
        //    else
        //    {
        //        ModelState.AddModelError("", "The requested email is already in use.");
        //    }
        //}

        //private void UpdatePassword(EditProfileViewModel vm)
        //{
        //    //update the user's password
        //    UserPassword up = Db.UserPasswords.Where(p => p.UserId == CurrentUser.Id).FirstOrDefault();
        //    if (up != null && string.IsNullOrEmpty(vm.NewPassword) == false)
        //    {
        //        up.Password = UserPassword.EncryptPassword(vm.NewPassword, CurrentUser);
        //        Db.SaveChanges();
        //        vm.UpdatePasswordSuccessMessage = "Your password has been successfully updated.";
        //    }
        //    else
        //    {
        //        ModelState.AddModelError("", "An error occurred while updating your password.  Please try again.  If the problem persists, please contact support at \"support@osbide.com\".");
        //    }
        //}
        //#endregion

        public FileStreamResult DefaultPicture(int size = 128)
        {
            try
            {
                string defaultImageLocation = Server.MapPath("/Content/icons/anonymous.png");
                MemoryStream defaultStream = new MemoryStream();
                using (Image defaultImage = Image.FromFile(defaultImageLocation))
                {
                    defaultImage.Save(defaultStream, System.Drawing.Imaging.ImageFormat.Png);
                }
                defaultStream.Position = 0;
                return new FileStreamResult(defaultStream, "image/png");
            }
            catch (Exception ex)
            {
                //LogErrorMessage(ex);
                return null;
            }
        }

        /// <summary>
        /// Returns the profile picture for the supplied user id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public FileStreamResult Picture(int id, int size = 128)
        {
            try
            {
                //invalid user ID?
                if (id < 1)
                {
                    return DefaultPicture(size);
                }

                using (SqlConnection conn = DBHelper.GetNewConnection())
                {
                    ProfileImage image = conn.Query<ProfileImage>("FROM ProfileImages p " +
                                                                  "WHERE UserID=@UserId" +
                                                                  "Select p", new {UserId = CurrentUser.ID}).Single();

                    //ProfileImage image = //Db.ProfileImages.Where(p => p.UserID == id).FirstOrDefault();
                    IdenticonRenderer renderer = new IdenticonRenderer();
                    Bitmap userBitmap;
                    if (image != null)
                    {
                        try
                        {
                            userBitmap = image.GetProfileImage();
                        }
                        catch (Exception)
                        {
                            
                            userBitmap = renderer.Render(image.GetHashCode(), 128);
                            //.SetProfileImage(userBitmap);
                            //Db.SaveChanges();
                        }
                    }
                    else
                    {
                        UserProfile user = conn.Query<UserProfile>("FROM UserProfiles u " +
                                                                   "WHERE u.Id = @UserId " +
                                                                   "SELECT u").Single();
                        //(Db.Users.Where(u => u.Id == id).FirstOrDefault());
                        if (user != null && user.ProfileImage == null)
                        {
                            try
                            {
                                user.SetProfileImage(renderer.Render(user.Email.GetHashCode(), 128));
                                userBitmap = user.ProfileImage.GetProfileImage();
                                //Db.SaveChanges();
                            }
                            catch (Exception)
                            {
                                userBitmap = renderer.Render(1, 128);
                            }
                        }
                        else
                        {
                            userBitmap = renderer.Render(1, 128);
                        }
                    }

                    if (size != 128)
                    {
                        Bitmap bmp = new Bitmap(userBitmap, size, size);
                        Graphics graph = Graphics.FromImage(userBitmap);
                        graph.InterpolationMode = InterpolationMode.High;
                        graph.CompositingQuality = CompositingQuality.HighQuality;
                        graph.SmoothingMode = SmoothingMode.AntiAlias;
                        graph.DrawImage(bmp, new Rectangle(0, 0, size, size));
                        userBitmap = bmp;
                    }

                    MemoryStream stream = new MemoryStream();
                    userBitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                    stream.Position = 0;
                    return new FileStreamResult(stream, "image/png");
                }
            }
            catch (Exception ex)
            {
                //LogErrorMessage(ex);
                return null;
            }
        }

        //[HttpPost]
        //[OsbideAuthorize]
        //public ActionResult Picture(HttpPostedFileBase file)
        //{
        //    try
        //    {
        //        //two options: user uploaded a profile picture 
        //        //             OR user requested a default profile picture
        //        if (Request.Params["upload"] != null)
        //        {
        //            //if the file is null, check the Request.Files construct before giving up
        //            if (file == null)
        //            {
        //                try
        //                {
        //                    file = Request.Files["file"];
        //                }
        //                catch (Exception)
        //                {
        //                }
        //            }
        //            if (file != null) // Upload Picture
        //            {
        //                Bitmap image;
        //                try
        //                {
        //                    image = new Bitmap(file.InputStream);
        //                }
        //                catch
        //                {   // If image format is invalid, discard it.
        //                    image = null;
        //                }

        //                if (image != null)
        //                {
        //                    int thumbSize = 128;

        //                    // Crop image to a square.
        //                    int square = Math.Min(image.Width, image.Height);
        //                    using (Bitmap cropImage = new Bitmap(square, square))
        //                    {
        //                        using (Bitmap finalImage = new Bitmap(thumbSize, thumbSize))
        //                        {
        //                            Graphics cropGraphics = Graphics.FromImage(cropImage);
        //                            Graphics finalGraphics = Graphics.FromImage(finalImage);

        //                            // Center cropped image horizontally, leave at the top vertically. (better focus on subject)
        //                            cropGraphics.DrawImage(image, -(image.Width - cropImage.Width) / 2, 0);

        //                            // Convert to thumbnail.
        //                            finalGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        //                            finalGraphics.DrawImage(cropImage,
        //                                                new Rectangle(0, 0, thumbSize, thumbSize),
        //                                                new Rectangle(0, 0, square, square),
        //                                                GraphicsUnit.Pixel);

        //                            // Write image to user's profile
        //                            CurrentUser.SetProfileImage(finalImage);
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            //reset to default profile picture
        //            IdenticonRenderer renderer = new IdenticonRenderer();
        //            CurrentUser.SetProfileImage(renderer.Render(CurrentUser.Email.GetHashCode(), 128));
        //        }
        //        return RedirectToAction("Edit");
        //    }
        //    catch (Exception ex)
        //    {
        //        LogErrorMessage(ex);
        //        return RedirectToAction("Index", "Error");
        //    }
        //}

        [HttpPost]
        public ActionResult UploadImages(IEnumerable<HttpPostedFileBase> files)
        {
            List<string> allowedExtensions = new List<string>(new string[] { ".jpg", ".jpeg", ".png", ".gif", ".gifv", ".bmp" });            

            foreach (var file in files)
            {                
                if (file != null && file.ContentLength > 0 && allowedExtensions.Contains(Path.GetExtension(file.FileName)))
                {
                    string path = AppDomain.CurrentDomain.BaseDirectory + "Content\\OSBLEImages\\" + ActiveCourseUser.UserProfileID.ToString() + "\\";
                    (new FileInfo(path)).Directory.Create();                    
                    file.SaveAs(path +  file.FileName );
                }
            }
            return RedirectToAction("Index", new { id = ActiveCourseUser.UserProfileID});
        }

        [HttpPost]
        public ActionResult DeleteImage()
        {
            string filename = Request.Form["fileName"];
            string path = AppDomain.CurrentDomain.BaseDirectory + "Content\\OSBLEImages\\" + ActiveCourseUser.UserProfileID.ToString() + "\\";
            if (filename.Length > 0 && System.IO.File.Exists(path + filename))
            {
                System.IO.File.Delete(path + filename);
            }

            return RedirectToAction("Index", new { id = ActiveCourseUser.UserProfileID });
        }

        private List<string> GetImageFilesForCurrentUser()
        {
            List<string> fileList = new List<string>();
            DirectoryInfo d = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory + "Content\\OSBLEImages\\" + ActiveCourseUser.UserProfileID.ToString() + "\\");
            if (Directory.Exists(d.FullName))
            {
                FileInfo[] Files = d.GetFiles("*.*");
                string str = "";
                foreach (FileInfo file in Files)
                {
                    fileList.Add(file.Name);
                }
            }
            return fileList;
        }        
    }
}
