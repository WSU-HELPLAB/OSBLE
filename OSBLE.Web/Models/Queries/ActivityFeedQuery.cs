using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.WebPages.Scope;
using Dapper;
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
        private readonly List<EventType> _eventSelectors = new List<EventType>();
        protected List<UserProfile> SubscriptionSubjects = new List<UserProfile>();
        protected readonly List<int> EventIds = new List<int>();

        public ActivityFeedQuery()
        {
            StartDate = new DateTime(2010, 1, 1);
            EndDate = DateTime.Today.AddDays(3);
            CommentFilter = string.Empty;
            MinLogId = null;
            MaxLogId = null;
            MaxQuerySize = 20;
            using (SqlConnection conn = DBHelper.GetNewConnection())
            {
                string query = "SELECT * " +
                               "FROM AbstractRoles a " +
                               "WHERE " +
                               "a.Name = 'Student'";

                CourseRoleFilter = new CourseRole(conn.Query<CourseRole>(query).First());
            }
            CourseFilter = new Course() { ID = -1 };
        }

        /// <summary>
        /// Sets a limit on the newest post to be retrieved.  Example: if <see cref="EndDate"/> is set to
        /// 2010-01-01, no posts after 2010-01-01 will be retrieved.
        /// </summary>
        public DateTime EndDate { get; private set; }

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
        /// </summary>B
        public int MaxQuerySize { protected get; set; }

        /// <summary>
        /// Used to select posts made by only certain users.  This works by restricting posts below 
        /// the supplied threshold.  E.g. CourseRole.Student will select everyone whereas 
        /// CourseRole.Coordinator will only select course coordinators.
        /// </summary>
        public CourseRole CourseRoleFilter { private get; set; }

        /// <summary>
        /// Used to select only posts made by students in a given course.  Default value is all
        /// courses.
        /// </summary>
        public Course CourseFilter { private get; set; }

        /// <summary>
        /// Comment search token entered by the user
        /// </summary>
        public string CommentFilter { private get; set; }

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
                EventType.LogCommentEvent,
                EventType.HelpfulMarkGivenEvent,
                EventType.SubmitEvent,
            };
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

        public static IEnumerable<int> GetNecessaryEvents()
        {
            return new List<int>
            {
                (int)EventType.AskForHelpEvent,
                (int)EventType.FeedPostEvent
            };
        }

        /// <summary>
        /// returns a list of all possible events that a user can subscribe to
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<EventType> GetAllEvents()
        {
            return GetIdeEvents().Concat(GetSocialEvents());
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
                                , GetNecessaryEvents()//_eventSelectors.Select(e => (int)e) // 4
                                , CourseFilter != null && CourseFilter.ID > 0 ? CourseFilter.ID : 0
                                , CourseRoleFilter.ID
                                , CommentFilter
                                , SubscriptionSubjects.Select(s => s.ID).ToList()
                                , 20
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
                    , GetNecessaryEvents()//_eventSelectors.Select(e => (int)e) // 4
                    , CourseFilter != null && CourseFilter.ID > 0 ? CourseFilter.ID : 0
                    , CourseRoleFilter.ID
                    , CommentFilter
                    , SubscriptionSubjects.Select(s => s.ID).ToList()
                    , numberOfItemsToReturn
                    );

            MinLogId = MaxLogId = null;

            return query.GetAwaiter().GetResult();
        }
    }
}