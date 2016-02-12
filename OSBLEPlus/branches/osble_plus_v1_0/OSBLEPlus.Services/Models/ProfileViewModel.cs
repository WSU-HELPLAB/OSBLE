using System;
using System.Collections.Generic;
using System.Linq;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLEPlus.Services.Models
{
    public class ProfileViewModel
    {
        public UserProfile User { get; set; }
        public FeedViewModel Feed { get; set; }
        public List<AggregateFeedItem> EventLogSubscriptions { get; set; }
        public SocialActivityManager SocialActivity { get; set; }
        //public UserScore Score { get; set; }
        public int NumberOfComments { get; set; }
        public int NumberOfPosts { get; set; }
        
        public ProfileViewModel()
        {
            Feed = new FeedViewModel();
            SocialActivity = new SocialActivityManager();
            EventLogSubscriptions = new List<AggregateFeedItem>();
        }
    }

    public class SocialActivityManager
    {
        private SortedDictionary<DateTime, SortedDictionary<int, SortedDictionary<int, CommentActivityLog>>> _activites = new SortedDictionary<DateTime, SortedDictionary<int, SortedDictionary<int, CommentActivityLog>>>();

        public List<CommentActivityLog> GetLogs(DateTime date, int EventLogId)
        {
            return _activites[date][EventLogId].Values.ToList();
        }

        public int TopLevelLogCount
        {
            get
            {
                return _activites.Count;
            }
        }

        public List<DateTime> ActivityDates
        {
            get
            {
                return _activites.Keys.ToList();
            }
        }

        public List<int> GetEventIds(DateTime date)
        {
            return _activites[date].Keys.ToList();
        }

        public void AddLog(CommentActivityLog activityLog)
        {
            DateTime dateKey = activityLog.LogCommentEvent.EventDate.Date;
            int logKey = activityLog.LogCommentEvent.SourceEventLogId;
            int userKey = activityLog.LogCommentEvent.SourceEvent.SenderId;

            //organized first by date
            if (_activites.ContainsKey(dateKey) == false)
            {
                _activites.Add(dateKey, new SortedDictionary<int,SortedDictionary<int,CommentActivityLog>>());
            }

            //then the ID of the event that they track
            if (_activites[dateKey].ContainsKey(logKey) == false)
            {
                _activites[dateKey].Add(logKey, new SortedDictionary<int, CommentActivityLog>());
            }

            //then the user the generated the event
            if (_activites[dateKey][logKey].ContainsKey(userKey) == false)
            {
                _activites[dateKey][logKey][userKey] = activityLog;
            }
            else
            {
                //we want the most recent post by the user, replace if we are storing an older item
                CommentActivityLog other = _activites[dateKey][logKey][userKey];
                if (activityLog.LogCommentEvent.EventDate > other.LogCommentEvent.EventDate)
                {
                    _activites[dateKey][logKey][userKey] = activityLog;
                }
            }
        }
    }
}
