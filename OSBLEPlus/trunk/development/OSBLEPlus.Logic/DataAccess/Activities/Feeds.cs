using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Xml.Schema;
using Dapper;

using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.DomainObjects.Profiles;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;
using OSBLE.Interfaces;
using OSBLE.Models.Users;

namespace OSBLEPlus.Logic.DataAccess.Activities
{
    public class Feeds
    {
        public static IEnumerable<FeedItem> Get(DateTime dateReceivedMin, DateTime dateReceivedMax,
            int? minEventLogId, int? maxEventLogId, IEnumerable<int> logIds, IEnumerable<int> eventTypes,
            int? courseId, int? roleId, string commentFilter,
            IEnumerable<int> senderIds, int? topN)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var c = string.IsNullOrWhiteSpace(commentFilter) ? string.Empty :  string.Format("%{0}%", commentFilter);
                var l = string.Join(",", logIds != null ? logIds.Where(i => i > 0).ToArray() : new int[0]);
                var etypes = eventTypes as int[] ?? eventTypes.ToArray();
                var t = etypes.Any() ? string.Format("{0}", string.Join(",", etypes)) : string.Empty;
                var s = string.Join(",", senderIds != null ? senderIds.Where(i => i > 0).ToArray() : new int[0]);

                var multiResults = connection.QueryMultiple("dbo.GetActivityFeeds",
                                        new
                                        {
                                            DateReceivedMin = dateReceivedMin,
                                            DateReceivedMax = dateReceivedMax,
                                            MinEventLogId = minEventLogId ?? -1,
                                            MaxEventLogId = maxEventLogId ?? 2000000000,
                                            EventLogIds = l,
                                            EventTypes = t,
                                            CourseId = courseId ?? 0,
                                            RoleId = roleId ?? 99,
                                            CommentFilter = c,
                                            SenderIds = s,
                                            TopN = topN ?? 20
                                        }, commandType: CommandType.StoredProcedure);
                
                var eventLogs = multiResults.Read<ActivityEvent>().ToList();
                var users = multiResults.Read<UserProfile>().ToList();
                var askHelps = multiResults.Read<AskForHelpEvent>().ToList();
                var builds = multiResults.Read<BuildEvent>().ToList();
                var exceptions = multiResults.Read<ExceptionEvent>().ToList();
                var feedPosts = multiResults.Read<FeedPostEvent>().ToList();
                var logComments = multiResults.Read<LogCommentEvent>().ToList();
                var helpMark = multiResults.Read<HelpfulMarkGivenEvent>().ToList();
                var submits = multiResults.Read<SubmitEvent>().ToList();

                return NormalizeDataObjects(eventLogs,
                                            users,
                                            askHelps,
                                            builds,
                                            exceptions,
                                            feedPosts,
                                            logComments,
                                            helpMark,
                                            submits);
            }
        }

        private static IEnumerable<FeedItem> NormalizeDataObjects(IList<ActivityEvent> eventLogs, IList<UserProfile> users,
            IList<AskForHelpEvent> askHelps, IList<BuildEvent> builds, IList<ExceptionEvent> exceptions,
            IList<FeedPostEvent> feedPosts, IList<LogCommentEvent> logComments,
            IList<HelpfulMarkGivenEvent> helpMarks, IList<SubmitEvent> submits)
        {
            var feedItems = new List<FeedItem>();
            var userDictionary = new Dictionary<int, IUser>();

            try
            {
                IActivityEvent xActivityEvent = null;
                foreach (var x in eventLogs)
                {
                    #region compose the event log's activity event

                    switch (x.EventType)
                    {
                        case EventType.AskForHelpEvent:
                            xActivityEvent = ComposeAskForHelpEvent(x, userDictionary, users, askHelps);
                            break;

                        case EventType.BuildEvent:
                            xActivityEvent = ComposeBuildEvent(x, userDictionary, users, builds);
                            break;

                        case EventType.ExceptionEvent:
                            xActivityEvent = ComposeExceptionEvent(x, userDictionary, users, exceptions);
                            break;

                        case EventType.FeedPostEvent:
                            xActivityEvent = ComposeFeedPostEvent(x, userDictionary, users, feedPosts);
                            break;

                        case EventType.SubmitEvent:
                            xActivityEvent = ComposeSubmitEvent(x, userDictionary, users, submits);
                            break;
                    }

                    #endregion

                    if (xActivityEvent == null) continue;
                    FeedItem f = new FeedItem()
                    {
                        Event = xActivityEvent,
                        Comments = ComposeComments(xActivityEvent, eventLogs, users, logComments, helpMarks, userDictionary)
                    };

                    if (!feedItems.Contains(f))
                        feedItems.Add(f);

                    xActivityEvent = null;
                    //feedItems.Add(new FeedItem
                    //{
                    //    Event = xActivityEvent,
                    //    Comments =
                    //        ComposeComments(xActivityEvent, eventLogs, users, logComments, helpMarks, userDictionary)
                    //});
                }
            }
            catch (Exception)
            {
                // do nothing
            }
            

            return feedItems;
        }

        #region static helpers

        private static IActivityEvent ComposeAskForHelpEvent(IEventLog eventLog,
            Dictionary<int, IUser> userDictionary,
            IEnumerable<IUser> users, IEnumerable<AskForHelpEvent> askHelps)
        {
            var evt = askHelps.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
            if (evt == null) return null;

            return new AskForHelpEvent(evt.EventDate)
            {
                EventId = evt.EventId,
                EventLogId = eventLog.EventLogId,
                SenderId = eventLog.SenderId,
                Sender = GetUser(userDictionary, users, eventLog.SenderId),
                Code = evt.Code,
                SolutionName = evt.SolutionName,
                UserComment = evt.UserComment
            };
        }

        private static IActivityEvent ComposeBuildEvent(IEventLog eventLog,
            Dictionary<int, IUser> userDictionary,
            IEnumerable<IUser> users, IEnumerable<BuildEvent> builds)
        {
            var evt = builds.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
            if (evt == null) return null;

            return new BuildEvent(evt.EventDate)
            {
                EventId = evt.EventId,
                EventLogId = eventLog.EventLogId,
                SenderId = eventLog.SenderId,
                Sender = GetUser(userDictionary, users, eventLog.SenderId),
                SolutionName = evt.SolutionName,
            };
        }

        private static IActivityEvent ComposeExceptionEvent(IEventLog eventLog,
            Dictionary<int, IUser> userDictionary,
            IEnumerable<IUser> users, IEnumerable<ExceptionEvent> exceptions)
        {
            var evt = exceptions.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
            if (evt == null) return null;

            return new ExceptionEvent(evt.EventDate)
            {
                EventId = evt.EventId,
                EventLogId = eventLog.EventLogId,
                SenderId = eventLog.SenderId,
                Sender = GetUser(userDictionary, users, eventLog.SenderId),
                SolutionName = evt.SolutionName,
                DocumentName = evt.DocumentName,
                ExceptionAction = evt.ExceptionAction,
                ExceptionCode = evt.ExceptionCode,
                ExceptionDescription = evt.ExceptionDescription,
                ExceptionType = evt.ExceptionType,
                LineContent = evt.LineContent,
                LineNumber = evt.LineNumber
            };
        }

        private static IActivityEvent ComposeFeedPostEvent(IEventLog eventLog,
            Dictionary<int, IUser> userDictionary,
            IEnumerable<IUser> users, IEnumerable<FeedPostEvent> exceptions)
        {
            var evt = exceptions.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
            if (evt == null) return null;

            return new FeedPostEvent(evt.EventDate)
            {
                EventId = evt.EventId,
                EventLogId = eventLog.EventLogId,
                SenderId = eventLog.SenderId,
                Sender = GetUser(userDictionary, users, eventLog.SenderId),
                SolutionName = evt.SolutionName,
                Comment = evt.Comment,
            };
        }

        private static IActivityEvent ComposeSubmitEvent(IEventLog eventLog,
            Dictionary<int, IUser> userDictionary,
            IEnumerable<IUser> users, IEnumerable<SubmitEvent> exceptions)
        {
            var evt = exceptions.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
            if (evt == null) return null;

            return new SubmitEvent(evt.EventDate)
            {
                EventId = evt.EventId,
                EventLogId = eventLog.EventLogId,
                SenderId = eventLog.SenderId,
                Sender = GetUser(userDictionary, users, eventLog.SenderId),
                SolutionName = evt.SolutionName,
                AssignmentId = evt.AssignmentId
            };
        }

        private static List<LogCommentEvent> ComposeComments(IActivityEvent subjectEvent,
            IList<ActivityEvent> eventLogs,
            IList<UserProfile> users,
            IEnumerable<LogCommentEvent> logComments,
            IList<HelpfulMarkGivenEvent> helpMarks,
            Dictionary<int, IUser> userDictionary)
        {
            var comments = logComments.Where(y => y.SourceEventLogId == subjectEvent.EventLogId).ToList();

            foreach (var c in comments)
            {
                // fill up each comment event properties not returned by query
                c.Sender = GetUser(userDictionary, users, c.SenderId);
                c.SourceEvent = subjectEvent;
                c.NumberHelpfulMarks = helpMarks.Count(y => y.LogCommentEventId == c.EventId);

                // inflate the comment helpful marks for detail views
                var commentMarks = helpMarks.Where(y => y.LogCommentEventId == c.EventId).ToList();
                foreach (var h in commentMarks)
                {
                    var helpMarkEventLog = eventLogs.Single(z => z.EventLogId == h.EventLogId);
                    h.Sender = GetUser(userDictionary, users, helpMarkEventLog.SenderId);
                    h.LogComment = c;
                }
            }
            return comments;
        }

        private static IUser GetUser(Dictionary<int, IUser> userDictionary, IEnumerable<IUser> users, int userId)
        {
            if (!userDictionary.Keys.Contains(userId))
                // change to firstordefault from single to prevent errors, may cause errors in the future?
                userDictionary.Add(userId, users.FirstOrDefault(y => y.UserId == userId));
                

            return userDictionary[userId];
        }

        #endregion static helpers
    }
}
