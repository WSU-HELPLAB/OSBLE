using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Courses;
using OSBLE.Models.Assignments;

namespace OSBLE.Models.DiscussionAssignment
{
    public class DiscussionPost
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        [Required]
        public int CourseUserID { get; set; }
        public virtual CourseUser CourseUser { get; set; }

        [Required]
        public string Content { get; set; }

        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        public virtual ICollection<DiscussionPost> Replies { get; set; }

        public string DisplayName { get; set; }

        public bool ShowProfilePicture { get; set; }

        public bool IsReply { get; set; }

        [Required]
        public int DiscussionTeamID { get; set; }
        [ForeignKey("DiscussionTeamID")]
        public virtual DiscussionTeam DiscussionTeam { get; set; }

        public DiscussionPost()
            : base()
        {
            Posted = DateTime.Now;
            ShowProfilePicture = false;
            IsReply = false;
        }
    }
}