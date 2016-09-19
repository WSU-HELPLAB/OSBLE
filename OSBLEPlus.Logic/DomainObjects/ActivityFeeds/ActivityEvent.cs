using System;
using System.Data.SqlClient;

using OSBLE.Interfaces;
using OSBLE.Models.Courses;
using OSBLEPlus.Logic.DomainObjects.Interface;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Logic.Utility.Lookups;
using OSBLE.Models;
using System.Linq;
using Dapper;
using System.Collections.Generic;

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
        public string EventVisibilityGroups { get; set; }
        public string EventVisibleTo { get; set; }

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
            if (currentUser == null) 
            {
                HideMail = false;
                EventVisibilityGroups = "";
                EventVisibleTo = "";
                CanMail = false;
                CanDelete = false;
                CanEdit = false;
                CanReply = false;
                CanVote = false;
                ShowProfilePicture = false;
                DisplayTitle = "CourseUser";
                return;
            } 

            bool anonymous = currentUser.AbstractRole.Anonymized;

            // Anyone can mail anyone but themselves (provided they're not anonimous)
            CanMail = !anonymous && SenderId != currentUser.UserProfileID;

            //get course setting for hiding mail
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();                    
                    string query = "SELECT * FROM AbstractCourses WHERE ID = @abstractCourseId;";
                    Course result = sqlConnection.Query<Course>(query, new { abstractCourseId = currentUser.AbstractCourseID }).Single();
                    
                    if (null == result)
                    {
                        HideMail = false;
                    }
                    else
                    {
                        HideMail = result.HideMail;
                    }

                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging                
                HideMail = false;
            }

            //set EventVisibilityGroups 
            try
            {   
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();
                    //get the EventVisibilityGroups value from the db
                    string query = "SELECT ISNULL(EventVisibilityGroups, '') FROM EventLogs WHERE Id = @eventLogId;";
                    string result = sqlConnection.Query<string>(query, new { eventLogId = EventLogId }).Single();
                    if (result.Length > 0)
                        EventVisibilityGroups = result;
                    else
                        EventVisibilityGroups = "";
	                
                    sqlConnection.Close();                    
                }
            }
            catch (Exception e)
            {                
                //TODO: handle exception logging                
                EventVisibilityGroups = "";
            }
            
            //setup event visibility
            try
            {
                using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
                {
                    sqlConnection.Open();                    
                    string query = "SELECT ISNULL(EventVisibleTo, '') FROM EventLogs WHERE Id = @eventLogId;";
                    string result = sqlConnection.Query<string>(query, new { eventLogId = EventLogId }).Single();
                    
                    if (result.Length > 0)
                        EventVisibleTo = result;                    
                    else                    
                        EventVisibleTo = "";
                    
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                //TODO: handle exception logging
                EventVisibleTo = "";
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
