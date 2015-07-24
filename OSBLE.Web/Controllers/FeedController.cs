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
using OSBLE.Models.Courses;
using OSBLE.Models.Queries;
using OSBLE.Models;
using OSBLE.Models.Users;
using OSBLE.Models.ViewModels;
using OSBLE.Utility;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Auth;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLE.Controllers
{
    public class FeedController : OSBLEController
    {
        //private UserFeedSetting _userSettings = ;
        private ActivityFeedQuery _activityFeedQuery;
        public FeedController()
        {
            _activityFeedQuery = new ActivityFeedQuery(ActiveCourseUser.AbstractCourseID);
        }

        /// <summary>
        /// Index, returns feed items
        /// </summary>
        /// <param name="id">The ID of the last event received by the user.  Used for AJAX updates</param>
        /// <returns></returns>
        public ActionResult Index(long timestamp = -1, int errorType = -1, string errorTypeStr = "", string keyword = "",
            int hash = 0)
        {
            //turned off for now.
            //return RedirectToAction("FeedDown", "Error");
            try
            {
                FeedViewModel vm = GetFeedViewModel(timestamp, errorType, errorTypeStr, keyword, hash);
                return PartialView(vm);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return PartialView("Error");
            }

        }

        private FeedViewModel GetFeedViewModel(long timestamp = -1, int errorType = -1, string errorTypeStr = "", string keyword = "", int hash = 0)
        {
            var query = _activityFeedQuery;

                //query.CommentFilter = hash == 0 ? keyword : "#" + keyword;

                FeedViewModel vm = new FeedViewModel();

                if (timestamp > 0)
                {
                    DateTime pullDate = new DateTime(timestamp);
                    query.StartDate = pullDate;
                }
                else
                {
                    query.MaxQuerySize = 20;
                }

                List<FeedItem> returnItems = query.Execute().ToList();

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
                List<FeedItem> feedItems = returnItems.OrderByDescending(i => i.Event.EventDate).ToList();

                List<AggregateFeedItem> aggregateFeed = AggregateFeedItem.FromFeedItems(feedItems);
                //this.UpdateLogSubscriptions(CurrentUser);
                try
                {
                    vm.LastPollDate = aggregateFeed.Select(a => a.MostRecentOccurance).Max();
                }
                catch (Exception)
                {
                    vm.LastPollDate = DateTime.MinValue.AddDays(2);
                }
                vm.Feed = aggregateFeed;
                vm.EventFilterOptions = ActivityFeedQuery.GetAllEvents().OrderBy(e => e.ToString()).ToList();
                vm.UserEventFilterOptions = query.ActiveEvents;

                ////build possible courses and user types
                vm.CourseRoles.Add(CourseRole.CourseRoles.Student);
                vm.CourseRoles.Add(CourseRole.CourseRoles.TA);
                vm.CourseRoles.Add(CourseRole.CourseRoles.Instructor);
                vm.Keyword = keyword;
            return vm;
        }

        /// <summary>
        /// Returns just the feed part of the activity feed, without the forms at the top for posting/filtering.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GetFeed(long timestamp = -1, int errorType = -1, string errorTypeStr = "", string keyword = "", int hash = 0)
        {
            FeedViewModel vm = GetFeedViewModel(timestamp, errorType, errorTypeStr, keyword, hash);
            return GetJsonFromViewModel(vm);
        }   

        private object MakeLogCommentJsonObject(LogCommentEvent comment)
        {
            comment.SetPrivileges(ActiveCourseUser);
            return new
            {
                EventId = comment.EventLogId,
                ParentEventId = comment.SourceEventLogId,
                SenderName = comment.DisplayTitle,
                SenderId = comment.SenderId,
                TimeString = "time", //e.EventDate.UTCToCourse(e.CourseId).ToLongDateString(),
                CanMail = comment.CanMail,
                CanEdit = comment.CanEdit,
                CanDelete = comment.CanDelete,
                CanReply = false,
                ShowPicture = comment.ShowProfilePicture,
                Comments = new List<dynamic>(),
                HTMLContent = PartialView("Details/_LogCommentEvent", comment).Capture(this.ControllerContext),
                Content = comment.Content,
                IdString = comment.EventId.ToString()
            };
        }

        private object MakeCommentListJsonObject(IEnumerable<LogCommentEvent> comments, int parentLogID)
        {
            var obj = new List<dynamic>();
            foreach(LogCommentEvent e in comments)
            {
                obj.Add(MakeLogCommentJsonObject(e));
            }
            return obj;
        }

        private JsonResult GetJsonFromViewModel(FeedViewModel vm)
        {
            var obj = new { Feed = new List<dynamic>() };
            foreach(AggregateFeedItem item in vm.Feed)
            {
                var eventLog = item.Items[0].Event;
                eventLog.SetPrivileges(ActiveCourseUser);

                var comments = MakeCommentListJsonObject(item.Comments, eventLog.EventLogId);

                obj.Feed.Add(new {
                    EventId = eventLog.EventLogId,
                    ParentEventId = -1,
                    SenderName = eventLog.DisplayTitle,
                    SenderId = item.Creator.ID,
                    TimeString = "time",//item.MostRecentOccurance.UTCToCourse(eventLog.CourseId).ToLongDateString(),
                    CanMail = eventLog.CanMail,
                    CanEdit = eventLog.CanEdit,
                    CanDelete = eventLog.CanDelete,
                    CanReply = eventLog.CanReply,
                    ShowPicture = eventLog.ShowProfilePicture,
                    Comments = comments,
                    HTMLContent = PartialView("Feed/_" + eventLog.EventType.ToString().Replace(" ", ""), item).Capture(this.ControllerContext),
                    Content = eventLog.EventType == EventType.FeedPostEvent? (eventLog as FeedPostEvent).Comment : "",
                    IdString = string.Join(",", item.Items.Select(i => i.Event.EventLogId))
                });
            }
            return Json(obj);
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

        [HttpPost]
        public JsonResult PostComment(int id, string content)
        {            
            // Check for blank comment
            if (String.IsNullOrWhiteSpace(content)) {
                throw new Exception();
            }

            // Insert the comment
            bool success = DBHelper.InsertActivityFeedComment(id, CurrentUser.ID, content);
            if (!success) {
                throw new Exception();
            }

            // Get the new comment list by getting the parent feed item
            FeedItem post = GetFeedItemFromID(id);

            // return the new list of comments in a Json object
            return Json(MakeCommentListJsonObject(post.Comments, id));
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

            if ((current.UserId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
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

            if ((current.UserId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
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
        public JsonResult EditFeedPost(int id, string newText, bool details = false)
        {
            // do checking, make sure non-authorized users cannot edit posts

            UserProfile current = CurrentUser; //DBHelper.GetUserProfile(ActiveCourseUser.UserProfileID);
            if ((current.UserId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
            {
                using (SqlConnection conn = DBHelper.GetNewConnection())
                {
                    DBHelper.EditFeedPost(id, newText, conn);
                    AggregateFeedItem item = new AggregateFeedItem(GetFeedItemFromID(id));
                    string html;
                    if (details)
                        html = PartialView("Details/_FeedPostEvent", item).Capture(this.ControllerContext);
                    else
                        html = PartialView("Feed/_FeedPostEvent", item).Capture(this.ControllerContext);
                    return Json(new { HTMLContent = html });
                }
            }
            else
            {
                Response.StatusCode = 403;
                throw new Exception();
            }
        }

        [HttpPost]
        public JsonResult EditLogComment(int id, string newText)
        {
            // do checking, make sure non-authorized users cannot edit posts

            UserProfile current = CurrentUser; //DBHelper.GetUserProfile(ActiveCourseUser.UserProfileID);
            if ((current.UserId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
            {
                using (SqlConnection conn = DBHelper.GetNewConnection()) 
                {
                    DBHelper.EditLogComment(id, newText, conn);
                    LogCommentEvent c = DBHelper.GetSingularLogComment(id, conn);
                    return Json(new { HTMLContent = PartialView("Details/_LogCommentEvent", c).Capture(this.ControllerContext)});
                }

            }
            else
            {
                Response.StatusCode = 403;
                throw new Exception();
            }
        }



        public JsonResult GetComments(int? singleLogId)
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
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                FeedDetailsViewModel vm = GetDetailsViewModel(id);
                return View(vm);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return View("Error");
            }
        }

        private ActionResult DetailsPartial(string id)
        {
            try
            {
                FeedDetailsViewModel vm = GetDetailsViewModel(id);
                return PartialView("Details", vm);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return PartialView("Error");
            }
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
        /// <param name="comment"></param>
        /// <returns></returns>
        [System.Web.Http.HttpPost]
        [ValidateInput(false)]
        public ActionResult PostFeedItem(FormCollection formCollection)
        {
            try
            {
                var comment = formCollection["comment"];
                if (!string.IsNullOrWhiteSpace(comment)) 
                { 
                    comment = comment.TrimStart(',');

                    if (string.IsNullOrEmpty(comment) == false)
                    {
                        FeedPostEvent log = new FeedPostEvent()
                        {
                            SenderId = CurrentUser.ID,
                            Comment = comment,
                            CourseId = ActiveCourseUser.AbstractCourseID,
                            SolutionName = "OSBLEPlus"
                        };

                        using (SqlConnection conn = DBHelper.GetNewConnection())
                        {
                            try
                            {
                                string sql = log.GetInsertScripts();
                                conn.Execute(sql);
                            }
                            catch (Exception ex)
                            {
                                //
                            }
                        }

                    }
                }

            }
            catch (Exception ex)
            {
               // Ignore
            }
            return GetFeed();
        }

        [HttpPost]
        public ActionResult ApplyFeedfilter(IEnumerable<EventType> eventFilter = null, string commentFilter = null )
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

        private FeedItem GetFeedItemFromID(int id)
        {
            ActivityFeedQuery query = new ActivityFeedQuery(ActiveCourseUser.AbstractCourseID);
            query.MinLogId = id;
            query.MaxLogId = id;
            IEnumerable<FeedItem> result = query.Execute();
            return result.SingleOrDefault();
        }

        private List<FeedItem> GetFeedItemsFromIDs(IEnumerable<int> ids)
        {
            List<FeedItem> items = new List<FeedItem>();

            foreach(int id in ids)
            {
                items.Add(GetFeedItemFromID(id));
            }

            return items;
        }
    }
}
