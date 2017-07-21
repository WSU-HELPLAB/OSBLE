using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Mvc;
using Dapper;
using OSBLE.Attributes;
using OSBLE.Models.Courses;
using OSBLE.Models.Queries;
using OSBLE.Models;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Models.HomePage;
using OSBLE.Utility;
using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Auth;
using OSBLEPlus.Logic.Utility.Lookups;
using OSBLE.Hubs;
using Microsoft.AspNet.SignalR;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Web.UI;
using System.IO;
using OSBLEPlus.Services.Controllers;

namespace OSBLE.Controllers
{

    [OsbleAuthorize]
    public class FeedController : OSBLEController
    {
        //private UserFeedSetting _userSettings = ;
        private ActivityFeedQuery _activityFeedQuery;

        public FeedController()
        {
            // try to fix activeCourseUser for plugin
            if (ActiveCourseUser == null && CurrentUser != null)
            {
                GetEnrolledCourses();
                if (ActiveCourseUser == null)
                {
                    ViewBag.ErrorName = "User not logged in or registered.";
                    ViewBag.ErrorMessage = "User needs to register an account and be approved for a class";
                    return;
                }

                ViewBag.ErrorName = null;
                ViewBag.ErrorMessage = null;
            }

            // after check make sure we set a courseUser before we make an instalce of the activityFeedQuery
            if (ActiveCourseUser == null) return;
            _activityFeedQuery = new ActivityFeedQuery(ActiveCourseUser.AbstractCourseID) { MaxQuerySize = 20 };
            _activityFeedQuery.UpdateEventSelectors(ActivityFeedQuery.GetSocialEvents());
        }

        /// <summary>
        /// Index, returns a view with the Activity Feed and the inputs for 
        /// creating new posts and filtering.
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            //turned off for now.
            //return RedirectToAction("FeedDown", "Error");

            // give the user an error message, specifically for the plugin
            if (ViewBag.ErrorName != null)
            {
                return View("Error");
            }

            try
            {
                if (ActiveCourseUser.AbstractRole.CanGrade)
                {
                    ViewBag.CanGrade = true;
                }
                else
                {
                    ViewBag.CanGrade = false;
                }

                if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                {
                    ViewBag.IsInstructor = true;
                }
                else
                {
                    ViewBag.IsInstructor = false;
                }

                ViewBag.EnableCustomPostVisibility = ConfigurationManager.AppSettings["EnableCustomPostVisibility"]; //<add key="EnableCustomPostVisibility" value="false"/> in web.config

                //check if interventions are enabled on this course
                ViewBag.InterventionsEnabled = DBHelper.InterventionEnabledForCourse(ActiveCourseUser.AbstractCourseID);

                return PartialView();
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return PartialView("Error");
            }

        }

        /// <summary>
        /// This action returns the same view as Index, but inside a wrapper view
        /// that makes sure all componenets such as jquery and bootstrap are included.
        /// This is so the OSBIDE viewer does not include the _layout.cshtml layout,
        /// which normally includes these things.
        /// </summary>
        /// <returns></returns>
        [OsbleAuthorize]
        public ActionResult OSBIDE(int? courseID)
        {
            if (courseID != null)
            {
                SetCourseID(courseID.Value);
            }
            else if (ActiveCourseUser != null)
            {
                courseID = ActiveCourseUser.AbstractCourseID;
            }

            if (ActiveCourseUser == null || courseID == null) //may get here using the plugin
            {
                using (AccountController account = new AccountController())
                {
                    Authentication authenticate = new Authentication();
                    return account.TokenLogin(authenticate.GetAuthenticationKey(), StringConstants.WebClientRoot + "/feed/osbide/");
                }
            }

            if (ActiveCourseUser.AbstractRole.CanGrade)
            {
                ViewBag.CanGrade = true;
            }
            else
            {
                ViewBag.CanGrade = false;
            }

            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                ViewBag.IsInstructor = true;
            else
                ViewBag.IsInstructor = false;

            ViewBag.ActiveCourse = DBHelper.GetCourseUserFromProfileAndCourse(ActiveCourseUser.UserProfileID, (int)courseID);

            //setup user list for autocomplete            
            var viewableProfiles = DBHelper.GetUserProfilesForCourse(ActiveCourseUser.AbstractCourseID);
            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Observer) //If the current user is an Observer, remove everyone besides them and instructors from viewableProfiles
            {
                bool isSelf;
                bool isInstructor;
                var instructors = DBHelper.GetCourseInstructorIds(ActiveCourseUser.AbstractCourseID);
                var tempViewableProfiles = new List<UserProfile>(viewableProfiles); //Need a deep copy of the list to iterate over so elements can be removed if necessary
                foreach (var profile in tempViewableProfiles)
                {
                    isSelf = profile.ID == ActiveCourseUser.UserProfileID ? true : false;
                    isInstructor = instructors.Contains(profile.ID) ? true : false;
                    if (!isSelf && !isInstructor)
                    {
                        viewableProfiles.Remove(profile);
                    }
                }
            }
            ViewBag.CurrentCourseUsers = viewableProfiles;

            ViewBag.HashTags = DBHelper.GetHashTags();

            ViewBag.EnableCustomPostVisibility = ConfigurationManager.AppSettings["EnableCustomPostVisibility"]; //<add key="EnableCustomPostVisibility" value="false"/> in web.config

            //check if interventions are enabled on this course
            ViewBag.InterventionsEnabled = DBHelper.InterventionEnabledForCourse(ActiveCourseUser.AbstractCourseID);

            return View("Index", "_OSBIDELayout", courseID);
        }

        public bool SetCourseID(int courseId)
        {
            try
            {
                Cache["ActiveCourse"] = courseId;
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        [HttpPost]
        private string GetEventId(FeedItem f)
        {
            return f.Event.EventId.ToString();
        }

        private FeedViewModel GetFeedViewModel()
        {
            var query = _activityFeedQuery;

            FeedViewModel vm = new FeedViewModel();

            List<FeedItem> initialReturnItems = _activityFeedQuery.Execute().ToList();
            List<FeedItem> returnItems = new List<FeedItem>();

            // store the current End date so we can restore it later
            DateTime temp = _activityFeedQuery.EndDate;

            for (int i = 0; i < 10; i++)
            {
                //trim any posts the user is not a part of
                foreach (FeedItem item in initialReturnItems)
                {
                    try
                    {
                        //filter out any posts that the current user should not be able to see
                        string eventVisibleToList = DBHelper.GetEventLogVisibleToList(item.Event.EventLogId);

                        if (eventVisibleToList != "" ? InEventVisibleToList(eventVisibleToList, ActiveCourseUser.UserProfileID) : true)
                        {
                            returnItems.Add(item);
                        }
                    }
                    catch (Exception)
                    {
                        //do nothing for now.
                    }
                }

                //get more posts if we don't have 20
                if (returnItems.Count() >= 20)
                    break; //we have 20, exit... otherwise try to get more posts.  

                if (i != 9) //only clear the list if it's not the last iteration
                {
                    _activityFeedQuery.MaxQuerySize += 20;
                    initialReturnItems = _activityFeedQuery.Execute().ToList();
                    returnItems = new List<FeedItem>();
                }
            }

            // restore the original end date and query size
            _activityFeedQuery.EndDate = temp;
            _activityFeedQuery.MaxQuerySize = 20;

            //get top 20
            returnItems = returnItems.Reverse<FeedItem>().Take(20).Reverse().ToList();

            // add senders to items
            using (SqlConnection sqlc = DBHelper.GetNewConnection())
            {
                foreach (FeedItem f in returnItems)
                {
                    bool isSelf = ActiveCourseUser.UserProfileID == f.Event.SenderId ? true : false;
                    bool isObserver = ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Observer ? true : false;
                    if (f.Event.IsAnonymous || (isObserver && !isSelf))
                    {
                        //make anon userprofile
                        UserProfile anonUserProfile = new UserProfile();
                        anonUserProfile.ID = f.Event.EventId;
                        anonUserProfile.FirstName = "Anonymous";
                        anonUserProfile.LastName = f.Event.EventId.ToString();

                        f.Event.Sender = anonUserProfile;

                        f.Event.SenderId = 0;
                        f.Event.ShowProfilePicture = false;
                    }
                    else
                    {
                        f.Event.Sender = DBHelper.GetUserProfile(f.Event.SenderId, sqlc);
                    }
                }
            }

            // retrieve our list of feed items
            int maxIdQuery = int.MaxValue;

            //Get the Resolved post IDs and display them as [Resolved]
            var allResolvedPostIds = DBHelper.GetResolvedPostIds();

            //Intersect the Resolved post IDs & the top 20 return items ids
            var topResolvedPostIds = allResolvedPostIds.Intersect(returnItems.Select(item => item.Event.EventLogId));

            //Store in the ViewBag so the post ids can be accessed in FeedItems.cshtml
            ViewBag.ResolvedPostIds = topResolvedPostIds;

            foreach (FeedItem f in returnItems)
            {
                if (f.Event.EventId < maxIdQuery)
                    maxIdQuery = f.Event.EventId;

                //The intersected query contains the feed item
                if (topResolvedPostIds.Contains(f.Event.EventId))
                {
                    f.IsResolved = true;
                }
                else
                {
                    f.IsResolved = false;
                }
            }

            vm.LastLogId = maxIdQuery - 1;

            // order items correctly, currently the Stored Procedure returns items in reverse order even though it orders dates by DESC
            // see GetActivityFeeds.sql or run the Stored Procedure in Sql Server Managment Studio to see output.

            List<FeedItem> feedItems = new List<FeedItem>();
            for (int i = returnItems.Count - 1; i >= 0; i--)
            {
                feedItems.Add(returnItems[i]);
            }
            //.OrderByDescending(i => i.Event.EventDate).ToList();
            List<AggregateFeedItem> aggregateFeed = AggregateFeedItem.FromFeedItems(feedItems);

            if (aggregateFeed.Count() > 0)
            {
                try
                {
                    vm.LastPollDate = aggregateFeed.Select(a => a.MostRecentOccurance).Max();
                }
                catch (Exception)
                {
                    vm.LastPollDate = DateTime.MinValue.AddDays(2);
                }
            }
            else
            {
                vm.LastPollDate = DateTime.MinValue.AddDays(2);
            }

            vm.Feed = aggregateFeed;
            vm.EventFilterOptions = ActivityFeedQuery.GetNecessaryEvents().OrderBy(e => e.ToString()).ToList();
            vm.UserEventFilterOptions = query.ActiveEvents;

            ////build possible courses and user types
            vm.CourseRoles.Add(CourseRole.CourseRoles.Student);
            vm.CourseRoles.Add(CourseRole.CourseRoles.TA);
            vm.CourseRoles.Add(CourseRole.CourseRoles.Instructor);
            vm.Keyword = query.CommentFilter;
            return vm;
        }

        /// <summary>
        /// Returns just the feed part of the activity feed, without the forms at the top for posting/filtering.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public JsonResult GetFeed(string keywords = null, string events = null)
        {
            // if all events unchecked, return empty feed
            if (events == "")
            {
                // update cookie to contain no items in the feed
                ActivityFeedQuery.FilterCookies(new List<EventType>());
                FeedViewModel emptyVM = new FeedViewModel()
                {
                    Feed = new List<AggregateFeedItem>()
                };
                return GetJsonFromViewModel(emptyVM);
            }

            // Set filters
            if (!string.IsNullOrWhiteSpace(keywords))
                _activityFeedQuery.CommentFilter = keywords;

            if (events != null)
            {
                var eventList = events.Replace(" ", "").Split(',').Where(s => s != "");
                var listOfEvents = eventList.Select(s => (EventType)int.Parse(s)).ToList();

                // update cookies if someone changed their list of selected events
                ActivityFeedQuery.FilterCookies(listOfEvents);

                _activityFeedQuery.UpdateEventSelectors(listOfEvents);
            }

            // Build the model and convert it to JSON to send to the view
            FeedViewModel vm = GetFeedViewModel();
            return GetJsonFromViewModel(vm);
        }

        [HttpPost]
        public JsonResult GetProfileFeed(int profileUserId)
        {
            return Json(1);
        }

        /// <returns> Returns Active Course User's course user role as a string. </returns>
        /// courtney-snyder
        [HttpPost]
        public string GetUserRole()
        {
            if (ActiveCourseUser == null)
            {
                return "None";
            }
            switch (ActiveCourseUser.AbstractRoleID)
            {
                case (int)CourseRole.CourseRoles.Instructor:
                    return "Instructor";
                case (int)CourseRole.CourseRoles.Moderator:
                    return "Moderator";
                case (int)CourseRole.CourseRoles.Observer:
                    return "Observer";
                case (int)CourseRole.CourseRoles.Pending:
                    return "Pending";
                case (int)CourseRole.CourseRoles.Student:
                    return "Student";
                case (int)CourseRole.CourseRoles.TA:
                    return "TA";
                case (int)CourseRole.CourseRoles.Withdrawn:
                    return "Withdrawn";
                default:
                    return "None";
            }
        }

        [HttpPost]
        public int GetUserId()
        {
            //If the user is not enrolled in any courses or is only Withdrawn from courses, ActiveCourseUser = 0
            return ActiveCourseUser != null ? ActiveCourseUser.UserProfileID : 0;
        }

        private string GetDisplayTimeString(DateTime time, CourseUser courseUser = null)
        {
            if (courseUser != null)
            {
                return time.UTCToCourse(courseUser.AbstractCourseID).ToShortDateString() + " " + time.UTCToCourse(courseUser.AbstractCourseID).ToShortTimeString();
            }
            return time.UTCToCourse(ActiveCourseUser.AbstractCourseID).ToShortDateString() + " " + time.UTCToCourse(ActiveCourseUser.AbstractCourseID).ToShortTimeString();
        }

        private object MakeLogCommentJsonObject(LogCommentEvent comment, Dictionary<int, bool> commentMarkedDictionary, SqlConnection sql = null)
        {
            comment.SetPrivileges(ActiveCourseUser, (ActivityEvent)comment);
            comment.NumberHelpfulMarks = DBHelper.GetHelpfulMarksLogIds(comment.EventLogId, sql).Count;
            return new
            {
                EventId = comment.EventLogId,
                ParentEventId = comment.SourceEventLogId,
                SenderName = comment.DisplayTitle,
                SenderId = comment.SenderId,
                TimeString = GetDisplayTimeString(comment.EventDate),
                EventDate = comment.EventDate.Ticks,
                EventLogId = comment.EventLogId,
                CanMail = comment.CanMail,
                HideMail = comment.HideMail,
                EventVisibilityGroups = comment.EventVisibilityGroups,
                CanEdit = comment.CanEdit,
                CanDelete = comment.CanDelete,
                CanVote = comment.CanVote,
                CanReply = false,
                IsHelpfulMark = false,
                HighlightMark = commentMarkedDictionary[comment.EventId],//DBHelper.UserMarkedLog(ActiveCourseUser.UserProfileID, comment.EventLogId, sql),
                ShowPicture = comment.ShowProfilePicture,
                Comments = new List<dynamic>(),
                HTMLContent = PartialView("Details/_LogCommentEvent", comment).Capture(this.ControllerContext),
                //Content = comment.Content,
                IdString = comment.EventId.ToString(),
                NumberHelpfulMarks = comment.NumberHelpfulMarks,
                ActiveCourseUserId = ActiveCourseUser.UserProfileID,
                EventVisibleTo = comment.EventVisibleTo,
                Role = DBHelper.GetRoleNameFromCourseAndUserProfileId(ActiveCourseUser.AbstractCourseID, comment.SenderId),
            };
        }

        public object MakeAggregateFeedItemJsonObject(AggregateFeedItem item, bool details, int userProfileId = 0, int courseId = 0)
        {
            CourseUser courseUser = null;
            if (userProfileId != 0 && courseId != 0)
            {
                courseUser = DBHelper.GetCourseUserFromProfileAndCourse(userProfileId, courseId);
            }

            var eventLog = item.Items[0].Event;
            eventLog.SetPrivileges(ActiveCourseUser ?? courseUser, (ActivityEvent)eventLog);

            var comments = MakeCommentListJsonObject(item.Comments, eventLog.EventLogId, ActiveCourseUser != null ? ActiveCourseUser.UserProfileID : userProfileId);
            string viewFolder = details ? "Details/_" : "Feed/_";
            string idString = null;

            if (eventLog.EventType == EventType.HelpfulMarkGivenEvent)
            {
                // need to change the detailsId to the Feed details for a HelpfulMarkGivenEvent
                idString = DBHelper.GetHelpfulMarkFeedSourceId(eventLog.EventLogId).ToString();
            }

            string htmlContent = PartialView(viewFolder + eventLog.EventType.ToString().Replace(" ", ""), item).Capture(this.ControllerContext);

            if (String.IsNullOrEmpty(htmlContent))
            {
                try
                {
                    htmlContent = BuildEventHtmlContent(eventLog);
                }
                catch (Exception)
                {
                    htmlContent = "Error loading content. Please refresh the page.";
                }
            }

            if (eventLog.IsAnonymous != null ? eventLog.IsAnonymous : false)
            {
                eventLog.DisplayTitle = "Anonymous " + eventLog.EventId.ToString();
            }

            if (idString == null)
            {
                idString = string.Join(",", item.Items.Select(i => i.Event.EventLogId));

                if (idString == null) //for some reason the above does not always work
                {
                    idString = item.Items.First().Event.EventLogId.ToString();
                }
            }

            return new
            {
                EventId = eventLog.EventLogId,
                ParentEventId = -1,
                SenderName = eventLog.DisplayTitle,
                SenderId = item.Creator.ID,
                TimeString = GetDisplayTimeString(item.MostRecentOccurance, courseUser),
                EventDate = item.MostRecentOccurance.Ticks,
                CanMail = eventLog.CanMail,
                HideMail = eventLog.HideMail,
                EventVisibilityGroups = eventLog.EventVisibilityGroups,
                CanEdit = eventLog.CanEdit,
                CanDelete = eventLog.CanDelete,
                CanReply = eventLog.CanReply,
                IsHelpfulMark = item.PrettyName == EventType.HelpfulMarkGivenEvent.ToString().ToDisplayText(),
                IsResolved = item.Items[0].IsResolved,
                HighlightMark = false,
                ShowPicture = eventLog.ShowProfilePicture,
                Comments = comments,
                HTMLContent = htmlContent,
                //Content = eventLog.EventType == EventType.FeedPostEvent ? (eventLog as FeedPostEvent).Comment : "",
                IdString = idString,
                ActiveCourseUserId = ActiveCourseUser != null ? ActiveCourseUser.UserProfileID : userProfileId,
                EventVisibleTo = eventLog.EventVisibleTo,
                EventType = eventLog.EventType.ToString(),
                IsAnonymous = eventLog.IsAnonymous,
                Role = DBHelper.GetRoleNameFromCourseAndUserProfileId(ActiveCourseUser != null ? ActiveCourseUser.AbstractCourseID : courseId, eventLog.SenderId),
            };
        }

        private string BuildEventHtmlContent(IActivityEvent eventLog)
        {
            string htmlContent = "";
            if (eventLog.EventType == EventType.AskForHelpEvent)
            {
                StringWriter stringWriter = new StringWriter();
                using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
                {
                    // Write a DIV with encoded text.
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "non-user-text");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderBeginTag(HtmlTextWriterTag.Em);

                    if (eventLog.IsAnonymous)
                    {
                        writer.WriteEncodedText("Anonymous");
                    }
                    else
                    {
                        writer.WriteEncodedText(DBHelper.GetUserFirstNameFromEventLogId(eventLog.EventLogId));
                    }

                    writer.RenderEndTag();
                    writer.WriteEncodedText(" asked the following question: ");
                    writer.RenderEndTag();
                    writer.WriteBreak();
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "user-text");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.WriteEncodedText(((AskForHelpEvent)(eventLog)).UserComment);
                    writer.RenderEndTag();
                    writer.WriteBreak();
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "non-user-text");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.WriteEncodedText("Click the ");
                    writer.RenderBeginTag(HtmlTextWriterTag.Em);
                    writer.WriteEncodedText("\"Details\"");
                    writer.RenderEndTag();
                    writer.WriteEncodedText(" link below to see code details.");
                    writer.RenderEndTag();
                    writer.WriteBreak();
                    htmlContent = stringWriter.ToString();
                }
            }
            else if (eventLog.EventType == EventType.SubmitEvent)
            {
                StringWriter stringWriter = new StringWriter();
                using (HtmlTextWriter writer = new HtmlTextWriter(stringWriter))
                {
                    // Write a DIV with encoded text.
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "non-user-text");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    writer.RenderBeginTag(HtmlTextWriterTag.Em);
                    writer.WriteEncodedText(DBHelper.GetUserFirstNameFromEventLogId(eventLog.EventLogId));
                    writer.RenderEndTag();
                    writer.WriteEncodedText(" submitted ");

                    writer.AddAttribute(HtmlTextWriterAttribute.Href, "\\AssignmentDetails\\" + ((SubmitEvent)eventLog).AssignmentId.ToString());
                    writer.RenderBeginTag(HtmlTextWriterTag.A);
                    string output = DBHelper.GetAssignmentName(((SubmitEvent)eventLog).AssignmentId) + ": ";
                    writer.Write(output);
                    writer.RenderEndTag(); //A

                    writer.RenderEndTag();
                    writer.AddAttribute(HtmlTextWriterAttribute.Class, "non-user-text");
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                    output = "\"" + eventLog.SolutionName.Split('\\').Last().Split('.').First() + "\"";
                    writer.WriteEncodedText(output);
                    writer.RenderEndTag();
                    writer.WriteBreak();
                    htmlContent = stringWriter.ToString();
                }
            }
            else
            {
                htmlContent = ((FeedPostEvent)(eventLog)).Comment;
            }
            return htmlContent;
        }

        private object MakeCommentListJsonObject(IEnumerable<LogCommentEvent> comments, int parentLogID, int userProfileId = 0)
        {
            var obj = new List<dynamic>();

            // get all the eventLogIds for the comments passed in
            List<int> commentIds = comments.Select(comment => comment.EventLogId).ToList();

            //If the current user is tagged, let them see their own tag
            //bool isTagged = commentIds.Contains(ActiveCourseUser.UserProfileID) ? true : false;

            using (SqlConnection sql = DBHelper.GetNewConnection())
            {
                Dictionary<int, bool> logCommentMarkedByCurrentUser =
                DBHelper.DictionaryOfMarkedLogs(ActiveCourseUser == null ? userProfileId : ActiveCourseUser.UserProfileID, commentIds, sql);

                foreach (LogCommentEvent e in comments)
                {
                    //If the Active User is an Observer AND it wasn't their comment, hide the sender's identity
                    bool isSelf = ActiveCourseUser.UserProfileID == e.SenderId ? true : false;
                    bool isObserver = ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Observer;
                    if (isObserver && !isSelf)
                    {
                        e.Sender.FirstName = "Anonymous ";
                        e.Sender.LastName = e.EventId.ToString();
                        e.SenderId = 0;
                    }
                    obj.Add(MakeLogCommentJsonObject(e, logCommentMarkedByCurrentUser, sql));
                }
            }

            return obj;
        }

        public JsonResult GetJsonFromViewModel(FeedViewModel vm)
        {
            var obj = new { Feed = new List<dynamic>(), HasLastPost = vm.Feed.Count < _activityFeedQuery.MaxQuerySize };
            foreach (AggregateFeedItem item in vm.Feed)
            {
                obj.Feed.Add(MakeAggregateFeedItemJsonObject(item, false));
            }
            //return Json(obj, JsonRequestBehavior.AllowGet);
            return Json(obj, null, null, JsonRequestBehavior.AllowGet);
        }

        protected override JsonResult Json(object data, string contentType, System.Text.Encoding contentEncoding, JsonRequestBehavior behavior)
        {
            return new JsonResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = behavior,
                MaxJsonLength = Int32.MaxValue
            };
        }

        private JsonResult GetJsonFromViewModel(FeedDetailsViewModel vm)
        {
            var obj = new { Item = MakeAggregateFeedItemJsonObject(vm.FeedItem, true) };
            return Json(obj, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Returns a raw feed without any extra HTML chrome.  Used for AJAX updates to an existing feed.
        /// </summary>
        /// <param name="id">The ID of the last feed item received by the client</param>
        /// <returns></returns>
        public ActionResult RecentFeedItems(int id, int userId = -1, string keyword = "",
            int hash = 0)
        {
            //return View("AjaxFeed", new List<AggregateFeedItem>()); 

            var query = _activityFeedQuery;
            query.CommentFilter = hash == 0 ? keyword : "#" + keyword;
            BuildBasicQuery(ref query);
            query.MinLogId = id;
            query.MaxQuerySize = 10;

            //used to build a feed for a single person.  Useful for building profile-based feeds
            if (userId > 0)
            {
                query.ClearSubscriptionSubjects();
                query.AddSubscriptionSubject(db.UserProfiles.Where(u => u.ID == userId).FirstOrDefault());
            }
            List<FeedItem> feedItems = query.Execute().ToList();
            List<AggregateFeedItem> aggregateFeed = AggregateFeedItem.FromFeedItems(feedItems);

            //build the "you and 5 others got this error"-type messages
            FeedViewModel vm = new FeedViewModel();

            ViewBag.RecentUserErrors = vm.RecentUserErrors;
            //ViewBag.RecentClassErrors = vm.RecentClassErrors;
            //ViewBag.ErrorTypes = vm.ErrorTypes;

            return View("AjaxFeed", aggregateFeed);
        }

        /// <summary>
        /// Removes a FeedPostEvent, AJAX-style!
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult DeleteFeedPost(int id)
        {
            //we need to make sure the user is either a grader (TA/Instructor) or they are deleting their own post!
            ActivityEvent feedPostEvent = DBHelper.GetActivityEvent(id);

            if ((feedPostEvent.SenderId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
            {
                DBHelper.DeleteFeedPostEvent(id);
            }
            else
            {
                Response.StatusCode = 403;
            }

            return View("_AjaxEmpty");
        }

        /// <summary>
        /// Removes a Reply, AJAX-style!
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult DeleteLogComment(int id)
        {
            //we need to make sure the user is either a grader (TA/Instructor) or they are deleting their own reply!
            ActivityEvent logCommentEvent = DBHelper.GetActivityEvent(id);

            if ((logCommentEvent.SenderId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
            {
                DBHelper.DeleteLogComment(id);
            }
            else
            {
                Response.StatusCode = 403;
            }

            return View("_AjaxEmpty");
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult EditFeedPost(int id, string newText, bool details = false)
        {
            //we need to make sure the user is either a grader (TA/Instructor) or they are deleting their own post!
            ActivityEvent feedPostEvent = DBHelper.GetActivityEvent(id);

            if ((feedPostEvent.SenderId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
            {
                using (SqlConnection conn = DBHelper.GetNewConnection())
                {
                    DBHelper.EditFeedPost(id, newText, conn);
                    AggregateFeedItem item = new AggregateFeedItem(Feeds.Get(id));
                    string html;
                    if (details)
                        html = PartialView("Details/_FeedPostEvent", item).Capture(this.ControllerContext);
                    else
                        html = PartialView("Feed/_FeedPostEvent", item).Capture(this.ControllerContext);
                    return Json(new
                    {
                        HTMLContent = html,
                        TimeString = GetDisplayTimeString(item.MostRecentOccurance)
                    });
                }
            }
            else
            {
                Response.StatusCode = 403;
                throw new Exception();
            }
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult EditLogComment(int id, string newText)
        {
            // do checking, make sure non-authorized users cannot edit posts

            //we need to make sure the user is either a grader (TA/Instructor) or they are deleting their own reply!
            ActivityEvent logCommentEvent = DBHelper.GetActivityEvent(id);

            if ((logCommentEvent.SenderId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
            {
                using (SqlConnection conn = DBHelper.GetNewConnection())
                {
                    DBHelper.EditLogComment(id, newText, conn);
                    LogCommentEvent c = DBHelper.GetSingularLogComment(id, conn);
                    return Json(new
                    {
                        HTMLContent = PartialView("Details/_LogCommentEvent", c).Capture(this.ControllerContext),
                        TimeString = GetDisplayTimeString(c.EventDate)
                    });
                }

            }
            else
            {
                Response.StatusCode = 403;
                throw new Exception();
            }
        }

        /// <summary>
        /// Allows other users to mark replies to the original post as "Helpful"
        /// </summary>
        /// <param name="eventLogToMark"></param>
        /// <param name="markerId"></param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult MarkHelpfulComment(int eventLogToMark, int markerId)
        {
            int logToMarkSenderId = DBHelper.GetActivityEvent(eventLogToMark).SenderId;

            if (ActiveCourseUser != null && logToMarkSenderId == markerId)
            {
                markerId = ActiveCourseUser.UserProfileID;
            }

            using (SqlConnection sqlc = DBHelper.GetNewConnection())
            {
                int helpfulMarks = DBHelper.MarkLogCommentHelpful(eventLogToMark, markerId, sqlc);
                bool isMarker = DBHelper.UserMarkedLog(markerId, eventLogToMark, sqlc); // markerId is the CU
                return Json(new
                {
                    helpfulMarks,
                    isMarker
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Allows other users to like the actual feed item (not including replies)
        /// </summary>
        /// <param name="eventLogId"> Id of the feed post being liked </param>
        /// <param name="senderId"> Id of the person "liking" the feed post </param>
        /// <returns></returns>
        /// courtney-snyder
        [HttpGet]
        public JsonResult LikeFeedPost (int eventLogId, int senderId)
        {
            int logToMarkSenderId = DBHelper.GetActivityEvent(eventLogId).SenderId;

            if (ActiveCourseUser != null && logToMarkSenderId == senderId)
            {
                senderId = ActiveCourseUser.UserProfileID;
            }

            using (SqlConnection sqlc = DBHelper.GetNewConnection())
            {
                int helpfulMarks = DBHelper.GetPostLikeCount(eventLogId);
                bool isMarker = DBHelper.UserMarkedLog(senderId, eventLogId, sqlc); // markerId is the CU
                return Json(new
                {
                    helpfulMarks,
                    isMarker
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Updates the FeedPostEventFlags Table, MarkedResolved column.
        /// </summary>
        /// <param name="eventLogToMark"></param>
        /// <param name="markerId"></param>
        /// <returns></returns>
        /// courtney-snyder
        [HttpGet]
        public void MarkResolvedPost(int eventLogToMark, bool isResolved, int markerId)
        {
            int logToMarkSenderId = DBHelper.GetActivityEvent(eventLogToMark).SenderId;

            if (ActiveCourseUser != null && logToMarkSenderId == markerId)
            {
                markerId = ActiveCourseUser.UserProfileID;
            }
            
            //Update the db
            using (SqlConnection sqlc = DBHelper.GetNewConnection())
            {
                DBHelper.MarkFeedPostResolved(eventLogToMark, isResolved);
            }
        }

        /// <summary>
        /// Calls the DBHelper method "IsPostResolved" and returns the bool result to the AJAX call in a JSON object
        /// because you cannot return normal booleans for some reason.
        /// </summary>
        /// <param name="eventId"></param>
        /// <returns> JSON Object containing boolean result </returns>
        /// courtney-snyder
        [HttpGet]
        public JsonResult IsPostResolved(int eventId)
        {
            return Json(new { boolResult = DBHelper.IsPostResolved(eventId) }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Calls the DBHelper method "GetPostLikeCount" and returns it to the AJAX call.
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="senderId"></param>
        /// <returns> JSON Object containing number of likes </returns>
        /// courtney-snyder
        [HttpGet]
        public JsonResult GetPostLikeCount (int eventId)
        {
            return Json(new { numberOfLikes = DBHelper.GetPostLikeCount(eventId) }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Calls the DBHelper method "GetPostLikeCount" and returns it to the AJAX call.
        /// </summary>
        /// <param name="eventIds"> A list of visible feed item IDs </param>
        /// <returns> JSON Object containing a dictionary with eventIDs (key) and number of likes (value) </returns>
        /// courtney-snyder
        [HttpGet]
        public JsonResult GetPostLikeCounts(string eventIds)
        {
            List<int> eventIdInts = new List<int>();
            //Get the eventIds as a string
            string eventIdsString = Convert.ToString(eventIds);
            //Remove [ and ] from string
            eventIdsString = eventIdsString.Substring(1, eventIdsString.Length-2);
            //Split the IDs up
            var splitEventIds = eventIdsString.Split(',');
            //Convert IDs from strings to ints
            foreach (var s in splitEventIds)
            {
                int temp = Convert.ToInt32(s);
                eventIdInts.Add(temp);
            }

            var likeDictionary = DBHelper.GetPostLikeCount(eventIdInts);
            string likeString = string.Join(",", likeDictionary.Select(m => m.Key + ":" + m.Value).ToArray());

            return Json( new { likeString }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Checks the DB to see if that user liked the post
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="senderId"></param>
        /// <returns>JSON Object containing thte boolean result </returns>
        /// courtney-snyder
        [HttpGet]
        public JsonResult IsPostLikedByUser (int eventId, int senderId)
        {
            return Json(new { boolResult = DBHelper.IsPostLikedByUser(eventId, senderId) }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Checks the DB to see if that user liked any of the visible posts.
        /// </summary>
        /// <param name="eventIds"> A list of visible feed item ids. </param>
        /// <param name="senderId"> The current viewer/user. </param>
        /// <returns> A list of visible posts that the user has liked. </returns>
        /// courtney-snyder
        [HttpGet]
        public JsonResult ArePostsLikedByUser (string eventIds, int senderId)
        {
            List<int> eventIdInts = new List<int>();
            //Get the eventIds as a string
            string eventIdsString = Convert.ToString(eventIds);
            //Remove [ and ] from string
            eventIdsString = eventIdsString.Substring(1, eventIdsString.Length - 2);
            //Split the IDs up
            var splitEventIds = eventIdsString.Split(',');
            //Convert IDs from strings to ints
            foreach (var s in splitEventIds)
            {
                int temp = Convert.ToInt32(s);
                eventIdInts.Add(temp);
            }
            var likeList = DBHelper.ArePostsLikedByUser(eventIdInts, senderId);
            //string likeString = string.Join(",", likeDictionary.Select(m => m.Key + ":" + m.Value).ToArray());
            string likeString = string.Join(",", likeList);

            return Json(new { likeString }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Inserts new row into FeedPostLikes db.
        /// </summary>
        /// <param name="eventId"> Item to add like to/remove like from </param>
        /// <param name="senderId"> Person liking/unliking that item </param>
        /// courtney-snyder
        [HttpGet]
        public void UpdatePostItemLikeCount(int eventId, int senderId)
        {
            DBHelper.UpdatePostItemLikeCount(eventId, senderId);
        }

        /// <summary>
        /// Gets all Resolved post IDs.
        /// </summary>
        /// <returns> Json result containing the resolved post IDs. </returns>
        /// courtney-snyder
        [HttpGet]
        public JsonResult GetResolvedPostIds()
        {
            return Json(new { idList = DBHelper.GetResolvedPostIds() }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Gets all Resolved post IDs and their senders.
        /// </summary>
        /// <returns> Json result containing a dictionary of post IDs (key) and corresponding senders (value). </returns>
        /// courtey-snyder
        [HttpGet]
        public JsonResult GetResolvedPostIdsAndSenderIds()
        {
            return Json(new { idDict = DBHelper.GetResolvedPostIdsAndSenderIds() }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Gets all Resolved post IDs from a list of post IDs.
        /// </summary>
        /// <param name="viewablePostIds"> A list of post items the user can see on the page. </param>
        /// <returns> Json result containing the resolved post IDs in the list of given post IDs. </returns>
        /// courtney-snyder
        [HttpGet]
        public JsonResult GetSelectedResolvedPostIds(string viewablePostIds)
        {
            //Remove [ and ]
            viewablePostIds = viewablePostIds.Substring(1, viewablePostIds.Length - 2);
            var splitPostIds = viewablePostIds.Split(',');
            List<int> postEventIdInts = new List<int>();
            //Parse Event Id strings
            foreach (var s in splitPostIds)
            {
                int temp = Convert.ToInt32(s);
                postEventIdInts.Add(temp);
            }
            return Json(new { idList = DBHelper.GetResolvedPostIds(postEventIdInts) }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Gets all Resolved post IDs and their senders from a list of post IDs. Used in ActivityFeed.js
        /// </summary>
        /// <param name="viewablePostIds"> A list of post items the user can see on the page. </param>
        /// <returns> Json result containing the resolved post IDs in the list of given post IDs. </returns>
        /// courtney-snyder
        [HttpGet]
        public JsonResult GetSelectedResolvedPostIdsAndSenderIds(string viewablePostIds)
        {
            //Remove [ and ]
            viewablePostIds = viewablePostIds.Substring(1, viewablePostIds.Length - 2);
            var splitPostIds = viewablePostIds.Split(',');
            List<int> postEventIdInts = new List<int>();
            //Parse Event Id strings
            foreach (var s in splitPostIds)
            {
                int temp = Convert.ToInt32(s);
                postEventIdInts.Add(temp);
            }
            var resolvedDictionary = DBHelper.GetResolvedPostIdsAndSenderIds(postEventIdInts);
            //Because javascript seems to have issues with JSON objects containing dictionaries
            string resolvedString = string.Join(",", resolvedDictionary.Select(m => m.Key + ":" + m.Value).ToArray());

            return Json(new { resolvedString }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetFeedItemSenderId(int eventID)
        {
            int senderId = DBHelper.GetFeedItemSenderId(eventID);
            return Json(new { senderId }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Used for the first pull on a log comment to see if we have already marked the item
        /// </summary>
        /// <param name="currentUserId"></param>
        /// <param name="logCommentId"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult FindMarker(int currentUserId, int logCommentId)
        {
            return Json(new
            {
                value = DBHelper.UserMarkedLog(currentUserId, logCommentId)
            });
        }

        /*public JsonResult GetComments(int? singleLogId)
        {
            //turned off for now
            //return this.Json(new { Data = new{} }, JsonRequestBehavior.AllowGet);

            try
            {
                List<int> logIds = new List<int>();

                if (!string.IsNullOrWhiteSpace(Request["logIds"]))
                {
                    logIds = Newtonsoft.Json.JsonConvert.DeserializeObject<List<int>>(Request.Form["logIds"]);
                }
                else
                {
                    logIds = new List<int>();
                }

                //legacy code will send a single log Id.  In that case, add it to the list of log ids
                if (singleLogId != null)
                {
                    logIds.Add((int) singleLogId);
                }

                // needs to use FeedController.Get()
                //var allcomments = CommentsProc.Get(string.Join(",", logIds), CurrentUser.ID).OrderBy(c => c.EventDate);
                string logs = string.Join(",", logIds);

                List<LogCommentEvent> comments;
                using (SqlConnection conn = DBHelper.GetNewConnection())
                {
                    // need to get source user id, should all be the same user, use first item in logIds
                    int uid = conn.Query<int>("FROM EventLogs e " +
                                              "WHERE e.Id = @logId " +
                                              "SELECT e.SenderId", new {logId = logIds[0]}).FirstOrDefault();

                    // get all feed items
                    EventType e = conn.Query<EventType>("FROM EventTypes e " +
                                                        "WHERE e.EventTypeName = 'LogCommentEvent' " +
                                                        "SELECT e").SingleOrDefault();
                    var query = new OSBLEPlus.Services.Controllers.FeedController().Get(
                        new DateTime(2010, 01, 01),
                        DateTime.UtcNow,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        10000
                        );

                    List<FeedItem> items = query.GetAwaiter().GetResult().ToList();
                    //comments = conn.Query<LogCommentEvent>();
                    //return new JsonResult();
                }

                //for each log Id, build the appropriate comments view model
                Dictionary<int, List<CommentsViewModel>> viewModels = new Dictionary<int, List<CommentsViewModel>>();
                List<object> jsonVm = new List<object>();

                return Json(new { Data = jsonVm }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                //LogErrorMessage(ex);
                return Json(new {Data = "An error occurred duing data processing."}, JsonRequestBehavior.AllowGet);
            }
        }*/

        [HttpPost]
        public JsonResult GetMorePosts(long endDate, string keywords = null, string events = null)
        {
            /****************************************************************************/
            //don't forget about filters!
            // Set filters
            if (!string.IsNullOrWhiteSpace(keywords))
                _activityFeedQuery.CommentFilter = keywords;

            if (events != null)
            {
                var eventList = events.Replace(" ", "").Split(',').Where(s => s != "");
                var listOfEvents = eventList.Select(s => (EventType)int.Parse(s)).ToList();

                // update cookies if someone changed their list of selected events
                ActivityFeedQuery.FilterCookies(listOfEvents);

                _activityFeedQuery.UpdateEventSelectors(listOfEvents);
            }
            /****************************************************************************/

            // store the current End date so we can restore it later
            DateTime temp = _activityFeedQuery.EndDate;

            // Get the items we can that match the query and are older 
            // than the ones currently loaded
            DateTime end = new DateTime(endDate);
            _activityFeedQuery.EndDate = end;

            // Get one more post since the first one will already be
            // in the feed.
            _activityFeedQuery.MaxQuerySize++;

            FeedViewModel vm = GetFeedViewModel();

            // restore the original end date and query size
            _activityFeedQuery.EndDate = temp;
            _activityFeedQuery.MaxQuerySize--;

            // remove the first item, since it's already in the feed (NOTE; only seems to happen when we are serving the last post, doing so before then loses 1 post on the feed)            
            if (vm.Feed.Count > 0)
            {
                bool hasLastPost = vm.Feed.Count < _activityFeedQuery.MaxQuerySize;
                if (hasLastPost)
                {
                    vm.Feed.RemoveAt(0);
                }
            }
            return GetJsonFromViewModel(vm);
        }

        /// <summary>
        /// Returns a raw feed of past feed items without any extra HTML chrome.  Used for AJAX updates to an existing feed.
        /// </summary>
        /// <param name="id">The ID of the first feed item received by the client.</param>
        /// <returns></returns>
        public ActionResult OldFeedItems(int id, int count, int userId, int errorType = -1, string keyword = "", int hash = 0)
        {
            try
            {
                var query = _activityFeedQuery;
                query.CommentFilter = hash == 0 ? keyword : "#" + keyword;
                if (errorType > 0)
                {
                    //query = new BuildErrorQuery(Db);
                    //(query as BuildErrorQuery).BuildErrorTypeId = errorType;
                }
                BuildBasicQuery(ref query);
                query.MaxLogId = id - 1;
                query.MaxQuerySize = count;

                //used to build a feed for a single person.  Useful for building profile-based feeds
                if (userId > 0)
                {
                    query.ClearSubscriptionSubjects();
                    //query.AddSubscriptionSubject(Db.Users.Where(u => u.Id == userId).FirstOrDefault());
                }

                List<FeedItem> returnItems = query.Execute().ToList();
                List<FeedItem> feedItems = returnItems.OrderByDescending(i => i.Event.EventDate).ToList();

                List<AggregateFeedItem> aggregateFeed = AggregateFeedItem.FromFeedItems(feedItems);


                //build the "you and 5 others got this error"-type messages
                FeedViewModel vm = new FeedViewModel();

                ViewBag.RecentUserErrors = vm.RecentUserErrors;
                //ViewBag.RecentClassErrors = vm.RecentClassErrors;
                //ViewBag.ErrorTypes = vm.ErrorTypes;

                return View("AjaxFeed", aggregateFeed);
            }
            catch (Exception ex)
            {
                //LogErrorMessage(ex);
                //return RedirectToAction("FeedDown", "Error");
            }
            return View("Index");
        }

        /// <summary>
        /// Provides a details view for the provided Log IDs
        /// </summary>
        /// <param name="id">The ID(s) of the logs to retrieve.  Accepts a comma delimited list.  
        /// In the case of rendering multiple IDs, an aggregate view will be created
        /// </param>
        /// <returns></returns>
        public ActionResult Details(string id)
        {
            //make sure that we've gotten a valid ID
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("Index", "Home");
            }

            // Verify that none of the event logs have been deleted
            string[] ids = id.Split(',');
            int idNum = 0;
            foreach (string s in ids)
            {
                if (!int.TryParse(s, out idNum) || DBHelper.IsEventDeleted(idNum))
                {
                    ViewBag.ErrorName = "Item not found";
                    ViewBag.ErrorMessage = "One or more of the posts you are looking for has been deleted or is otherwise not available.";
                    return View("Error");
                }
            }

            if (ActiveCourseUser.AbstractRole.CanGrade)
                ViewBag.CanGrade = true;
            else
                ViewBag.CanGrade = false;

            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                ViewBag.IsInstructor = true;
            else
                ViewBag.IsInstructor = false;

            ViewBag.EnableCustomPostVisibility = ConfigurationManager.AppSettings["EnableCustomPostVisibility"]; //<add key="EnableCustomPostVisibility" value="false"/> in web.config

            //setup user list for autocomplete            
            var viewableProfiles = DBHelper.GetUserProfilesForCourse(ActiveCourseUser.AbstractCourseID);
            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Observer) //If the current user is an Observer, remove everyone besides them and instructors from viewableProfiles
            {
                bool isSelf;
                bool isInstructor;
                var instructors = DBHelper.GetCourseInstructorIds(ActiveCourseUser.AbstractCourseID);
                var tempViewableProfiles = new List<UserProfile>(viewableProfiles); //Need a deep copy of the list to iterate over so elements can be removed if necessary
                foreach (var profile in tempViewableProfiles)
                {
                    isSelf = profile.ID == ActiveCourseUser.UserProfileID ? true : false;
                    isInstructor = instructors.Contains(profile.ID) ? true : false;
                    if (!isSelf && !isInstructor)
                    {
                        viewableProfiles.Remove(profile);
                    }
                }
            }
            ViewBag.CurrentCourseUsers = viewableProfiles;
            ViewBag.HashTags = DBHelper.GetHashTags();

            ViewBag.RootId = id;
            return View();
        }

        [OsbleAuthorize]
        public ActionResult OSBIDEDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return RedirectToAction("OSBIDE");
            }

            ViewBag.RootId = id;
            ViewBag.BackURL = "/Feed/OSBIDE";
            return View("Details", "_OSBIDELayout");
        }

        [HttpPost]
        public JsonResult GetDetails(string id)
        {
            FeedDetailsViewModel vm = GetDetailsViewModel(id);
            return GetJsonFromViewModel(vm);
        }

        private FeedDetailsViewModel GetDetailsViewModel(string id)
        {
            // Get the list of ids
            List<int> ids = ParseIdString(id);

            // Get the list of feed items (EventLogs) with that id
            List<FeedItem> feedItems = GetFeedItemsFromIDs(ids);
            var query = _activityFeedQuery;

            // Check if we were able to get the feed items
            if (feedItems.Count == 0)
            {
                ViewBag.ErrorName = "Query Error";
                throw new Exception("The query for event log details has returned no usable results.");
            }


            List<AggregateFeedItem> aggregateItems = AggregateFeedItem.FromFeedItems(feedItems);

            //build the "you and 5 others got this error"-type messages
            FeedViewModel fvm = new FeedViewModel();

            ViewBag.RecentUserErrors = fvm.RecentUserErrors;
            //ViewBag.RecentClassErrors = fvm.RecentClassErrors;
            //ViewBag.ErrorTypes = fvm.ErrorTypes;

            FeedDetailsViewModel vm = new FeedDetailsViewModel();
            vm.Ids = id;
            vm.FeedItem = aggregateItems.FirstOrDefault();
            bool isSelf = ActiveCourseUser.UserProfileID == vm.FeedItem.Creator.ID ? true : false; //If the Active User is the poster, let them see who was tagged
            bool isObserver = ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Observer;
            if ((vm.FeedItem.IsAnonymous != null ? vm.FeedItem.IsAnonymous : false) || (isObserver && !isSelf))
            {
                vm.FeedItem.Items.First().Event.SenderId = 0;
            }
            return vm;
        }

        /// <summary>
        /// Adds a global comment that will appear in the activity feed
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult PostFeedItem(string text, bool emailToClass = false, string postVisibilityGroups = "", string eventVisibleTo = "", bool notifyHub = false, bool isAnonymous = false)
        {
            // We purposefully are not catching exceptions that could be thrown
            // here, because we want this response to fail if there is an error
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException();
            }

            //the user submitted their post but did not add any users
            if (postVisibilityGroups == "Selected Users" && eventVisibleTo.Length == 1)
            {
                throw new Exception("Poster did not select any users for a custom visiblity post.");
            }

            // Parse text and add any new hashtags to database
            ParseHashTags(text);

            if (postVisibilityGroups == "class")
            {
                eventVisibleTo = ""; //we don't need to parse visibility if the group is 'class'
            }
            else
            {
                //parse visibility groups for the db
                if (postVisibilityGroups != "Selected Users")
                {
                    eventVisibleTo = ParsePostVisibilityGroups(postVisibilityGroups);
                }
            }


            int courseID = ActiveCourseUser.AbstractCourseID;
            FeedPostEvent log = new FeedPostEvent()
                {
                    SenderId = CurrentUser.ID,
                    Comment = text,
                    CourseId = courseID,
                    SolutionName = null,
                    EventVisibilityGroups = postVisibilityGroups,
                    EventVisibleTo = eventVisibleTo,
                    IsAnonymous = isAnonymous,
                };

            int logID = Posts.SaveEvent(log);
            var newPost = new AggregateFeedItem(Feeds.Get(logID));

            //get email addresses for users who want to be notified by email
            List<MailAddress> emailList = DBHelper.GetActivityFeedForwardedEmails(courseID, null, emailToClass);
            //remove email for current user if they do not want to get their own posts emailed.
            bool emailSelf = ActiveCourseUser.UserProfile.EmailSelfActivityPosts;
            if (!emailSelf)
            {
                emailList.RemoveAll(el => el.Address == ActiveCourseUser.UserProfile.Email);
            }

            //remove users who do not belong in this post visibility group
            emailList = RemoveNonVisibilityGroupEmails(emailList, eventVisibleTo);

            //Remove Observers here
            var users = DBHelper.GetAllCourseUsersFromCourseId(courseID); //Get all users
            foreach (var user in users)
            {
                //Get user's profile from ID
                var profile = DBHelper.GetUserProfile(user.ID);
                var role = DBHelper.GetRoleNameFromCourseAndUserProfileId(courseID, user.ID); //Must get role separately since role is not in profile
                bool isObserver = role == "Observer" ? true : false;
                MailAddress email = new MailAddress(profile.Email); //profile.Email is a string, must be of type MailAddress
                
                //If Course Role == Observer and email is in emailList, remove email from emailList
                if (isObserver)
                {
                    emailList.Remove(emailList.Where(el => el.Address == email.Address).FirstOrDefault());
                }
            }

            // Parse text and create list of tagged users
            NotifyTaggedUsers(text, logID, eventVisibleTo, isAnonymous);

            // Send emails to those who want to be notified by email
            SendEmailsToListeners(log.Comment, logID, courseID, DateTime.UtcNow, emailList, false, isAnonymous);

            //push message to hub listeners if needed (post source from outside of the feed e.g. intervention windows)
            if (notifyHub)
            {
                try
                {
                    using (InterventionController ic = new InterventionController())
                    {
                        string authKey = Request.Cookies["AuthKey"].Value.Split('=').Last();
                        ic.NotifyHub(logID, log.SenderId, "FeedPostEvent", log.CourseId ?? 0, authKey);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Failed trying to notify hub in FeedController PostFeedItem()", e);
                }
            }

            return Json(MakeAggregateFeedItemJsonObject(newPost, false));
        }

        private List<MailAddress> RemoveNonVisibilityGroupEmails(List<MailAddress> emailList, string eventVisibleToString)
        {
            if (eventVisibleToString == "") //visibility is everyone
            {
                return emailList;
            }

            List<string> emailAddresses = new List<string>();
            foreach (MailAddress mailAddress in emailList)
            {
                emailAddresses.Add(mailAddress.Address);
            }

            List<int> eventVisibleTo = eventVisibleToString.Split(',').Select(Int32.Parse).ToList();
            Dictionary<int, string> addressIds = DBHelper.GetMailAddressUserId(emailAddresses);
            List<MailAddress> eventVisibleMailAddresses = new List<MailAddress>();

            foreach (KeyValuePair<int, string> item in addressIds)
            {
                if (eventVisibleTo.Contains(item.Key))
                {
                    eventVisibleMailAddresses.Add(emailList.Where(el => el.Address == item.Value).FirstOrDefault());
                }
            }
            return eventVisibleMailAddresses;
        }

        private string ParsePostVisibilityGroups(string postVisibilityGroups)
        {
            if (postVisibilityGroups == "") //no need to go further
            {
                return postVisibilityGroups; //legacy: display to everyone
            }

            List<string> groups = postVisibilityGroups.Split(',').ToList();

            //case class == everyone
            if (groups.Contains("class"))
            {
                //base case: display to everyone in the class. 
                //it doesn't matter if instructor and TA are unchecked, we don't allow class but not instructor/TA
                return "";
            }
            else
            {
                int courseId = ActiveCourseUser.AbstractCourseID;
                bool containsInstructors = groups.Contains("instructors");
                bool containsTAs = groups.Contains("tas");
                bool containsSection = groups.Contains("section");
                string idList = "";

                //case instructors and tas
                if (containsInstructors && containsTAs)
                {
                    idList = String.Join(",", DBHelper.GetCourseInstructorIds(courseId));
                    string TAs = String.Join(",", DBHelper.GetCourseTAIds(courseId));
                    if (TAs != "")
                    {
                        idList = idList + "," + TAs;
                    }

                    if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor || ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
                    { } //the user is instructor or TA, they are already on the list so don't add their own ID to the list.
                    else
                        idList = idList + "," + ActiveCourseUser.UserProfileID; //the user making the post can also see the post!
                }

                //case just instructors
                if (containsInstructors && !containsTAs)
                {
                    if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                        idList = String.Join(",", DBHelper.GetCourseInstructorIds(courseId)); //the user an instructor, they are already on the list.
                    else
                        idList = String.Join(",", DBHelper.GetCourseInstructorIds(courseId)) + "," + ActiveCourseUser.UserProfileID; //the user making the post can also see the post!;
                }

                //case just tas
                if (!containsInstructors && containsTAs)
                {
                    if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.TA)
                        idList = String.Join(",", DBHelper.GetCourseTAIds(courseId)); //the user is a TA, they are already on the list.
                    else
                        idList = String.Join(",", DBHelper.GetCourseTAIds(courseId)) + "," + ActiveCourseUser.UserProfileID; //the user making the post can also see the post!
                }

                //case: section
                if (containsSection)
                {
                    idList = String.Join(",", DBHelper.GetCourseSectionUserProfileIds(courseId, ActiveCourseUser.Section));
                }

                //TODO: add support for custom groups here.

                //remove hanging chads ;)
                if (idList.Length > 0 && idList[0] == ',') //remove initial comma if we somehow ended up with one here...
                    idList = idList.Remove(0, 1);
                if (idList.Length > 0 && idList[idList.Length - 1] == ',') //remove hanging comma if we somehow ended up with one here...        
                    idList = idList.Remove(idList.Length - 1);

                return idList;
            }
            return ""; //legacy: display to everyone
        }

        [HttpGet]
        public ActionResult GetPostVisibilityAddMorePartialView()
        {

            return PartialView("_PostVisibilityAddMore");
        }

        [OsbleAuthorize]
        [HttpPost]
        public ActionResult ModifyPostVisibility(string addCustomVisibilitySelectionIdList, string eventLogId, string currentUserId = "")
        {
            //don't allow changing a visibility group to empty
            if (String.IsNullOrEmpty(addCustomVisibilitySelectionIdList))
            {
                throw new ArgumentException();
            }

            //get current visibility list
            int eventLogIdInt;
            bool logParseSuccess = int.TryParse(eventLogId, out eventLogIdInt);
            string currentVisibleList = logParseSuccess ? DBHelper.GetEventLogVisibleToList(eventLogIdInt) : "";

            int currentUserIdInt;
            bool userParseSuccess = int.TryParse(currentUserId, out currentUserIdInt);

            //check that the active user is currently a member of the post and that the active users is the same as the string id we have received
            bool IsPostMember = userParseSuccess ? (currentUserIdInt == ActiveCourseUser.UserProfileID && currentVisibleList.Contains(currentUserId)) : false;

            //make db change
            if (IsPostMember || ActiveCourseUser.AbstractRole.CanGrade)
            {
                //user can't remove themself from the post, make sure they are on the list
                List<string> userIdsString = addCustomVisibilitySelectionIdList.Split(',').ToList();
                string sanitizedIdList = "";
                bool ContainsPosterId = false;

                foreach (string id in userIdsString)
                {
                    if (id == ActiveCourseUser.UserProfileID.ToString())
                        ContainsPosterId = true;

                    if (sanitizedIdList == "")
                        sanitizedIdList = id;
                    else
                        sanitizedIdList += "," + id;
                }

                if (!ContainsPosterId)
                {
                    sanitizedIdList += "," + ActiveCourseUser.UserProfileID; //add them back to the list!
                }

                DBHelper.UpdateEventVisibleToList(Int32.Parse(eventLogId), sanitizedIdList);
            }

            return Redirect(Request.UrlReferrer.ToString()); //return them to the page they came from
        }

        /// <summary>
        /// Parses given text for tagged users in the form of "id=XXX;" and notifies them
        /// </summary>
        /// <param name="text"></param>
        private void NotifyTaggedUsers(string text, int postId, string eventVisibleToString = "", bool isAnonymous = false)
        {
            try
            {
                List<int> eventVisibleTo = new List<int>();
                if (eventVisibleToString != "")
                {
                    eventVisibleTo = eventVisibleToString.Split(',').Select(Int32.Parse).ToList();
                }

                List<int> idNumbers = new List<int>();

                for (int i = 0; i < text.Length; i++)
                {
                    // If we find an '@' character, see if it's followed by "id=" then a number then a semicolon
                    if (text[i] == '@')
                    {
                        if (text.Substring(i + 1, 3) == "id=")
                        {
                            int digit = 0, rIndex = 4;
                            bool hasDigit = false;
                            string idString = "";

                            while (Int32.TryParse(text.Substring(i + rIndex, 1), out digit)) // Keep reading characters until we hit something that isn't a digit
                            {
                                hasDigit = true;
                                idString += digit.ToString();
                                rIndex++;
                            }
                            if (hasDigit == true && text[i + rIndex] == ';')
                            {
                                idNumbers.Add(Convert.ToInt32(idString)); // If the character following the numbers is a semicolon, we know there is a name reference here so record the id
                            }
                        }
                    }
                }

                List<CourseUser> taggedUsers = new List<CourseUser>();

                foreach (int id in idNumbers)
                {
                    CourseUser user = db.CourseUsers.Where(cu => cu.UserProfileID == id && cu.AbstractCourseID == ActiveCourseUser.AbstractCourseID).FirstOrDefault();

                    if (user != null) // Skip users that don't / no longer exist
                    {
                        if (eventVisibleToString == "" || eventVisibleTo.Contains(user.UserProfileID)) //only add users in an allowed visibility group (or everyone/legacy if "")
                        {
                            taggedUsers.Add(user);
                        }
                    }
                }

                if (taggedUsers.Count > 0)
                {
                    using (NotificationController nc = new NotificationController())
                    {
                        nc.SendUserTagNotifications(ActiveCourseUser, postId, taggedUsers, isAnonymous);
                    }
                }
            }
            catch
            {
                return; // Will return without notifications in failure
            }
        }

        /// <summary>
        /// Parse given text for hashtags and add new hashtags not in the database to the database
        /// </summary>
        /// <param name="text"></param>
        public static void ParseHashTags(string text)
        {
            List<string> hashTags = new List<string>();

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '#')
                {
                    int startIndex = ++i;
                    while (i < text.Length && IsAlphaNumericChar(text[i]))
                        i++;
                    if (startIndex != i)
                        hashTags.Add(text.Substring(startIndex, i - startIndex));
                }
            }

            DBHelper.AddHashTags(hashTags);
        }

        /// <summary>
        /// Returns true if given character is alphanumberic, false if not.
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        private static bool IsAlphaNumericChar(char character) // This function probably already exists somewhere in .NET, if so just delete
        {
            if ((character >= '0' && character <= '9') || (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z'))
            {
                return true;
            }
            return false;
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult PostComment(int id, string content, string postVisibilityGroups = "", bool isAnonymous = false)
        {
            // Check for blank comment
            if (String.IsNullOrWhiteSpace(content))
            {
                throw new Exception();
            }

            // Parse text and add any new hashtags to database
            ParseHashTags(content);

            // Insert the comment
            bool success = DBHelper.InsertActivityFeedComment(id, CurrentUser.ID, content, null, isAnonymous);
            if (!success)
            {
                throw new Exception();
            }

            // Get the new comment list by getting the parent feed item
            FeedItem post = Feeds.Get(id);

            //get email addresses for users who want to be notified by email
            List<MailAddress> emailList = DBHelper.GetReplyForwardedEmails(id);

            //remove email for current user if they do not want to get their own posts emailed.
            bool emailSelf = ActiveCourseUser.UserProfile.EmailSelfActivityPosts;
            if (!emailSelf)
            {
                emailList.RemoveAll(el => el.Address == ActiveCourseUser.UserProfile.Email);
            }

            //parse visibility groups for the db
            string eventVisibleTo = ParsePostVisibilityGroups(GetPostVisibilityGroups(id));

            //remove users who do not belong in this post visibility group
            emailList = RemoveNonVisibilityGroupEmails(emailList, eventVisibleTo);

            // Send emails if neccesary
            SendEmailsToListeners(content, id, ActiveCourseUser.AbstractCourseID, DateTime.UtcNow, emailList, true, isAnonymous);

            // Notify users of tags
            NotifyTaggedUsers(content, id, eventVisibleTo, isAnonymous);

            // return the new list of comments in a Json object
            return Json(MakeCommentListJsonObject(post.Comments, id));
        }

        private string GetPostVisibilityGroups(int eventLogId)
        {
            return DBHelper.GetEventLogVisibilityGroups(eventLogId);
        }

        /// <summary>
        /// Sends an email to those who have access to this post and have the
        /// "Send all activity feed posts to my e-mail address" option checked
        /// </summary>
        private void SendEmailsToListeners(string postContent, int sourcePostID, int courseID, DateTime timePosted, List<MailAddress> emails, bool isReply = false, bool isAnonymous = false)
        {
#if !DEBUG
            // first check to see if we need to email anyone about this post
            if (emails.Count > 0)
            {
                string subject = "";
                string body = "";

                using (SqlConnection conn = DBHelper.GetNewConnection())
                {
                    if (isReply)
                    {
                        FeedPostEvent originalPost = DBHelper.GetFeedPostEvent(sourcePostID, conn);

                        string originalPostComment = "";

                        if (null == originalPost) //ask for help event
                        {
                            AskForHelpEvent afhe = DBHelper.GetAskForHelpEvent(sourcePostID, conn);
                            if (afhe != null)
                            {
                                originalPostComment = afhe.UserComment;
                            }
                            else
                            {
                                ExceptionEvent ee = DBHelper.GetExceptionEvent(sourcePostID, conn);
                                if (ee != null)
                                {
                                    originalPostComment = ee.ExceptionDescription;
                                }
                                else
                                {
                                    originalPostComment = "(See post in OSBLE for full details)";
                                }
                            }

                        }
                        else
                        {
                            originalPostComment = originalPost.Comment;
                        }

                        UserProfile originalPoster = DBHelper.GetFeedItemSender(sourcePostID, conn, isAnonymous);

                        //we want to sanitize for the current user information if the reply was anonymous
                        string currentUserFullName = isAnonymous ? "Anonymous User" : CurrentUser.FullName;
                        subject = string.Format("OSBLE Plus - {0} replied to a post in {1}", currentUserFullName, DBHelper.GetCourseShortNameFromID(courseID, conn));
                        body = string.Format("{0} replied to a post{1} at {2}:\n\n{3}\n-----------------------\nOriginal Post:\n{4}\n\n", // by {1} > "" to temporarily handle anon reply
                            currentUserFullName, "", timePosted.UTCToCourse(courseID), postContent, originalPostComment);
                        body = ReplaceMentionWithName(body);
                    }
                    else
                    {
                        //we want to sanitize for the current user information if the reply was anonymous
                        string currentUserFullName = isAnonymous ? "Anonymous User" : CurrentUser.FullName;

                        subject = string.Format("OSBLE Plus - {0} posted in {1}", currentUserFullName, DBHelper.GetCourseShortNameFromID(courseID, conn));
                        body = string.Format("{0} made the following post at {1}:\n\n{2}\n\n", currentUserFullName, timePosted.UTCToCourse(courseID), postContent);
                        body = ReplaceMentionWithName(body);
                    }
                }

                // add a link at the bottom to the website
                body += string.Format("<a href=\"{0}\">View and reply to post in OSBLE</a>", Url.Action("Details", "Feed", new { id = sourcePostID }, Request.Url.Scheme));

                body += ".";
                //Send the message
                Email.Send(subject, body, emails);
            }
#endif
        }

        /// <summary>
        /// The @mention tags will be replaced with the correct names
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public string ReplaceMentionWithName(string body)
        {
            List<int> nameIndices = new List<int>();
            
           
            for (int i = 0; i < body.Length; i++)
            {
                try
                {// If we find an '@' character, see if it's followed by "id=" then a number then a semicolon
                    if (body[i] == '@')
                    {
                        if (body.Substring(i + 1, 3) == "id=")
                        {
                            // After the '=', make sure there are numbers then a semicolon following it
                            int digit = 0, rIndex = 4;
                            bool hasDigit = false;
                            while (int.TryParse(body.Substring(i + rIndex, 1), out digit))  // Keep reading characters until we hit something that isn't a digit
                            {
                                hasDigit = true;
                                rIndex++;
                            }
                            if (hasDigit && body[i + rIndex] == ';')
                                nameIndices.Add(i); // If the character following the numbers is a semicolon, we know there is a name reference here so record the index
                        }
                    }
                }
                catch (Exception e)
                {
                    //throw out some information about variables in the current state to try and get more information about the exception to generate an actual fix in the future. 
                    throw new Exception("Body.Length = " + body.Length + " nameIndicies.Count= " + nameIndices.Count + " i= " + i, e);
                }
            }
            nameIndices.Reverse();
            foreach (int index in nameIndices) // In reverse order, we need to replace each @... with the students name
            {

                try
                {

                    // First let's get the length of the part we will replace and also record the id
                    int length = 0, tempIndex = index + 1;
                    string idString = "";
                    while (body[tempIndex] != ';') { length++; tempIndex++; idString += body[tempIndex]; }

                    // Get the id= part off the beginning of idString and the ; from the end
                    idString = idString.Substring(2);
                    idString = idString.Substring(0, idString.Length - 1);

                    // Then get the student's name from the id
                    int id; int.TryParse(idString, out id);
                    if (id != null)
                    {
                        UserProfile referencedUser = (from user in db.UserProfiles where user.ID == id select user).FirstOrDefault();
                        if (referencedUser == null) continue; // It's possible the user no longer exists, or for some reason someone manually entered @id=blahblahblah; into the text field.
                        string studentFullName = referencedUser.FirstName + referencedUser.LastName;

                        // Now replace the id number in the string with the user name
                        body = body.Replace(body.Substring(index + 1, length + 1), string.Format("<a href=\"{0}\">{1}</a>", Url.Action("Index", "Profile", new { id = id }, Request.Url.Scheme), studentFullName));
                    }
                }
                catch (Exception ex)
                {

                    throw new Exception("Index = " + index + " nameIndices.Count= " + nameIndices.Count,ex);
                }
            }
            return body;
        }

        [HttpPost]
        public ActionResult ApplyFeedfilter(IEnumerable<EventType> eventFilter = null, string commentFilter = null)
        {
            if (eventFilter != null)
            {
                _activityFeedQuery.UpdateEventSelectors(eventFilter.ToList());
            }

            if (commentFilter != null)
            {
                _activityFeedQuery.CommentFilter = commentFilter;
            }

            _activityFeedQuery.CourseFilter = new Course() { ID = _activityFeedQuery.CourseFilter.ID };

            return View("Index");
        }

        /// <summary>
        /// Constructs a basic query to be further manipulated by other functions in this class
        /// </summary>
        /// <returns></returns>
        private void BuildBasicQuery(ref ActivityFeedQuery query)
        {
            //check for null query
            if (query == null)
            {
                query = _activityFeedQuery;
            }

            foreach (var evt in ActivityFeedQuery.GetAllEvents())
            {
                query.AddEventType(evt);
            }

            //load in course and user type filtering
            query.CourseRoleFilter = (CourseRole)query.CourseRoleFilter;
            query.CourseFilter = new Course() { ID = query.CourseFilter.ID };
        }

        public List<FeedItem> GetFeedItemsFromIDs(IEnumerable<int> ids)
        {
            List<FeedItem> items = new List<FeedItem>();

            foreach (int id in ids)
            {
                items.Add(Feeds.Get(id));
            }

            return items;
        }

        [HttpGet]
        public JsonResult GetAggregateFeedFromIDs(string stringIds)
        {
            FeedViewModel vm = GetFeedViewModel();

            List<int> ids = new List<int>();

            if (stringIds != "")
            {
                //parse comma separated id list e.g. "1,2,3"
                ids = stringIds.Split(',').Select(Int32.Parse).ToList();
            }

            List<FeedItem> feedItems = GetFeedItemsFromIDs(ids);
            vm.Feed = AggregateFeedItem.FromFeedItems(feedItems);

            vm.SelectedCourseId = ActiveCourseUser.AbstractCourseID;
            vm.SingleUserId = ActiveCourseUser.UserProfileID;

            return GetJsonFromViewModel(vm);
        }

        [HttpPost]
        public JsonResult GetProfileIndexForName(string firstName, string lastName)
        {
            int index = DBHelper.GetUserProfileIndexForName(firstName, lastName);
            return Json(new { Index = index });
        }

        [HttpPost]
        public JsonResult GetProfileName(int id)
        {
            string name = "";
            UserProfile up = DBHelper.GetUserProfile(id);
            if (up != null)
            {
                name = up.DisplayName(ActiveCourseUser).Replace(" ", "");
            }
            return Json(new { Name = name });
        }

        [HttpPost]
        public JsonResult GetProfileNames()
        {
            if (ActiveCourseUser == null) //new user with no active courses
            {
                return Json(new { userProfiles = new Dictionary<string, string>() });
            }
            List<UserProfile> userProfiles = DBHelper.GetUserProfilesForCourse(ActiveCourseUser.AbstractCourseID);
            var instructors = DBHelper.GetCourseInstructorIds(ActiveCourseUser.AbstractCourseID);
            Dictionary<string, string> nameIdPairs = new Dictionary<string, string>();
            foreach (UserProfile userProfile in userProfiles) //
            {
                if (!nameIdPairs.ContainsKey(userProfile.ID.ToString()))
                {
                    nameIdPairs.Add(userProfile.ID.ToString(), userProfile.FullName);
                }
            }
            return Json(new { userProfiles = nameIdPairs });
        }

        public static Dictionary<string, string> GetProfileNames(int courseId)
        {
            List<UserProfile> userProfiles = DBHelper.GetUserProfilesForCourse(courseId);
            Dictionary<string, string> nameIdPairs = new Dictionary<string, string>();

            foreach (UserProfile userProfile in userProfiles)
            {
                if (!nameIdPairs.ContainsKey(userProfile.ID.ToString()))
                {
                    nameIdPairs.Add(userProfile.ID.ToString(), userProfile.FullName);
                }
            }
            return nameIdPairs;
        }

        [HttpGet]
        public ActionResult ShowHashtag(string hashtag)
        {
            if (hashtag == null || hashtag == "")
                return RedirectToAction("Index", "Home");

            hashtag = hashtag.Split('?').First(); // //?component=7 is added to links to force opening in the main window in the VS Plugin, it has no effect in-browser. the actual hashtag should be the first item always

            ViewBag.Hashtag = hashtag;
            ViewBag.HideLoadMore = true;
            ViewBag.CurrentCourseUsers = DBHelper.GetUserProfilesForCourse(ActiveCourseUser.AbstractCourseID);
            ViewBag.HashTags = DBHelper.GetHashTags();

            string enableLogging = ConfigurationManager.AppSettings["EnableActivityLogging"];
            if (enableLogging == "true") //only log if logging is enabled
            {
                try
                {
                    string authToken = Request.Cookies["AuthKey"].Value.Split('=').Last();
                    LogActivityEvent(authToken, "ClickHashtag", hashtag, "Hashtag", ActiveCourseUser.AbstractCourseID);
                }
                catch (Exception)
                {
                    //do nothing for now                    
                }
            }

            return View();
        }

        [HttpPost]
        [OsbleAuthorize]
        public string GetFeedPostContent(int eventId, int parentEventId)
        {
            if (parentEventId == -1) //feedpost
            {
                FeedPostEvent feedPost = DBHelper.GetFeedPostEvent(eventId);
                return feedPost.Comment;
            }
            else //logcomment
            {
                LogCommentEvent logComment = DBHelper.GetLogCommentEvents(parentEventId).Where(lce => lce.EventLogId == eventId).FirstOrDefault();
                return logComment.Content;
            }

        }

        public JsonResult GetPermissions(int eventId)
        {
            ActivityEvent e = DBHelper.GetActivityEvent(eventId);

            if (ActiveCourseUser == null)
            {
                return Json(new
                {
                    canDelete = false,
                    canEdit = false,
                    canMail = false,
                    hideMail = true,
                    eventVisibilityGroups = "",
                    canVote = false,
                    showPicture = false,
                    eventVisibleTo = "",
                    isAnonymous = false,
                });
            }
            else
            {
                e.SetPrivileges(ActiveCourseUser, e);
            }

            return Json(new
            {
                canDelete = e.CanDelete,
                canEdit = e.CanEdit,
                canMail = e.CanMail,
                hideMail = e.HideMail,
                eventVisibilityGroups = e.EventVisibilityGroups,
                canVote = e.CanVote,
                showPicture = e.ShowProfilePicture,
                eventVisibleTo = e.EventVisibleTo,
                isAnonymous = e.IsAnonymous
            });
        }

        private class AutocompleteObject
        {
            public int value { get; set; }

            public string label { get; set; }

            public AutocompleteObject(int value, string label)
            {
                this.value = value;
                this.label = label;
            }
        }

        /// <summary>
        /// Autocomplete Search for Posts. Returns JSON.
        /// </summary>
        /// <returns></returns>
        public ActionResult AutoCompleteNames() //Not used?
        {
            string term = Request.Params["term"].ToString().ToLower();
            // If we are not anonymous or an Observer in a course, allow search of all users.
            List<int> authorizedCourses = currentCourses
                .Where(c => c.AbstractRole.Anonymized == false && c.AbstractRoleID != (int)CourseRole.CourseRoles.Observer)
                .Select(c => c.AbstractCourseID)
                .ToList();

            List<UserProfile> authorizedUsers = db.CourseUsers
                .Where(c => authorizedCourses.Contains(c.AbstractCourseID))
                .Select(c => c.UserProfile)
                .ToList();

            // If we are anonymous or an Observer, limit search to ourselves plus instructors/TAs
            List<int> addedCourses = currentCourses
                .Where(c => c.AbstractRole.Anonymized == true || c.AbstractRoleID != (int)CourseRole.CourseRoles.Observer)
                .Select(c => c.AbstractCourseID)
                .ToList();

            List<UserProfile> addedUsers = db.CourseUsers
                .Where(c => addedCourses.Contains(c.AbstractCourseID) && ((c.UserProfileID == CurrentUser.ID) || (c.AbstractRole.CanGrade == true)))
                .Select(c => c.UserProfile)
                .ToList();

            // Combine lists into one distinct list of users, removing all pending users.
            List<UserProfile> users = authorizedUsers.Union(addedUsers).Where(u => u.UserName != null).OrderBy(u => u.LastName).Distinct().ToList();

            // Search list for our search string
            users = users.Where(u => (u.FirstName + " " + u.LastName).ToLower().IndexOf(term) != -1).ToList();

            List<AutocompleteObject> outputList = new List<AutocompleteObject>();

            foreach (UserProfile u in users)
            {
                outputList.Add(new AutocompleteObject(u.ID, u.FirstName + u.LastName));
            }

            return Json(outputList, JsonRequestBehavior.AllowGet);
        }

        public bool InEventVisibleToList(string eventVisibleToList, int userProfileId)
        {
            //case: legacy or open to all viewers. we get a "" list in the case where the value is null 
            //or we purposly saved a list as "" so visibility is for everyone
            if (eventVisibleToList == "")
            {
                return true;
            }
            //if the list is not empty, make sure the user id is in the list.
            List<string> idList = eventVisibleToList.Split(',').ToList();

            foreach (string id in idList)
            {
                if (id != "" && Int32.Parse(id) == userProfileId)
                {
                    return true;
                }
            }
            //if the list is not "" and we did not find an id match, hide the post/reply
            return false;
        }

        [HttpPost]
        public HttpResponseMessage LogActivityEvent(string authToken, string eventAction, string eventData, string eventDataDescription, int courseId = 0)
        {
            try
            {
                if (courseId == 0 && ActiveCourseUser != null)
                {
                    courseId = ActiveCourseUser.AbstractCourseID;
                }

                var auth = new Authentication();
                if (!auth.IsValidKey(authToken) || courseId == 0)
                    return new HttpResponseMessage { StatusCode = HttpStatusCode.Forbidden };

                ActivityEvent log = new ActivityEvent();

                log.SenderId = auth.GetActiveUserId(authToken);
                if (courseId != 0)
                    log.CourseId = courseId;
                log.EventTypeId = 12; //OSBLEActivityEvent
                DateTime timestamp = DateTime.UtcNow;

                //save log
                SqlConnection connection = new SqlConnection(StringConstants.ConnectionString);
                var result = connection.Query<int>("INSERT INTO EventLogs (EventTypeId, EventDate, DateReceived, SenderId, CourseId)" +
                                                   "OUTPUT INSERTED.Id " +
                                                   "VALUES (@eventTypeId, @eventDate, @eventDate, @senderId, @courseId)",
                    new { eventTypeId = log.EventTypeId, eventDate = timestamp, senderId = log.SenderId, courseId = log.CourseId }).SingleOrDefault();

                if (result > 0)
                {
                    result = connection.Query<int>("INSERT INTO OSBLEActivityEvents (EventLogId, EventAction, EventData, EventDataDescription)" +
                                                   "OUTPUT INSERTED.Id " +
                                                   "VALUES (@eventLogId, @eventAction, @eventData, @eventDataDescription)",
                    new { eventLogId = result, eventAction = eventAction, eventData = eventData, eventDataDescription = eventDataDescription }).SingleOrDefault();
                }
                else //event did not save to the database
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Content = new StringContent(result.ToString())
                    };
                }

                return new HttpResponseMessage
                {
                    StatusCode = result > 0 ? HttpStatusCode.OK : HttpStatusCode.InternalServerError,
                    Content = new StringContent(result.ToString())
                };
            }
            catch (Exception e)
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(e.Message)
                };
            }
        }

        public ActionResult Profile(int id)
        {
            string enableLogging = ConfigurationManager.AppSettings["EnableActivityLogging"];

            if (enableLogging == "true") //only log if logging is enabled
            {
                try
                {
                    string authToken = Request.Cookies["AuthKey"].Value.Split('=').Last();
                    LogActivityEvent(authToken, "ClickMention", id.ToString(), "MentionUserProfileId", ActiveCourseUser.AbstractCourseID);
                }
                catch (Exception)
                {
                    //do nothing for now                    
                }
            }
            //redirect to the profile page of the user
            return RedirectToAction("Index", "Profile", new { id = id });
        }
    }
}
