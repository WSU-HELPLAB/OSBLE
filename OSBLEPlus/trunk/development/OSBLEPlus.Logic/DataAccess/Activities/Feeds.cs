using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using OSBLE.Interfaces;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;

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
                string t;

                // get possible posts with user names in them
                List<string> possibleNames = new List<string>();
                List<int> possibleNameIds = new List<int>();

                List<FeedItem> nameFilterFeedItems = new List<FeedItem>();

                try
                {
                    var etypes = eventTypes as int[] ?? eventTypes.ToArray();
                    t = etypes.Any() ? string.Format("{0}", string.Join(",", etypes)) : string.Empty;
                }
                catch (Exception ex)
                {
                    t = "";
                }

                // add all the possible names of users from the comment filter to a list of logIds
                if (!string.IsNullOrEmpty(commentFilter))
                {
                    possibleNames = commentFilter.Split(' ').ToList();
                    string nameSql = "SELECT u.ID " +
                                     "FROM UserProfiles u " +
                                     "WHERE u.FirstName LIKE @name OR u.LastName LIKE @name";

                    foreach (string name in possibleNames)
                    {
                        string partialName = "%" + name + "%";
                        int id = connection.Query<int>(nameSql, new {name = partialName}).SingleOrDefault();

                        if (id > 0 && !possibleNameIds.Contains(id))
                        {
                            possibleNameIds.Add(id);
                        }
                    }

                    // Format names into a comma separated list
                    int[] nameIds = possibleNameIds.ToArray();
                    string nID = nameIds.Any() ? string.Format("{0}", string.Join(",", nameIds)) : string.Empty;

                    // sql query, -1 needs to be in the sender Ids list otherwise SQL will complain
                    // This gets a list of all the event logs with either names of students/teachers, or by comment values
                    string eventLogSql = string.Format(@"SELECT Id " +
                                                        "FROM EventLogs " +
                                                        "WHERE EventLogs.SenderId IN (-1{0}) OR EventLogs.Id IN ( " +
                                                            "SELECT DISTINCT SourceEventLogId AS EventLogId " +
                                                            "FROM LogCommentEvents " +
                                                            "JOIN EventLogs e ON " +
                                                            "e.Id = LogCommentEvents.EventLogId " +
                                                            "WHERE Content LIKE @filter OR SenderId IN (-1{0}) " +
                                                            "UNION " +
                                                            "SELECT FeedPostEvents.EventLogId " +
                                                            "FROM FeedPostEvents " +
                                                            "WHERE FeedPostEvents.Comment LIKE @filter) ", string.IsNullOrEmpty(nID) ? nID : "," + nID);


                    List<int> eventLogsForComments = connection.Query<int>(eventLogSql, new {filter = commentFilter}).ToList();

                    // recursive call to Get
                    if (possibleNameIds.Count > 0 || eventLogsForComments.Count > 0)
                    {
                        nameFilterFeedItems.AddRange(
                            Get(
                                dateReceivedMin,
                                dateReceivedMax,
                                minEventLogId,
                                maxEventLogId,
                                eventLogsForComments, // send list of EventLogs to filter
                                eventTypes,
                                courseId,
                                roleId,
                                "",
                                null,
                                topN)
                            );
                    }

                }
                
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
                                            CourseId = courseId ?? -1,
                                            RoleId = roleId ?? 99,
                                            CommentFilter = c,
                                            SenderIds = s,
                                            TopN = topN ?? 20
                                        }, commandType: CommandType.StoredProcedure);

                // multiResults reads in order of the tables declared in dbo.GetActivityEvents
                // if you need to change these make sure that the table is read in the right order

                var eventLogs = multiResults.Read<ActivityEvent>().ToList();            //1
                var users = multiResults.Read<UserProfile>().ToList();                  //2
                var askHelps = multiResults.Read<AskForHelpEvent>().ToList();           //3
                var builds = multiResults.Read<BuildEvent>().ToList();                  //4
                var cutcopypastes = multiResults.Read<CutCopyPasteEvent>().ToList();    //5
                var debugs = multiResults.Read<DebugEvent>().ToList();                  //6
                var editoractivites = multiResults.Read<EditorActivityEvent>().ToList();//7
                var exceptions = multiResults.Read<ExceptionEvent>().ToList();          //8
                var feedPosts = multiResults.Read<FeedPostEvent>().ToList();            //9
                var helpMark = multiResults.Read<HelpfulMarkGivenEvent>().ToList();     //10
                var logComments = multiResults.Read<LogCommentEvent>().ToList();        //11
                var saves = multiResults.Read<SaveEvent>().ToList();                    //12
                var submits = multiResults.Read<SubmitEvent>().ToList();                //13

                // associate logComments with senderId

                List<LogCommentEvent> nonDeletedLogComments = new List<LogCommentEvent>();
                //List<ActivityEvent> eventLogs = eventLogsTemp.Distinct(new ActivityEventEqualityComparer()).ToList();
                foreach (LogCommentEvent log in logComments)
                {
                    try
                    {
                        ActivityEvent e = eventLogs.Single(x => x.EventLogId == log.EventLogId);
                        if (e == null) continue;
                        
                        log.SenderId = e.SenderId;
                        if(!nonDeletedLogComments.Contains(log))
                            nonDeletedLogComments.Add(log);
                    }
                    catch(Exception ex)
                    {
                        //
                    }
                }

                var itemsToAdd = NormalizeDataObjects(
                        eventLogs,
                        users,
                        askHelps,
                        builds,
                        exceptions,
                        feedPosts,
                        nonDeletedLogComments,
                        helpMark,
                        submits,
                        cutcopypastes,
                        debugs,
                        editoractivites,
                        saves
                    );

                // remove duplicate items
                List<int> logIdsContained = nameFilterFeedItems.Select(x => x.Event.EventLogId).ToList();

                foreach (var item in itemsToAdd)
                {
                    if (!logIdsContained.Contains(item.Event.EventLogId))
                    {
                        nameFilterFeedItems.Add(item);
                    }
                }   

                return nameFilterFeedItems;
            }
        }

        public static FeedItem Get(int logId)
        {
            using (
                var connection = new SqlConnection(StringConstants.ConnectionString))
            {
                var multiResults = connection.QueryMultiple("dbo.GetActivityFeedById",
                                        new { LogId = logId }, commandType: CommandType.StoredProcedure);

                // multiResults reads in order of the tables declared in dbo.GetActivityEvents
                // if you need to change these make sure that the table is read in the right order

                var eventLogs = multiResults.Read<ActivityEvent>().ToList();            //1
                var users = multiResults.Read<UserProfile>().ToList();                  //2
                var askHelps = multiResults.Read<AskForHelpEvent>().ToList();           //3
                var builds = multiResults.Read<BuildEvent>().ToList();                  //4
                var cutcopypastes = multiResults.Read<CutCopyPasteEvent>().ToList();    //5
                var debugs = multiResults.Read<DebugEvent>().ToList();                  //6
                var editoractivites = multiResults.Read<EditorActivityEvent>().ToList();//7
                var exceptions = multiResults.Read<ExceptionEvent>().ToList();          //8
                var feedPosts = multiResults.Read<FeedPostEvent>().ToList();            //9
                var helpMark = multiResults.Read<HelpfulMarkGivenEvent>().ToList();     //10
                var logComments = multiResults.Read<LogCommentEvent>().ToList();        //11
                var saves = multiResults.Read<SaveEvent>().ToList();                    //12
                var submits = multiResults.Read<SubmitEvent>().ToList();                //13

                // associate logComments with senderId

                var nonDeletedLogComments = new List<LogCommentEvent>();
                foreach (var log in logComments)
                {
                    try
                    {
                        var e = eventLogs.Single(x => x.EventLogId == log.EventLogId);
                        if (e == null) continue;

                        log.SenderId = e.SenderId;
                        if (!nonDeletedLogComments.Contains(log))
                            nonDeletedLogComments.Add(log);
                    }
                    catch (Exception ex)
                    {
                        //
                    }
                }


                return NormalizeDataObjects(eventLogs,
                                            users,
                                            askHelps,
                                            builds,
                                            exceptions,
                                            feedPosts,
                                            nonDeletedLogComments,
                                            helpMark,
                                            submits,
                                            cutcopypastes,
                                            debugs,
                                            editoractivites,
                                            saves).FirstOrDefault();
            }
        }

        private static IEnumerable<FeedItem> NormalizeDataObjects(IList<ActivityEvent> eventLogs, IList<UserProfile> users,
            IList<AskForHelpEvent> askHelps, IList<BuildEvent> builds, IList<ExceptionEvent> exceptions,
            IList<FeedPostEvent> feedPosts, IList<LogCommentEvent> logComments,
            IList<HelpfulMarkGivenEvent> helpMarks, IList<SubmitEvent> submits, 
            IList<CutCopyPasteEvent> cutCopyPastes, IList<DebugEvent> debugs, 
            IList<EditorActivityEvent> editorActivities, IList<SaveEvent> saves )
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

                        case EventType.HelpfulMarkGivenEvent:
                            xActivityEvent = ComposeHelpfulMarkGivenEvent(x, userDictionary, users, helpMarks);
                            break;

                        case EventType.FeedPostEvent:
                            xActivityEvent = ComposeFeedPostEvent(x, userDictionary, users, feedPosts);
                            break;

                        case EventType.SubmitEvent:
                            xActivityEvent = ComposeSubmitEvent(x, userDictionary, users, submits);
                            break;
                        case EventType.SaveEvent:
                            xActivityEvent = ComposeSaveEvent(x, userDictionary, users, saves);
                            break;
                        case EventType.EditorActivityEvent:
                            xActivityEvent = ComposeEditorActivityEvent(x, userDictionary, users, editorActivities);
                            break;
                        case EventType.DebugEvent:
                            xActivityEvent = ComposeDebugEvent(x, userDictionary, users, debugs);
                            break;
                        case EventType.CutCopyPasteEvent:
                            xActivityEvent = ComposeCutCopyPasteEvent(x, userDictionary, users, cutCopyPastes);
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
                }
            }
            catch (Exception)
            {
                // ignore
            }
            

            return feedItems;
        }

        private static IActivityEvent ComposeHelpfulMarkGivenEvent(IEventLog eventLog,
            Dictionary<int, IUser> userDictionary, 
            IList<UserProfile> users, IList<HelpfulMarkGivenEvent> helpfulMarks)
        {
            var evt = helpfulMarks.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);

            if (evt == null) return null;

            return new HelpfulMarkGivenEvent()
            {
                EventId = evt.EventId,
                EventLogId = evt.EventLogId,
                SenderId = evt.Sender.IUserId,
                Sender = GetUser(userDictionary, users, eventLog.SenderId),
                SolutionName = evt.SolutionName,
                LogCommentEventId = evt.LogCommentEventId,
                LogComment = evt.LogComment
            };
        }

        private static IActivityEvent ComposeCutCopyPasteEvent(IEventLog eventLog, 
            Dictionary<int, IUser> userDictionary, 
            IList<UserProfile> users, IList<CutCopyPasteEvent> cutCopyPastes)
        {
            var evt = cutCopyPastes.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
            if (evt == null) return null;

            return new CutCopyPasteEvent()
            {
                EventId = evt.EventId,
                EventLogId = evt.EventLogId,
                Content = evt.Content,
                CourseId = evt.CourseId,
                DocumentName = evt.DocumentName,
                SenderId = eventLog.SenderId,
                Sender = GetUser(userDictionary, users, eventLog.SenderId),
                SolutionName = evt.SolutionName
            };
        }

        private static IActivityEvent ComposeDebugEvent(IEventLog eventLog, 
            Dictionary<int, IUser> userDictionary, 
            IList<UserProfile> users, IList<DebugEvent> debugs)
        {
            var evt = debugs.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
            if (evt == null) return null;

            return new DebugEvent()
            {
                EventId = evt.EventId,
                EventLogId = evt.EventLogId,
                CourseId = evt.CourseId,
                DebugOutput = evt.DebugOutput,
                LineNumber = evt.LineNumber,
                ExecutionAction = evt.ExecutionAction,
                DocumentName = evt.DocumentName,
                SenderId = eventLog.SenderId,
                Sender = GetUser(userDictionary, users, eventLog.SenderId)
            };
        }

        private static IActivityEvent ComposeEditorActivityEvent(IEventLog eventLog, 
            Dictionary<int, IUser> userDictionary, 
            IList<UserProfile> users, IList<EditorActivityEvent> editorActivities)
        {
            var evt = editorActivities.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
            if (evt == null) return null;

            return new EditorActivityEvent()
            {
                EventId = evt.EventId,
                EventLogId = evt.EventLogId,
                CourseId = evt.CourseId,
                SolutionName = evt.SolutionName,
                SenderId = eventLog.SenderId,
                Sender = GetUser(userDictionary, users, eventLog.SenderId)
            };
        }

        private static IActivityEvent ComposeSaveEvent(IEventLog eventLog, 
            Dictionary<int, IUser> userDictionary, 
            IList<UserProfile> users, IList<SaveEvent> saves)
        {
            var evt = saves.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
            if (evt == null) return null;

            return new SaveEvent()
            {
                EventId = evt.EventId,
                EventLogId = evt.EventLogId,
                DocumentId = evt.DocumentId,
                SolutionName = evt.SolutionName,
                SenderId = eventLog.SenderId,
                Sender = GetUser(userDictionary, users, eventLog.SenderId)
            };
        }

        #region static helpers

        private static IActivityEvent ComposeAskForHelpEvent(IEventLog eventLog,
            Dictionary<int, IUser> userDictionary,
            IEnumerable<IUser> users, IEnumerable<AskForHelpEvent> askHelps)
        {
            var evt = askHelps.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
            if (evt == null) return null;

            evt.Code = evt.Code.Replace("'", "''");

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
            Dictionary<int, IUser> userDictionary, IEnumerable<IUser> users, 
            IEnumerable<FeedPostEvent> exceptions)
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
            IEnumerable<IUser> users, IEnumerable<SubmitEvent> submits)
        {
            var evt = submits.SingleOrDefault(y => y.EventLogId == eventLog.EventLogId);
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
                userDictionary.Add(userId, users.FirstOrDefault(y => y.IUserId == userId));
                

            return userDictionary[userId];
        }

        #endregion static helpers
    }
}
