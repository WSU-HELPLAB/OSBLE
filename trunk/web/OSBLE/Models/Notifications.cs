﻿using System;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{
    public class Notifications
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int RecipientID { get; set; }

        public virtual UserProfile Recipient { get; set; }

        public int? SenderID { get; set; }

        public virtual UserProfile Sender { get; set; }

        public int? CourseID { get; set; }

        public virtual Course Course { get; set; }

        public bool Read { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        // Multiple items can be used in a notification, including Assignments, Grades, etc.
        // They are defined in the Types class at the bottom of this model.
        public string ItemType { get; set; }

        public int ItemID { get; set; }

        [NotMapped]
        public string Message { get; set; }

        public Notifications()
            : base()
        {
            Posted = DateTime.Now;
            Read = false;
        }

        public static class Types {
            public const string Mail = "Mail";
        }

    }
}