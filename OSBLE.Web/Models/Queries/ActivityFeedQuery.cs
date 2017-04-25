using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.WebPages.Scope;
using Dapper;
using DDay.Collections;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;
using OSBLE.Utility;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;
using OSBLEPlus.Logic.Utility.Lookups;
using OSBLEPlus.Services.Controllers;

namespace OSBLE.Models.Queries
{
    public class ActivityFeedQuery : IOSBLEQuery<FeedItem>
    {
        private List<EventType> _eventSelectors = new List<EventType>();
        protected List<UserProfile> SubscriptionSubjects = new List<UserProfile>();
        protected List<int> EventIds = new List<int>();

        public ActivityFeedQuery(int activeCourseUserCourseId)
        {
            StartDate = new DateTime(2010, 1, 1);
            EndDate = DateTime.Today.AddDays(3);
            CommentFilter = string.Empty;
            MinLogId = null;
            MaxLogId = null;
            MaxQuerySize = 20;
            //using (SqlConnection conn = DBHelper.GetNewConnection())
            //{
            //    string query = "SELECT * " +
            //                   "FROM AbstractRoles a " +
            //                   "WHERE " +
            //                   "a.Name = 'Student'";

            //    CourseRoleFilter = new CourseRole(conn.Query<CourseRole>(query).First());
            //}

            CourseRoleFilter = null;

            CourseFilter = new Course() { ID = activeCourseUserCourseId };
            // default events
            _eventSelectors = GetSocialEvents().ToList();
        }

        /// <summary>
        /// Sets a limit on the newest post to be retrieved.  Example: if <see cref="EndDate"/> is set to
        /// 2010-01-01, no posts after 2010-01-01 will be retrieved.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Sets a limit on the oldest post to be retrieved.  Example: if <see cref="StartDate"/> is set to
        /// 2010-01-01, no posts before 2010-01-01 will be retrieved.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Used to set a floor on the logs to retrieve.  Example: if <see cref="MinLogId"/> is set to 5,
        /// no posts with an Id less than 6 will be retrieved.
        /// </summary>
        public int? MinLogId { protected get; set; }

        /// <summary>
        /// Used to set a ceiling on the logs to retrieve.  Example: if <see cref="MaxLogId"/> is set to 5,
        /// no posts with an Id greater than 4 will be retrieved.
        /// </summary>
        public int? MaxLogId { protected get; set; }

        /// <summary>
        /// Used to limit the number of query results.  Default of -1 means to return all results.
        /// </summary>
        public int MaxQuerySize { get; set; }

        /// <summary>
        /// Used to select posts made by only certain users.  This works by restricting posts below 
        /// the supplied threshold.  E.g. CourseRole.Student will select everyone whereas 
        /// CourseRole.Coordinator will only select course coordinators.
        /// </summary>
        public CourseRole CourseRoleFilter { get; set; }

        /// <summary>
        /// Used to select only posts made by students in a given course.  Default value is all
        /// courses.
        /// </summary>
        public Course CourseFilter { get; set; }

        /// <summary>
        /// Comment search token entered by the user
        /// </summary>
        public string CommentFilter { get; set; }

        /// <summary>
        /// returns a lits of all social events in OSBLE
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<EventType> GetSocialEvents()
        {
            return new List<EventType>
            {
                EventType.FeedPostEvent,
                EventType.AskForHelpEvent,
                //EventType.LogCommentEvent,
                EventType.HelpfulMarkGivenEvent,
                //EventType.SubmitEvent,
            };
        }

        public static string GetCookies(EventType e)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies["FilterCookie"] ?? new HttpCookie("FilterCookie");

            // setup cookies to default value if they don't exist
            if (cookie.Values[e.ToString()] == null)
            {
                cookie.Values[e.ToString()] = GetSocialEvents().Contains(e).ToString();
            }

            cookie.Expires = DateTime.Now.AddDays(365);

            HttpContext.Current.Response.Cookies.Add(cookie);

            return cookie.Values[e.ToString()];
        }

        public static void FilterCookies(IEnumerable<EventType> events)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies["FilterCookie"] ?? new HttpCookie("FilterCookie");

            List<EventType> falseEvents = GetNecessaryEvents().Where(s => events.All(e => e != s)).ToList();

            foreach (var e in falseEvents)
            {
                cookie.Values[e.ToString()] = "False";
            }
            foreach (var e in events)
            {
                cookie.Values[e.ToString()] = "True";
            }

            // if they do exist, get form data to update values


            // keep cookie values for a year
            cookie.Expires = DateTime.Now.AddDays(365);

            // add it to the context
            HttpContext.Current.Response.Cookies.Add(cookie);
        }

        /// <summary>
        /// returns a list of IDE-based events in OSBLE
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<EventType> GetIdeEvents()
        {
            return new List<EventType>
            {
                EventType.BuildEvent,
                EventType.ExceptionEvent
            };
        }

        public static IEnumerable<EventType> GetNecessaryEvents(bool needHelpfulMark = true)
        {
            List<EventType> l;
            if (needHelpfulMark)
            {
                return new List<EventType>
                {
                    EventType.AskForHelpEvent,
                    //EventType.BuildEvent,
                    //EventType.DebugEvent,
                    EventType.HelpfulMarkGivenEvent,
                    EventType.ExceptionEvent,
                    EventType.FeedPostEvent,
                    EventType.SubmitEvent
                };
            }

            return new List<EventType>
            {
                EventType.AskForHelpEvent,
                EventType.BuildEvent,
                EventType.DebugEvent,
                EventType.ExceptionEvent,
                EventType.FeedPostEvent,
                EventType.SubmitEvent
            };

        }

        public static IEnumerable<EventType> GetEventsForUserProfile()
        {
            return new List<EventType>
            {
                EventType.AskForHelpEvent,
                EventType.HelpfulMarkGivenEvent,
                EventType.ExceptionEvent,
                EventType.FeedPostEvent,
                EventType.LogCommentEvent,
            };
        } 

        private IEnumerable<int> GetEventFilter()
        {
            List<int> l = new List<int>();

            l = _eventSelectors.Select(i => (int) i).ToList();

            return l;
        }

        /// <summary>
        /// returns a list of all possible events that a user can subscribe to
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<EventType> GetAllEvents()
        {
            return new List<EventType>
            {
                EventType.AskForHelpEvent,
                EventType.BuildEvent,
                EventType.CutCopyPasteEvent,
                EventType.DebugEvent,
                EventType.EditorActivityEvent,
                EventType.ExceptionEvent,
                EventType.FeedPostEvent,
                EventType.HelpfulMarkGivenEvent,
                EventType.SaveEvent,
                EventType.SubmitEvent
            };
        }

        /// <summary>
        /// add user selected event types
        /// </summary>
        /// <param name="evt"></param>
        public void AddEventType(EventType evt)
        {
            if (_eventSelectors.All(e => e != evt))
            {
                _eventSelectors.Add(evt);
            }
        }

        /// <summary>
        /// get user selected event types
        /// </summary>
        public List<EventType> ActiveEvents
        {
            get
            {
                return _eventSelectors.ToList();
            }
        }

        /// <summary>
        /// add user subscriptions
        /// </summary>
        /// <param name="user"></param>
        public void AddSubscriptionSubject(UserProfile user)
        {
            if (user != null)
            {
                SubscriptionSubjects.Add(user);
            }
        }

        /// <summary>
        /// combine current and the new user subscriptions
        /// </summary>
        /// <param name="users"></param>
        public void AddSubscriptionSubject(IEnumerable<UserProfile> users)
        {
            SubscriptionSubjects = SubscriptionSubjects.Union(users).ToList();
        }

        /// <summary>
        /// clear user subscriptions
        /// </summary>
        public void ClearSubscriptionSubjects()
        {
            SubscriptionSubjects = new List<UserProfile>();
        }

        /// <summary>
        /// add sepecific event id for query filter
        /// </summary>
        /// <param name="id"></param>
        public void AddEventId(int id)
        {
            EventIds.Add(id);
        }

        private void ClearEventIds()
        {
            EventIds.Clear();
        }

        private void ClearEventSelectors()
        {
            _eventSelectors.Clear();
        }

        public void UpdateEventSelectors(IEnumerable<EventType> eList)
        {
            ClearEventSelectors();

            _eventSelectors = eList.Select(e => e).Distinct().ToList();
        }

        /// <summary>
        /// execute the query, returns 20 items by default
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<FeedItem> Execute()
        {
            var query = new OSBLEPlus.Services.Controllers.FeedController().Get(
                                StartDate // 1
                                , EndDate // 2
                                , MinLogId
                                , MaxLogId
                                , EventIds//.Select(eid => (int)eid).ToList() // 3
                                , GetEventFilter()//_eventSelectors.Select(e => (int)e) // 4
                                , CourseFilter != null && CourseFilter.ID > 0 ? CourseFilter.ID : 0
                                , CourseRoleFilter == null ? (int?) null : CourseRoleFilter.ID
                                , CommentFilter
                                , SubscriptionSubjects.Select(s => s.ID).ToList()
                                , MaxQuerySize
                                );

            MinLogId = MaxLogId = null;

            return query.GetAwaiter().GetResult();
        }
        /// <summary>
        /// Another query execution, able to designate the number of items you want
        /// </summary>
        /// <param name="numberOfItemsToReturn"></param>
        /// <returns></returns>
        public IEnumerable<FeedItem> Execute(int? numberOfItemsToReturn)
        {
            var query = new OSBLEPlus.Services.Controllers.FeedController().Get(
                    StartDate // 1
                    , EndDate // 2
                    , MinLogId
                    , MaxLogId
                    , EventIds//.Select(eid => (int)eid).ToList() // 3
                    , GetEventFilter()//_eventSelectors.Select(e => (int)e) // 4
                    , CourseFilter != null && CourseFilter.ID > 0 ? CourseFilter.ID : 0
                    , CourseRoleFilter == null ? (int?) null : CourseRoleFilter.ID
                    , CommentFilter == String.Empty ? String.Empty : "%" + CommentFilter + "%"
                    , SubscriptionSubjects.Select(s => s.ID).ToList()
                    , numberOfItemsToReturn
                    );

            MinLogId = MaxLogId = null;

            return query.GetAwaiter().GetResult();
        }

        /// <summary>
        /// For use with user's profile, returns all FeedItems of the user's ID given
        /// Will still need to be converted to AggregateFeedItems
        /// </summary>
        public static IEnumerable<FeedItem> ProfileQuery(int courseUserId)
        {
            List<int> eventIds = DBHelper.GetUserFeedFromId(courseUserId).ToList();

            var query = new OSBLEPlus.Services.Controllers.FeedController().Get(
                new DateTime(2010, 1, 1),   // start date
                DateTime.Now.AddDays(3),               // end date
                null,                       // min log id
                null,                       // max log id
                eventIds,                   // eventIds
                null,  // Event Filter
                null,                       // Course Filter
                null,                       // Role Filter
                String.Empty,               // Comment Filter
                new List<int>(){1},                       // Subscriptions
                eventIds.Count              // number to return
                );

            return query.GetAwaiter().GetResult();
        }

    }
}