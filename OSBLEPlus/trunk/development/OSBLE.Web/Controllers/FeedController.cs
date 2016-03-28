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
using OSBLE.Utility;
using OSBLEPlus.Logic.DataAccess.Activities;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Auth;
using OSBLEPlus.Logic.Utility.Lookups;
using OSBLE.Hubs;
using Microsoft.AspNet.SignalR;

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
                if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                    ViewBag.IsInstructor = true;
                else
                    ViewBag.IsInstructor = false;
                //FeedViewModel vm = GetFeedViewModel(timestamp, errorType, errorTypeStr, keyword, hash);
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
            else if(ActiveCourseUser != null)
            {
                courseID = ActiveCourseUser.AbstractCourseID;
            }

            if (ActiveCourseUser == null || courseID == null) //may get here using the plugin
            {
                using (AccountController account = new AccountController())
                {
                    if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                        ViewBag.IsInstructor = true;
                    else
                        ViewBag.IsInstructor = false;

                    Authentication authenticate = new Authentication();
                    return account.TokenLogin(authenticate.GetAuthenticationKey(), StringConstants.WebClientRoot + "/feed/osbide/");
                }
            }

            if (ActiveCourseUser.AbstractRoleID == (int)CourseRole.CourseRoles.Instructor)
                ViewBag.IsInstructor = true;
            else
                ViewBag.IsInstructor = false;

            ViewBag.ActiveCourse = DBHelper.GetCourseUserFromProfileAndCourse(ActiveCourseUser.UserProfileID, (int)courseID);
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

        private FeedViewModel GetFeedViewModel()
        {
            var query = _activityFeedQuery;

            FeedViewModel vm = new FeedViewModel();

            List<FeedItem> returnItems = _activityFeedQuery.Execute().ToList();

            // add senders to items
            using (SqlConnection sqlc = DBHelper.GetNewConnection())
            {
                foreach (FeedItem f in returnItems)
                {
                    f.Event.Sender = DBHelper.GetUserProfile(f.Event.SenderId, sqlc);                    
                }
            }

            //and finally, retrieve our list of feed items
            int maxIdQuery = int.MaxValue;

            foreach (FeedItem f in returnItems)
            {
                if (f.Event.EventId < maxIdQuery)
                    maxIdQuery = f.Event.EventId;
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

            try
            {
                vm.LastPollDate = aggregateFeed.Select(a => a.MostRecentOccurance).Max();
            }
            catch (Exception)
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

        private string GetDisplayTimeString(DateTime time)
        {
            return time.UTCToCourse(ActiveCourseUser.AbstractCourseID).ToShortDateString() + " " + time.UTCToCourse(ActiveCourseUser.AbstractCourseID).ToShortTimeString();
        }

        private object MakeLogCommentJsonObject(LogCommentEvent comment, Dictionary<int, bool> commentMarkedDictionary, SqlConnection sql = null)
        {
            comment.SetPrivileges(ActiveCourseUser);
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
                ActiveCourseUserId = ActiveCourseUser.UserProfileID
            };
        }

        private object MakeAggregateFeedItemJsonObject(AggregateFeedItem item, bool details)
        {
            var eventLog = item.Items[0].Event;
            eventLog.SetPrivileges(ActiveCourseUser);

            var comments = MakeCommentListJsonObject(item.Comments, eventLog.EventLogId);
            string viewFolder = details ? "Details/_" : "Feed/_";
            string idString = null;

            if (eventLog.EventType == EventType.HelpfulMarkGivenEvent)
            {
                // need to change the detailsId to the Feed details for a HelpfulMarkGivenEvent
                idString = DBHelper.GetHelpfulMarkFeedSourceId(eventLog.EventLogId).ToString();
            }


            return new
            {
                EventId = eventLog.EventLogId,
                ParentEventId = -1,
                SenderName = eventLog.DisplayTitle,
                SenderId = item.Creator.ID,
                TimeString = GetDisplayTimeString(item.MostRecentOccurance),
                EventDate = item.MostRecentOccurance.Ticks,
                CanMail = eventLog.CanMail,
                CanEdit = eventLog.CanEdit,
                CanDelete = eventLog.CanDelete,
                CanReply = eventLog.CanReply,
                IsHelpfulMark = item.PrettyName == EventType.HelpfulMarkGivenEvent.ToString().ToDisplayText(),
                HighlightMark = false,
                ShowPicture = eventLog.ShowProfilePicture,
                Comments = comments,
                HTMLContent = PartialView(viewFolder + eventLog.EventType.ToString().Replace(" ", ""), item).Capture(this.ControllerContext),
                //Content = eventLog.EventType == EventType.FeedPostEvent ? (eventLog as FeedPostEvent).Comment : "",
                IdString = idString ?? string.Join(",", item.Items.Select(i => i.Event.EventLogId)),
                ActiveCourseUserId = ActiveCourseUser.UserProfileID
            };
        }

        private object MakeCommentListJsonObject(IEnumerable<LogCommentEvent> comments, int parentLogID)
        {
            var obj = new List<dynamic>();

            // get all the eventLogIds for the comments passed in
            List<int> commentIds = comments.Select(comment => comment.EventLogId).ToList();



            using (SqlConnection sql = DBHelper.GetNewConnection())
            {
                Dictionary<int, bool> logCommentMarkedByCurrentUser =
                    DBHelper.DictionaryOfMarkedLogs(ActiveCourseUser.UserProfileID, commentIds, sql);
                foreach (LogCommentEvent e in comments)
                {
                    obj.Add(MakeLogCommentJsonObject(e, logCommentMarkedByCurrentUser, sql));
                }
            }

            return obj;
        }

        private JsonResult GetJsonFromViewModel(FeedViewModel vm)
        {
            var obj = new { Feed = new List<dynamic>(), HasLastPost = vm.Feed.Count < _activityFeedQuery.MaxQuerySize };
            foreach (AggregateFeedItem item in vm.Feed)
            {
                obj.Feed.Add(MakeAggregateFeedItemJsonObject(item, false));
            }
            return Json(obj, JsonRequestBehavior.AllowGet);
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
            UserProfile current = DBHelper.GetUserProfile(ActiveCourseUser.UserProfileID);// dp = db.DashboardPosts.Find(id);

            if ((current.IUserId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
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
            UserProfile current = DBHelper.GetUserProfile(ActiveCourseUser.UserProfileID);// dp = db.DashboardPosts.Find(id);

            if ((current.IUserId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
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
            // do checking, make sure non-authorized users cannot edit posts
            UserProfile current = CurrentUser ?? DBHelper.GetUserProfile(ActiveCourseUser.UserProfileID);
            if ((current.IUserId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
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

            UserProfile current = CurrentUser; //DBHelper.GetUserProfile(ActiveCourseUser.UserProfileID);
            if ((current.IUserId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
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

        [HttpGet]
        public JsonResult MarkHelpfulComment(int eventLogToMark, int markerId)
        {
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
        public JsonResult GetMorePosts(long endDate)
        {
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

            // remove the first item, since it's already in the feed
            if (vm.Feed.Count > 0)
                vm.Feed.RemoveAt(0);

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
            return vm;
        }

        /// <summary>
        /// Adds a global comment that will appear in the activity feed
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateInput(false)]
        public JsonResult PostFeedItem(string text, bool emailToClass = false)
        {
            // We purposefully are not catching exceptions that could be thrown
            // here, because we want this response to fail if there is an error
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException();
            }

            

            int courseID = ActiveCourseUser.AbstractCourseID;
            FeedPostEvent log = new FeedPostEvent()
                {
                    SenderId = CurrentUser.ID,
                    Comment = text,
                    CourseId = courseID,
                    SolutionName = null
                };

            int logID = Posts.SaveEvent(log);
            var newPost = new AggregateFeedItem(Feeds.Get(logID));

            //get email addresses for users who want to be notified by email
            var emailList = DBHelper.GetActivityFeedForwardedEmails(courseID, null, emailToClass);
            //remove email for current user if they do not want to get their own posts emailed.
            bool emailSelf = ActiveCourseUser.UserProfile.EmailSelfActivityPosts;
            if (!emailSelf)
            {
                emailList.RemoveAll(el => el.Address == ActiveCourseUser.UserProfile.Email);
            }
            // Send emails to those who want to be notified by email
            SendEmailsToListeners(log.Comment, logID, courseID, DateTime.UtcNow, emailList);

            return Json(MakeAggregateFeedItemJsonObject(newPost, false));
        }

        [HttpPost]
        [ValidateInput(false)]
        public JsonResult PostComment(int id, string content)
        {
            // Check for blank comment
            if (String.IsNullOrWhiteSpace(content))
            {
                throw new Exception();
            }

            // Insert the comment
            bool success = DBHelper.InsertActivityFeedComment(id, CurrentUser.ID, content);
            if (!success)
            {
                throw new Exception();
            }

            // Get the new comment list by getting the parent feed item
            FeedItem post = Feeds.Get(id);

            // Send emails if neccesary
            SendEmailsToListeners(content, id, ActiveCourseUser.AbstractCourseID, DateTime.UtcNow, DBHelper.GetReplyForwardedEmails(id), true);

            // return the new list of comments in a Json object
            return Json(MakeCommentListJsonObject(post.Comments, id));
        }

        /// <summary>
        /// Sends an email to those who have access to this post and have the
        /// "Send all activity feed posts to my e-mail address" option checked
        /// </summary>
        private void SendEmailsToListeners(string postContent, int sourcePostID, int courseID, DateTime timePosted, List<MailAddress> emails, bool isReply = false)
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
                        UserProfile originalPoster = DBHelper.GetFeedItemSender(sourcePostID, conn);

                        subject = string.Format("OSBLE Plus - {0} replied to a post in {1}", CurrentUser.FullName, DBHelper.GetCourseShortNameFromID(courseID, conn));
                        body = string.Format("{0} replied to a post by {1} at {2}:\n\n{3}\n-----------------------\nOriginal Post:\n{4}\n\n",
                            CurrentUser.FullName, originalPoster.FullName, timePosted.UTCToCourse(courseID), postContent, originalPost.Comment);
                    }
                    else
                    {
                        subject = string.Format("OSBLE Plus - {0} posted in {1}", CurrentUser.FullName, DBHelper.GetCourseShortNameFromID(courseID, conn));
                        body = string.Format("{0} made the following post at {1}:\n\n{2}\n\n", CurrentUser.FullName, timePosted.UTCToCourse(courseID), postContent);
                    }
                }

                // add a link at the bottom to the website
                body += string.Format("<a href=\"{0}\">View and reply to post in OSBLE</a>", Url.Action("Details", "Feed", new { id = sourcePostID }, Request.Url.Scheme));

                //Send the message

                Email.Send(subject, body, emails);
        }
#endif
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

        private List<FeedItem> GetFeedItemsFromIDs(IEnumerable<int> ids)
        {
            List<FeedItem> items = new List<FeedItem>();

            foreach (int id in ids)
            {
                items.Add(Feeds.Get(id));
            }

            return items;
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

        [HttpGet]
        public ActionResult ShowHashtag(string hashtag)
        {
            if (hashtag == null || hashtag == "")
                return RedirectToAction("Index", "Home");

            ViewBag.Hashtag = hashtag;
            return View();
        }

        public JsonResult GetPermissions(int eventId)
        {
            ActivityEvent e = DBHelper.GetActivityEvent(eventId);
            e.SetPrivileges(ActiveCourseUser);

            return Json(new
            {
                canDelete = e.CanDelete,
                canEdit = e.CanEdit,
                canMail = e.CanMail,
                canVote = e.CanVote,
                showPicture = e.ShowProfilePicture
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
        public ActionResult AutoCompleteNames()
        {
            string term = Request.Params["term"].ToString().ToLower();
            // If we are not anonymous in a course, allow search of all users.
            List<int> authorizedCourses = currentCourses
                .Where(c => c.AbstractRole.Anonymized == false)
                .Select(c => c.AbstractCourseID)
                .ToList();

            List<UserProfile> authorizedUsers = db.CourseUsers
                .Where(c => authorizedCourses.Contains(c.AbstractCourseID))
                .Select(c => c.UserProfile)
                .ToList();

            // If we are anonymous, limit search to ourselves plus instructors/TAs
            List<int> addedCourses = currentCourses
                .Where(c => c.AbstractRole.Anonymized == true)
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
    }
}
