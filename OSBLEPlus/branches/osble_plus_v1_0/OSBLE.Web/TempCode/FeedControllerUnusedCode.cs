using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OSBLE.TempCode
{
    public class FeedControllerUnusedCode
    {
        //public ActionResult GetHashTags(string query, bool isHandle = false)
        //{
        //    var tag = isHandle ? query.TrimStart('@') : query.TrimStart('#');
        //    return Json(GetHashtagsProc.Run(string.Format("%{0}%", tag), isHandle).Select(t => t.Name).ToArray(), JsonRequestBehavior.AllowGet);
        //}

        //public ActionResult GetTrendingNotifications()
        //{
        //    return Json(GetHashtagsProc.GetTrendAndNotification(CurrentUser.Id, 10, true), JsonRequestBehavior.AllowGet);
        //}


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

        //private void BuildEventRelations(FeedViewModel vm, List<FeedItem> feedItems)
        //{
        //    build the "you and 5 others got this error"-type messages
        //    vm.RecentUserErrors = base.GetRecentCompileErrors(CurrentUser).ToList();

        //    This code does the following:
        //     1. Find all errors being sent out
        //     2. Find students who have recently had these errors
        //     3. Add this info to our VM

        //    step 1
        //    List<BuildEvent> feedBuildEvents = feedItems
        //        .Where(i => i.LogType.CompareTo(BuildEvent.Name) == 0)
        //        .Select(i => i.Event)
        //        .Cast<BuildEvent>()
        //        .ToList();
        //    SortedDictionary<string, string> sortedFeedBuildErrors = new SortedDictionary<string, string>();
        //    List<string> feedBuildErrors;
        //    foreach (BuildEvent build in feedBuildEvents)
        //    {
        //        foreach (BuildEventErrorListItem errorItem in build.ErrorItems)
        //        {
        //            string key = errorItem.ErrorListItem.CriticalErrorName;
        //            if (string.IsNullOrEmpty(key) == false)
        //            {
        //                if (sortedFeedBuildErrors.ContainsKey(key) == false)
        //                {
        //                    sortedFeedBuildErrors.Add(key, key);
        //                }
        //            }
        //        }
        //    }

        //    //convert the above to a normal list once we're done
        //    feedBuildErrors = sortedFeedBuildErrors.Keys.ToList();

        //    //step 2: find other students who have also had these errors
        //    List<UserBuildErrorsByType> classBuildErrors = new List<UserBuildErrorsByType>();
        //    DateTime maxLookback = base.DefaultErrorLookback;
        //    var classBuilds = (from buildError in Db.BuildErrors
        //                        where feedBuildErrors.Contains(buildError.BuildErrorType.Name)
        //                        && buildError.Log.DateReceived > maxLookback
        //                        && buildError.Log.SenderId != CurrentUser.Id
        //                        group buildError by buildError.BuildErrorType.Name into be
        //                        select new { ErrorName = be.Key, Users = be.Select(s => s.Log.Sender).Distinct() }).ToList();

        //    foreach (var item in classBuilds)
        //    {
        //        classBuildErrors.Add(new UserBuildErrorsByType()
        //        {
        //            Users = item.Users.ToList(),
        //            ErrorName = item.ErrorName
        //        });
        //    }
        //    #endregion
        //    vm.RecentClassErrors = classBuildErrors;
        //}
    }
}