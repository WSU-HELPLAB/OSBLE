using System;
using System.Data.SqlClient;

using OSBLE.Interfaces;
using OSBLE.Models.Courses;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;
using OSBLE.Models;
using System.Linq;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    [Serializable]
    public class ActivityEvent : IActivityEvent
    {
        // EventLogs table contents
        public int EventLogId { get; set; }
        public virtual int EventTypeId { get; set; }

        public EventType EventType
        {
            get { return (EventType) EventTypeId; }
        }
        public DateTime EventDate { get; protected set; }
        public int SenderId { get; set; }
        public IUser Sender { get; set; }

        // Detailed events table contents
        public int EventId { get; set; }
        public string EventName
        {
            get { return EventType.ToString().ToDisplayText(); }
        }
        public string SolutionName { get; set; }
        public int? CourseId { get; set; }

        // Helper method to efficiently generate TSQL insert scripts
        // Don't use property, since the activities need to be serialized to go across the wire
        // Can't use abstract since Dapper ORM needs to instantiate instances of the class
        public virtual SqlCommand GetInsertCommand()
        {
            return null;
        }

        // for posting
        public bool CanMail { get; set; }
        public bool CanDelete { get; set; }
        public bool CanReply { get; set; }
        public bool CanEdit { get; set; }
        public bool CanVote { get; set; }
        public bool ShowProfilePicture { get; set; }
        public string DisplayTitle { get; set; }
        public bool HideMail { get; set; }

        public ActivityEvent() // NOTE!! This is required by Dapper ORM
        {
            EventDate = DateTime.UtcNow;
        }

        //public ActivityEvent(ActivityEvent a)
        //{
        //    EventLogId = a.EventLogId;
        //    //EventTypeId = a.EventTypeId,
        //    EventDate = a.EventDate;
        //    CourseId = a.CourseId;
        //    DisplayTitle = a.DisplayTitle;
        //    Sender = a.Sender;
        //    SenderId = a.SenderId;
        //    SolutionName = a.SolutionName;
        //}

        public void SetPrivileges(CourseUser currentUser)
        {
            bool anonymous = currentUser.AbstractRole.Anonymized;

            // Anyone can mail anyone but themselves (provided they're not anonimous)
            CanMail = !anonymous && SenderId != currentUser.UserProfileID;

            //get course setting for hiding mail
            using (OSBLEContext db = new OSBLEContext())
            {
                Course course = db.AbstractCourses.Where(ac => ac.ID == currentUser.AbstractCourseID).FirstOrDefault() as Course;
                HideMail = course.HideMail;
            }
            
            // Graders can delete posts, anyone can delete their own posts
            CanDelete = currentUser.AbstractRole.CanGrade || SenderId == currentUser.UserProfileID;

            // Anyone can edit their own posts
            CanEdit = (EventType == EventType.FeedPostEvent || EventType == EventType.LogCommentEvent) && SenderId == currentUser.UserProfileID;

            // Verify that user can make new posts (No one can reply to a reply)
            CanReply = currentUser.AbstractRole.CanSubmit && EventType != EventType.LogCommentEvent;

            // Cannot vote for yourself
            CanVote = currentUser.UserProfileID != SenderId;

            ShowProfilePicture = !anonymous;

            if (Sender != null)
                DisplayTitle = Sender.DisplayName(currentUser);
        }
    }
}
