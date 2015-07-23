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
            //_userSettings = (from setting in Db.UserFeedSettings
            //                    where setting.UserId == CurrentUser.Id
            //                    orderby setting.Id descending
            //                    select setting)
            //                .Take(1)
            //                .FirstOrDefault();
        }

        //public ActionResult GetHashTags(string query, bool isHandle = false)
        //{
        //    var tag = isHandle ? query.TrimStart('@') : query.TrimStart('#');
        //    return Json(GetHashtagsProc.Run(string.Format("%{0}%", tag), isHandle).Select(t => t.Name).ToArray(), JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult GetTrendingNotifications()
        //{
        //    return Json(GetHashtagsProc.GetTrendAndNotification(CurrentUser.Id, 10, true), JsonRequestBehavior.AllowGet);
        //}

        /// <summary>
        /// 
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
        public ActionResult GetFeed(long timestamp = -1, int errorType = -1, string errorTypeStr = "", string keyword = "", int hash = 0)
        {
            try
            {
                FeedViewModel vm = GetFeedViewModel(timestamp, errorType, errorTypeStr, keyword, hash);
                return PartialView("Feed/_Feed", vm);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = ex.Message;
                return PartialView("Error");
            }
        }

        private void BuildEventRelations(FeedViewModel vm, List<FeedItem> feedItems)
        {
            //build the "you and 5 others got this error"-type messages
            //vm.RecentUserErrors = base.GetRecentCompileErrors(CurrentUser).ToList();

            //This code does the following:
            // 1. Find all errors being sent out
            // 2. Find students who have recently had these errors
            // 3. Add this info to our VM

            //step 1
            //List<BuildEvent> feedBuildEvents = feedItems
            //    .Where(i => i.LogType.CompareTo(BuildEvent.Name) == 0)
            //    .Select(i => i.Event)
            //    .Cast<BuildEvent>()
            //    .ToList();
            SortedDictionary<string, string> sortedFeedBuildErrors = new SortedDictionary<string, string>();
            //List<string> feedBuildErrors;
            //foreach (BuildEvent build in feedBuildEvents)
            //{
            //    foreach (BuildEventErrorListItem errorItem in build.ErrorItems)
            //    {
            //        string key = errorItem.ErrorListItem.CriticalErrorName;
            //        if (string.IsNullOrEmpty(key) == false)
            //        {
            //            if (sortedFeedBuildErrors.ContainsKey(key) == false)
            //            {
            //                sortedFeedBuildErrors.Add(key, key);
            //            }
            //        }
            //    }
            //}

            ////convert the above to a normal list once we're done
            //feedBuildErrors = sortedFeedBuildErrors.Keys.ToList();

            ////step 2: find other students who have also had these errors
            //List<UserBuildErrorsByType> classBuildErrors = new List<UserBuildErrorsByType>();
            //DateTime maxLookback = base.DefaultErrorLookback;
            //var classBuilds = (from buildError in Db.BuildErrors
            //                    where feedBuildErrors.Contains(buildError.BuildErrorType.Name)
            //                    && buildError.Log.DateReceived > maxLookback
            //                    && buildError.Log.SenderId != CurrentUser.Id
            //                    group buildError by buildError.BuildErrorType.Name into be
            //                    select new { ErrorName = be.Key, Users = be.Select(s => s.Log.Sender).Distinct() }).ToList();

            //foreach (var item in classBuilds)
            //{
            //    classBuildErrors.Add(new UserBuildErrorsByType()
            //    {
            //        Users = item.Users.ToList(),
            //        ErrorName = item.ErrorName
            //    });
            //}
            //#endregion
            //vm.RecentClassErrors = classBuildErrors;
        }

        /// <summary>
        /// Returns a raw feed without any extra HTML chrome.  Used for AJAX updates to an existing feed.
        /// </summary>
        /// <param name="id">The ID of the last feed item received by the client</param>
        /// <returns></returns>
        public ActionResult RecentFeedItems(int id, int userId = -1, int errorType = -1, string keyword = "",
            int hash = 0)
        {
            //return View("AjaxFeed", new List<AggregateFeedItem>()); 

            var query = _activityFeedQuery;
            query.CommentFilter = hash == 0 ? keyword : "#" + keyword;
            if (errorType > 0)
            {
                //query = new BuildErrorQuery(Db);
                //(query as BuildErrorQuery).BuildErrorTypeId = errorType;
            }
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
            BuildEventRelations(vm, feedItems);

            ViewBag.RecentUserErrors = vm.RecentUserErrors;
            //ViewBag.RecentClassErrors = vm.RecentClassErrors;
            //ViewBag.ErrorTypes = vm.ErrorTypes;

            return View("AjaxFeed", aggregateFeed);
        }

        //[System.Web.Http.HttpPost]
        //[ValidateInput(false)]
        //public async Task<JsonResult> PostCommentAsync(string logId, string comment)
        //{
        //    int id = -1;
        //    if (Int32.TryParse(logId, out id))
        //    {
        //        bool result = await PostComment(logId, comment);
        //        return GetComments(id);
        //    }
        //    return this.Json(new {});
        //}

        [HttpPost]
        public ActionResult PostComment(FormCollection formCollection)
        {
            string content = formCollection["response"];
            string logIDstr = formCollection["logID"]; // the id of the parent event log
            int logID = -1;
            
            if (String.IsNullOrWhiteSpace(content) || String.IsNullOrWhiteSpace(logIDstr) || !int.TryParse(logIDstr, out logID))
            {
                return new EmptyResult();
            }

                // Insert the comment
            bool success = DBHelper.InsertActivityFeedComment(logID, CurrentUser.ID, content);
                if (!success)
                    return new EmptyResult();

            // Get the new comment list, and put it into a model
            FeedItem post = GetFeedItemFromID(logID);
            // Add a new FeedItem for each comment whose event is the logCommentEvent and whose comments are an empty list
            List<FeedItem> model = post.Comments.Select(c => new FeedItem { Event = c, Comments = new List<LogCommentEvent>() }).ToList();
                // Get the new comment list, and put it into a Model
                ActivityFeedQuery q = _activityFeedQuery;
                q.AddEventId(logID);

            // return the newly created partial view for the list of comments
            ViewData["ShowFooter"] = false;
            ViewData["ShowDetails"] = true;            
            ViewBag.ParentId = logID;
            return PartialView("Feed/_FeedItems", AggregateFeedItem.FromFeedItems(model));
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
        public ActionResult EditFeedPost(int id, string newText)
        {
            // do checking, make sure non-authorized users cannot edit posts

            UserProfile current = DBHelper.GetUserProfile(ActiveCourseUser.UserProfileID);
            if ((current.UserId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
            {
                DBHelper.EditFeedPost(id, newText);
            }
            else
            {
                Response.StatusCode = 403;
            }


            return View("_AjaxEmpty");
        }

        [HttpPost]
        public ActionResult EditLogComment(int id, string newText)
        {
            // do checking, make sure non-authorized users cannot edit posts

            UserProfile current = DBHelper.GetUserProfile(ActiveCourseUser.UserProfileID);
            if ((current.UserId == ActiveCourseUser.UserProfileID) || (ActiveCourseUser.AbstractRole.CanGrade))
            {
                DBHelper.EditLogComment(id, newText);
            }
            else
            {
                Response.StatusCode = 403;
            }


            return View("_AjaxEmpty");
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

        //[System.Web.Http.HttpPost]
        //public ActionResult GetItemUpdates(List<GetItemUpdatesViewModel> items)
        //{
        //    try
        //    {
        //        if (items != null)
        //        {
        //            List<int> logIds = items.Select(i => i.LogId).ToList();
        //            DateTime lastPollDate = new DateTime(items.First().LastPollTick);
        //            var query = from comment in Db.LogCommentEvents.Include("HelpfulMarks").Include("EventLog").Include("EventLog.Sender").AsNoTracking()
        //                        where
        //                        (logIds.Contains(comment.SourceEventLogId) || logIds.Contains(comment.EventLogId))
        //                        && comment.EventDate > lastPollDate
        //                        select comment;
        //            List<LogCommentEvent> comments = query.ToList();
        //            long lastPollTick = items[0].LastPollTick;
        //            var maxCommentTick = comments.Select(c => c.EventDate);
        //            if (maxCommentTick.Count() > 0)
        //            {
        //                lastPollTick = maxCommentTick.Max().Ticks;
        //            }
        //            var result = new { LastPollTick = lastPollTick, Comments = comments };
        //            ViewBag.LastPollTick = lastPollTick;
        //            return View(comments);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogErrorMessage(ex);
        //    }

        //    return View(new List<LogCommentEvent>());
        //}

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
                BuildEventRelations(vm, feedItems);

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
            BuildEventRelations(fvm, feedItems);

            ViewBag.RecentUserErrors = fvm.RecentUserErrors;
            //ViewBag.RecentClassErrors = fvm.RecentClassErrors;
            //ViewBag.ErrorTypes = fvm.ErrorTypes;

            FeedDetailsViewModel vm = new FeedDetailsViewModel();
            vm.Ids = id;
            vm.FeedItem = aggregateItems.FirstOrDefault();
            return vm;
        }

        /// <summary>
        /// Will subscribe the active user to the event log with the supplied ID number
        /// </summary>
        /// <param name="id"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        //public ActionResult FollowPost(int id, string returnUrl)
        //{
        //    int count = Db.EventLogs.Where(l => l.Id == id).Count();
        //    if (count > 0)
        //    {
        //        EventLogSubscription subscription = new EventLogSubscription()
        //        {
        //            LogId = id,
        //            UserId = CurrentUser.Id
        //        };
        //        try
        //        {
        //            Db.EventLogSubscriptions.Add(subscription);
        //            Db.SaveChanges();
        //        }
        //        catch (Exception ex)
        //        {
        //            LogErrorMessage(ex);
        //        }
        //    }

        //    Response.Redirect(returnUrl);
        //    return View();
        //}

        //public ActionResult MarkCommentHelpful(int commentId, string returnUrl)
        //{
        //    try
        //    {
        //        int count = Db.HelpfulMarkGivenEvents
        //            .Where(c => c.EventLog.SenderId == CurrentUser.Id)
        //            .Where(c => c.LogCommentEventId == commentId)
        //            .Count();
        //        if (count == 0)
        //        {
        //            LogCommentEvent comment = Db.LogCommentEvents.Where(c => c.Id == commentId).FirstOrDefault();
        //            if (commentId != 0)
        //            {
        //                HelpfulMarkGivenEvent help = new HelpfulMarkGivenEvent()
        //                {
        //                    LogCommentEventId = commentId
        //                };
        //                OsbideWebService client = new OsbideWebService();
        //                Authentication auth = new Authentication();
        //                string key = auth.GetAuthenticationKey();
        //                EventLog log = new EventLog(help, CurrentUser);
        //                client.SubmitLog(log, CurrentUser);
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogErrorMessage(ex);

        //    }
        //    Response.Redirect(returnUrl);
        //    return View();
        //}

        /// <summary>
        /// Will unsubscribe the active user from the event log with the supplied ID number
        /// </summary>
        /// <param name="id"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        //public ActionResult UnfollowPost(int id, string returnUrl)
        //{
        //    try
        //    {
        //        EventLogSubscription subscription = Db.EventLogSubscriptions.Where(s => s.UserId == CurrentUser.Id).Where(s => s.LogId == id).FirstOrDefault();
        //        if (subscription != null)
        //        {
        //            Db.Entry(subscription).State = System.Data.Entity.EntityState.Deleted;
        //            Db.SaveChanges();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogErrorMessage(ex);
        //    }
        //    Response.Redirect(returnUrl);
        //    return View();
        //}

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
                    //OsbideWebService client = new OsbideWebService();
                    //Authentication auth = new Authentication();
                    //string key = auth.GetAuthenticationKey();
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

                //return PartialView("_Feed", GetFeedViewModel());
            }
            catch (Exception ex)
            {
               // LogErrorMessage(ex);
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

        //[System.Web.Http.HttpPost]
        //public ActionResult ApplyFeedFilter(FormCollection formCollection)
        //{
        //    try
        //    {
        //        // update user settings in database
        //        //UserFeedSetting feedSetting = _userSettings;
        //        //if (feedSetting == null)
        //        //{
        //        //    feedSetting = new UserFeedSetting();
        //        //    feedSetting.UserId = CurrentUser.ID;
        //        //}
        //        //else
        //        //{
        //        //    feedSetting = new UserFeedSetting(feedSetting);
        //        //    feedSetting.Id = 0;
        //        //    feedSetting.SettingsDate = DateTime.UtcNow;
        //        //}
        //        ////Db.UserFeedSettings.Add(feedSetting);

        //        ////clear out existing settings
        //        //feedSetting.EventFilterSettings = 0;

        //        //load in new settings
        //        foreach (string key in Request.Form.Keys)
        //        {
        //            if (key.StartsWith("event_") == true)
        //            {
        //                string[] pieces = key.Split('_');
        //                if (pieces.Length == 2)
        //                {
        //                    EventType evt;
        //                    if (Enum.TryParse<EventType>(pieces[1], true, out evt))
        //                    {
        //                        feedSetting.SetSetting(evt, true);
        //                    }
        //                }
        //            }
        //        }

        //        //check for course filter
        //        if (Request.Form.AllKeys.Contains("course-filter"))
        //        {
        //            int courseId = -1;
        //            Int32.TryParse(Request.Form["course-filter"], out courseId);
        //            feedSetting.CourseFilter = courseId;
        //        }

        //        //check for user filter
        //        if (Request.Form.AllKeys.Contains("user-type-filter"))
        //        {
        //            int userRoleId = (int) CourseRole.CourseRoles.Student;
        //            Int32.TryParse(Request.Form["user-type-filter"], out userRoleId);
        //            using (SqlConnection conn = DBHelper.GetNewConnection())
        //            {
        //                string query = "SELECT * " +
        //                               "FROM AbstractRoles a " +
        //                               "WHERE " +
        //                               "a.Name = 'Student'";
        //                feedSetting.CourseRoleFilter = new CourseRole(conn.Query<CourseRole>(query).First());
        //            }
        //        }
        //        //save changes
        //        //Db.SaveChanges();

        //        //apply filter reload page
        //        var errorType = Request.Form.AllKeys.Contains("error-type") ? Request.Form["error-type"] : string.Empty;
        //        var keyword = ((EventFilterSetting)feedSetting.EventFilterSettings & EventFilterSetting.FeedPostEvent) == EventFilterSetting.FeedPostEvent
        //                        && Request.Form.AllKeys.Contains("keyword") ? Request.Form["keyword"] : string.Empty;

        //        return RedirectToAction("Index", new { errorType = errorType, keyword = keyword });
        //    }
        //    catch (Exception ex)
        //    {
        //        return RedirectToAction("Index");
        //        //LogErrorMessage(ex);
        //        //return RedirectToAction("FeedDown", "Error");
        //    }
        //}

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
