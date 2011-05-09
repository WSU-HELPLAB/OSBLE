using System;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Notifications
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int UserProfileID { get; set; }

        public UserProfile UserProfile { get; set; }

        public bool Read { get; set; }

        public DateTime Posted { get; set; }

        public string TableName { get; set; }

        public int TableID { get; set; }
    }
}