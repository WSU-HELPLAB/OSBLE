﻿using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using OSBLE.Models.Courses;

namespace OSBLE.Models.Users
{
    public class Mail : IModelBuilderExtender
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [Required]
        public int ThreadID { get; set; }

        [Required]
        public int ContextID { get; set; }

        public virtual AbstractCourse Context { get; set; }

        [Required]
        public int FromUserProfileID { get; set; }

        public virtual UserProfile FromUserProfile { get; set; }

        [Required]
        public int ToUserProfileID { get; set; }

        public virtual UserProfile ToUserProfile { get; set; }

        [Required]
        public bool Read { get; set; }

        [Required]
        public DateTime Posted { get; set; }

        [Required]
        [AllowHtml]
        [StringLength(100)]
        public string Subject { get; set; }

        [Required]
        [AllowHtml]
        public string Message { get; set; }

        public Mail()
            : base()
        {
            Posted = DateTime.Now;
        }


        public bool DeleteFromOutbox { get; set; }
        public bool DeleteFromInbox { get; set; }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Mail>()
                .HasRequired(n => n.ToUserProfile)
                .WithMany()
                .WillCascadeOnDelete(false);
        }
    }
}