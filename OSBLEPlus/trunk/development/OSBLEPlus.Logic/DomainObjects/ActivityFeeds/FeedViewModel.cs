using System;
using System.Collections.Generic;
using OSBLE.Models.Courses;
using OSBLEPlus.Logic.Utility.Lookups;

namespace OSBLEPlus.Logic.DomainObjects.ActivityFeeds
{
    public class FeedViewModel
    {
        public List<AggregateFeedItem> Feed { get; set; }
        public DateTime LastPollDate { get; set; }
        public int LastLogId { get; set; }
        public int SingleUserId { get; set; }
        public List<EventType> EventFilterOptions { get; set; }
        public List<EventType> UserEventFilterOptions { get; set; }
        //public List<ErrorType> ErrorTypes { get; set; }
        public List<Course> Courses { get; set; }
        public List<CourseRole.CourseRoles> CourseRoles { get; set; }
        //public ErrorType SelectedErrorType { get; set; }
        public CourseRole.CourseRoles SelectedCourseRole { get; set; }
        public int SelectedCourseId { get; set; }
        public List<string> RecentUserErrors { get; set; }
        //public List<UserBuildErrorsByType> RecentClassErrors { get; set; }
        public string Keyword { get; set; }

        public FeedViewModel()
        {
            Feed = new List<AggregateFeedItem>();
            SingleUserId = -1;
            LastLogId = -1;
            LastPollDate = DateTime.UtcNow;
            EventFilterOptions = new List<EventType>();
            UserEventFilterOptions = new List<EventType>();
            //SelectedErrorType = new ErrorType();
            RecentUserErrors = new List<string>();
            //RecentClassErrors = new List<UserBuildErrorsByType>();
            CourseRoles = new List<CourseRole.CourseRoles>();
            Courses = new List<Course>();
        }
    }
}