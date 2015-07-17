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
        private UserFeedSetting _userSettings = new UserFeedSetting();

        public FeedController()
        {
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
                var query = new ActivityFeedQuery();

                //query.CommentFilter = hash == 0 ? keyword : "#" + keyword;

                //Two ways that we can receive an error type: by name (errorTypeStr) or by ID (errorType).
                //First, we check the string and see if we can match it to an ID number.  Then, we check
                //to see if we have a valid ID number.  If it doesn't work out, just work as normal.
                //if (string.IsNullOrEmpty(errorTypeStr) == false)
                //{
                //    errorTypeStr = errorTypeStr.ToLower().Trim();
                //    ErrorType type = Db.ErrorTypes.Where(e => e.Name.CompareTo(errorTypeStr) == 0).FirstOrDefault();
                //    if (type != null)
                //    {
                //        errorType = type.Id;
                //    }
                //}
                //if (errorType > 0)
                //{
                //    query = new BuildErrorQuery(Db);
                //    (query as BuildErrorQuery).BuildErrorTypeId = errorType;
                //}
                //BuildBasicQuery(query);
                FeedViewModel vm = new FeedViewModel();

                if (timestamp > 0)
                {
                    DateTime pullDate = new DateTime(timestamp);
                    query.StartDate = pullDate;
                }
                else
                {
                    query.MaxQuerySize = 40;
                }

                //and finally, retrieve our list of feed items
                //var maxIdQuery = Db.EventLogs.Select(l => l.Id);
                //if (maxIdQuery.Count() > 0)
                //{
                //    vm.LastLogId = maxIdQuery.Max();
                //}
                //else
                //{
                //    vm.LastLogId = 0;
                //}

                List<FeedItem> feedItems = query.Execute().ToList();
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
                //vm.ErrorTypes = Db.ErrorTypes.Distinct().ToList();
                //vm.SelectedErrorType = new ErrorType();
                //if (errorType > 0)
                //{
                //    vm.SelectedErrorType = Db.ErrorTypes.Where(e => e.Id == errorType).FirstOrDefault();
                //    if (vm.SelectedErrorType == null)
                //    {
                //        vm.SelectedErrorType = new ErrorType();
                //    }
                //}

                ////build possible courses and user types
                //vm.Courses = Db.Courses.ToList();
                vm.CourseRoles.Add(CourseRole.CourseRoles.Student);
                vm.CourseRoles.Add(CourseRole.CourseRoles.TA);
                vm.CourseRoles.Add(CourseRole.CourseRoles.Instructor);
                //if (_userSettings != null)
                //{
                //    vm.SelectedCourseId = _userSettings.CourseFilter;
                //    vm.SelectedCourseRole = _userSettings.CourseRole;
                //}

                //build the "you and 5 others got this error"-type messages
                //BuildEventRelations(vm, feedItems);
                vm.Keyword = keyword;

                return View(vm);
            }
            catch (Exception ex)
            {
                //LogErrorMessage(ex);

                //return RedirectToAction("FeedDown", "Error");
            }

            // error go back to home page
            return RedirectToAction("Index", "Home");
        }

        private void BuildEventRelations(FeedViewModel vm, List<FeedItem> feedItems)
        {
            //build the "you and 5 others got this error"-type messages
            //vm.RecentUserErrors = base.GetRecentCompileErrors(CurrentUser).ToList();

            //This code does the following:
            // 1. Find all errors being sent out
            // 2. Find students who have recently had these errors
            // 3. Add this info to our VM

            #region class build errors

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

            var query = new ActivityFeedQuery();
            query.CommentFilter = hash == 0 ? keyword : "#" + keyword;
            if (errorType > 0)
            {
                //query = new BuildErrorQuery(Db);
                //(query as BuildErrorQuery).BuildErrorTypeId = errorType;
            }
            BuildBasicQuery(query);
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

        [System.Web.Http.HttpPost]
        [ValidateInput(false)]
        public async Task<JsonResult> PostCommentAsync(string logId, string comment)
        {
            int id = -1;
            if (Int32.TryParse(logId, out id))
            {
                bool result = await PostComment(logId, comment);
                return GetComments(id);
            }
            return this.Json(new {});
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
                        DateTime.Now,
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

                //foreach (int logId in logIds)
                //{
                //    var actualLogId = logId;

                //    if (allcomments.Any(c => c.OriginalId == logId && c.ActualId != logId))
                //    {
                //        // original log is either a comment or a helpful mark
                //        actualLogId = allcomments.First(c => c.OriginalId == logId && c.ActualId != logId).ActualId;
                //    }

                //    if (!viewModels.Keys.Contains(actualLogId))
                //    {
                //        viewModels.Add(actualLogId, new List<CommentsViewModel>());
                //    }

                //    var logComments = allcomments.Where(c => c.ActualId == actualLogId);
                //    //convert LogCommentEvents into JSON
                //    foreach (var comment in logComments)
                //    {
                //        if (!viewModels[actualLogId].Any(c => c.EventLogId == comment.CommentId))
                //        {
                //            var commentVM = new CommentsViewModel()
                //            {
                //                EventLogId = comment.CommentId,
                //                CourseName = comment.CourseName,
                //                Content = comment.Content,
                //                FirstAndLastName = comment.FirstAndLastName,
                //                ProfileUrl = Url.Action("Picture", "Profile", new {id = comment.SenderId, size = 48}),
                //                UtcEventDate = comment.EventDate,
                //                MarkHelpfulCount = comment.HelpfulMarkCounts,
                //                MarkHelpfulUrl =
                //                    Url.Action("MarkCommentHelpful", "Feed",
                //                        new {commentId = comment.CommentId, returnUrl = Url.Action("Index", "Feed")}),
                //                DisplayHelpfulMarkLink = !comment.IsHelpfulMarkSender,
                //            };

                //            //add to VM
                //            viewModels[actualLogId].Add(commentVM);
                    //    }
                    //}

                    //convert to json view model
                    //jsonVm.Add(
                    //    new {Comments = viewModels[actualLogId], ActualLogId = actualLogId, OriginalLogId = logId});
                return Json(new { Data = jsonVm }, JsonRequestBehavior.AllowGet);
            }

            
            //}
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
        //public ActionResult OldFeedItems(int id, int count, int userId, int errorType = -1, string keyword = "", int hash = 0)
        //{
        //    try
        //    {
        //        var query = new ActivityFeedQuery();
        //        query.CommentFilter = hash == 0 ? keyword : "#" + keyword;
        //        if (errorType > 0)
        //        {
        //            //query = new BuildErrorQuery(Db);
        //            //(query as BuildErrorQuery).BuildErrorTypeId = errorType;
        //        }
        //        BuildBasicQuery(query);
        //        query.MaxLogId = id;
        //        query.MaxQuerySize = count;

        //        //used to build a feed for a single person.  Useful for building profile-based feeds
        //        if (userId > 0)
        //        {
        //            query.ClearSubscriptionSubjects();
        //            //query.AddSubscriptionSubject(Db.Users.Where(u => u.Id == userId).FirstOrDefault());
        //        }

        //        List<FeedItem> feedItems = query.Execute().ToList();
        //        List<AggregateFeedItem> aggregateFeed = AggregateFeedItem.FromFeedItems(feedItems);


        //        //build the "you and 5 others got this error"-type messages
        //        FeedViewModel vm = new FeedViewModel();
        //        BuildEventRelations(vm, feedItems);

        //        ViewBag.RecentUserErrors = vm.RecentUserErrors;
        //        //ViewBag.RecentClassErrors = vm.RecentClassErrors;
        //        //ViewBag.ErrorTypes = vm.ErrorTypes;

        //        return View("AjaxFeed", aggregateFeed);
        //    }
        //    catch (Exception ex)
        //    {
        //        //LogErrorMessage(ex);
        //        //return RedirectToAction("FeedDown", "Error");
        //    }
        //    return View("Index");
        //}

        /// <summary>
        /// Provides a details view for the provided Log IDs
        /// </summary>
        /// <param name="id">The ID(s) of the logs to retrieve.  Accepts a comma delimited list.  
        /// In the case of rendering multiple IDs, an aggregate view will be created
        /// </param>
        /// <returns></returns>
        public ActionResult Details(string id)
        {
            try
            {
                //make sure that we've gotten a valid ID
                if (string.IsNullOrEmpty(id))
                {
                    return RedirectToAction("Index");
                }

                //check to receive if we've gotten a single ID back
                int idAsInt = -1;
                if (Int32.TryParse(id, out idAsInt) == true)
                {
                    //if we've received a log comment event or a helpful mark event, we have to reroute to the original event
                    //EventLog log = Db.EventLogs.Where(e => e.Id == idAsInt).FirstOrDefault();
                    //MarkReadProc.Update(idAsInt, CurrentUser.Id, true);
                    //if (log != null)
                    //{
                    //    if (log.LogType == LogCommentEvent.Name)
                    //    {
                    //        LogCommentEvent commentEvent = Db.LogCommentEvents.Where(c => c.EventLogId == log.Id).FirstOrDefault();
                    //        return RedirectToAction("Details", "Feed", new { id = commentEvent.SourceEventLogId });
                    //    }
                    //    else if (log.LogType == HelpfulMarkGivenEvent.Name)
                    //    {
                    //        HelpfulMarkGivenEvent helpfulEvent = Db.HelpfulMarkGivenEvents.Where(e => e.EventLogId == log.Id).FirstOrDefault();
                    //        return RedirectToAction("Details", "Feed", new { id = helpfulEvent.LogCommentEvent.SourceEventLogId });
                    //    }
                    //}
                }

                var query = new ActivityFeedQuery();

                List<int> ids = ParseIdString(id);
                foreach (int logId in ids)
                {
                    query.AddEventId(logId);
                }
                List<FeedItem> feedItems = query.Execute().ToList();
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
                //if (Db.EventLogSubscriptions.Where(e => e.UserId == CurrentUser.Id).Where(e => e.LogId == ids.Min()).Count() > 0)
                //{
                //    vm.IsSubscribed = true;
                //}
                return View(vm);
            }
            catch (Exception ex)
            {
                //LogErrorMessage(ex);
                //return RedirectToAction("FeedDown", "Error");
                return View();
            }
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
        public ActionResult PostFeedComment(FormCollection formCollection)
        {
            try
            {
                var comment = formCollection["comment"];
                if (string.IsNullOrWhiteSpace(comment)) return RedirectToAction("Index");

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
                            conn.Execute(log.GetInsertScripts());
                        }
                        catch (Exception ex)
                        {
                            //
                        }
                    }
                    //FeedPostEvent commentEvent = new FeedPostEvent();
                    //commentEvent.Comment = comment;
                    //log.Data.BinaryData = EventFactory.ToZippedBinary(commentEvent);
                    //log = client.SubmitLog(log, CurrentUser);

                    //find all of this user's subscribers and send them an email
                    //List<UserProfile> observers = new List<UserProfile>();

                    //observers = (from subscription in Db.UserSubscriptions
                    //             join user in Db.Users on
                    //                             new { InstitutionId = subscription.ObserverInstitutionId, SchoolId = subscription.ObserverSchoolId }
                    //                             equals new { InstitutionId = user.InstitutionId, SchoolId = user.SchoolId }
                    //             where subscription.SubjectSchoolId == CurrentUser.SchoolId
                    //             && subscription.SubjectInstitutionId == CurrentUser.InstitutionId
                    //             && user.ReceiveEmailOnNewFeedPost == true
                    //             select user).ToList();
                    //if (observers.Count > 0)
                    //{
                    //    string url = StringConstants.GetActivityFeedDetailsUrl(log.Id);
                    //    string body = "Greetings,<br />{0} posted a new item to the activity feed:<br />\"{1}\"<br />To view this "
                    //    + "conversation online, please visit {2} or visit your OSBIDE user profile.<br /><br />Thanks,\nOSBIDE<br /><br />"
                    //    + "These automated messages can be turned off by editing your user profile.";
                    //    body = string.Format(body, CurrentUser.FirstAndLastName, comment, url);
                    //    List<MailAddress> to = new List<MailAddress>();
                    //    foreach (OsbideUser user in observers)
                    //    {
                    //        to.Add(new MailAddress(user.Email));
                    //    }
                    //    Email.Send("[OSBIDE] New Activity Post", body, to);
                    //}
                }
            }
            catch (Exception ex)
            {
               // LogErrorMessage(ex);
            }
            return RedirectToAction("Index");
        }

        [System.Web.Http.HttpPost]
        public ActionResult ApplyFeedFilter(FormCollection formCollection)
        {
            try
            {
                // update user settings in database
                UserFeedSetting feedSetting = _userSettings;
                if (feedSetting == null)
                {
                    feedSetting = new UserFeedSetting();
                    feedSetting.UserId = CurrentUser.ID;
                }
                else
                {
                    feedSetting = new UserFeedSetting(feedSetting);
                    feedSetting.Id = 0;
                    feedSetting.SettingsDate = DateTime.UtcNow;
                }
                //Db.UserFeedSettings.Add(feedSetting);

                //clear out existing settings
                feedSetting.EventFilterSettings = 0;

                //load in new settings
                foreach (string key in Request.Form.Keys)
                {
                    if (key.StartsWith("event_") == true)
                    {
                        string[] pieces = key.Split('_');
                        if (pieces.Length == 2)
                        {
                            EventType evt;
                            if (Enum.TryParse<EventType>(pieces[1], true, out evt))
                            {
                                feedSetting.SetSetting(evt, true);
                            }
                        }
                    }
                }

                //check for course filter
                if (Request.Form.AllKeys.Contains("course-filter"))
                {
                    int courseId = -1;
                    Int32.TryParse(Request.Form["course-filter"], out courseId);
                    feedSetting.CourseFilter = courseId;
                }

                //check for user filter
                if (Request.Form.AllKeys.Contains("user-type-filter"))
                {
                    int userRoleId = (int) CourseRole.CourseRoles.Student;
                    Int32.TryParse(Request.Form["user-type-filter"], out userRoleId);
                    using (SqlConnection conn = DBHelper.GetNewConnection())
                    {
                        string query = "SELECT * " +
                                       "FROM AbstractRoles a " +
                                       "WHERE " +
                                       "a.Name = 'Student'";
                        feedSetting.CourseRoleFilter = new CourseRole(conn.Query<CourseRole>(query).First());
                    }
                }
                //save changes
                //Db.SaveChanges();

                //apply filter reload page
                var errorType = Request.Form.AllKeys.Contains("error-type") ? Request.Form["error-type"] : string.Empty;
                var keyword = ((EventFilterSetting)feedSetting.EventFilterSettings & EventFilterSetting.FeedPostEvent) == EventFilterSetting.FeedPostEvent
                                && Request.Form.AllKeys.Contains("keyword") ? Request.Form["keyword"] : string.Empty;

                return RedirectToAction("Index", new { errorType = errorType, keyword = keyword });
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index");
                //LogErrorMessage(ex);
                //return RedirectToAction("FeedDown", "Error");
            }
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
                query = new ActivityFeedQuery();
            }

            //add the event types that the user wants to see
            UserFeedSetting feedSettings = _userSettings;
            if (feedSettings == null || feedSettings.ActiveSettings.Count == 0)
            {
                foreach (var evt in ActivityFeedQuery.GetAllEvents())
                {
                    query.AddEventType(evt);
                }
            }
            else
            {
                //load in event filter settings
                foreach (EventFilterSetting setting in feedSettings.ActiveSettings)
                {
                    query.AddEventType(UserFeedSetting.FeedOptionToOsbideEvent(setting));
                }

                //load in course and user type filtering
                query.CourseRoleFilter = feedSettings.CourseRoleFilter;
                query.CourseFilter = new Course() { ID = feedSettings.CourseFilter };
            }

            //return query;
        }
    }
}
            #endregion