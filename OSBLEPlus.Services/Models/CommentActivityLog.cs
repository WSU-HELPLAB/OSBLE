using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OSBLE.Models.Users;
using OSBLEPlus.Logic.DomainObjects.ActivityFeeds;

namespace OSBLEPlus.Services.Models
{
    public class CommentActivityLog
    {
        [Key]
        [Required]
        public int Id { get; set; }

        [Required]
        public int UserProfileId { get; set; }

        [ForeignKey("UserProfileId")]
        public virtual UserProfile UserProfile { get; set; }

        [Required]
        public int LogCommentEventId { get; set; }

        [ForeignKey("LogCommentEventId")]
        public virtual LogCommentEvent LogCommentEvent { get; set; }
    }
}