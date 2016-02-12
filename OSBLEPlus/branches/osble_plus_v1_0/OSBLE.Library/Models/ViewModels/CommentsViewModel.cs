using System;

namespace OSBLE.Models.ViewModels
{
    public class CommentsViewModel
    {
        /// <summary>
        /// LogCommentEventId
        /// </summary>
        public int EventLogId { get; set; }
        public string CourseName { get; set; }
        public string ProfileUrl { get; set; }
        public string FirstAndLastName { get; set; }
        public DateTime UtcEventDate { get; set; }
        public Int64 UtcUnixDate
        {
            get
            {
                return Convert.ToInt64((UtcEventDate - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds);
            }
        }
        public string UtcEventDateStr
        {
            get
            {
                return string.Format("{0} (UTC)", UtcEventDate.ToString("MM/dd @ hh:mmtt"));
            }
        }
        public string Content { get; set; }
        public bool DisplayHelpfulMarkLink { get; set; }
        public string MarkHelpfulUrl { get; set; }
        public int MarkHelpfulCount { get; set; }
    }
}