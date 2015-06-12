using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSBLE.Models.Assignments
{
    /// <summary>
    /// Settings for critical review assignment types.
    /// Note that I'm using bit masking to store all settings inside one variable.  As such,
    /// be sure to make each setting's integer value a power of 2.
    /// </summary>
    [Flags]
    public enum ReviewSettings : byte
    {
        AllowDownloadAfterPublish = 2,
        AnonymizeAuthor = 4,
        AnonymizeComments = 8,
        AnonymizeCommentsAfterPublish = 16
    }

    public class CriticalReviewSettings : IModelBuilderExtender
    {
        [Key]
        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        /// <summary>
        /// Used for raw DB access.  Do not use if you're a programmer.  Instead, use the various 
        /// accessor methods.
        /// </summary>
        [Required]
        public byte ReviewSettings { get; set; }

        public CriticalReviewSettings(CriticalReviewSettings other)
        {
            if (other == null)
            {
                return;
            }

            this.AssignmentID = other.AssignmentID;
            this.AnonymizeAuthor = other.AnonymizeAuthor;
            this.AnonymizeCommentsAfterPublish = other.AnonymizeCommentsAfterPublish;
            this.AnonymizeComments = other.AnonymizeComments;
            this.AllowDownloadAfterPublish = other.AllowDownloadAfterPublish;
        }

        public CriticalReviewSettings()
        {
            AnonymizeAuthor = false;
            AnonymizeCommentsAfterPublish = false;
            AnonymizeComments = false;
            AllowDownloadAfterPublish = false;
        }
        [Display(Name = "Anonymize Author to Reviewers")]
        [NotMapped]
        public bool AnonymizeAuthor
        {
            get
            {
                return HasSetting(Assignments.ReviewSettings.AnonymizeAuthor);
            }
            set
            {
                if (value == true)
                {
                    AddSetting(Assignments.ReviewSettings.AnonymizeAuthor);
                }
                else
                {
                    RemoveSetting(Assignments.ReviewSettings.AnonymizeAuthor);
                }
            }
        }

        [Display(Name = "Anonymize comments during review")]
        [NotMapped]
        public bool AnonymizeComments
        {
            get
            {
                return HasSetting(Assignments.ReviewSettings.AnonymizeComments);
            }
            set
            {
                if (value == true)
                {
                    AddSetting(Assignments.ReviewSettings.AnonymizeComments);
                }
                else
                {
                    RemoveSetting(Assignments.ReviewSettings.AnonymizeComments);
                }
            }
        }

        [Display(Name = "Allow reviewers to access the reviewed document after publish")]
        [NotMapped]
        public bool AllowDownloadAfterPublish
        {
            get
            {
                return HasSetting(Assignments.ReviewSettings.AllowDownloadAfterPublish);
            }
            set
            {
                if (value == true)
                {
                    AddSetting(Assignments.ReviewSettings.AllowDownloadAfterPublish);
                }
                else
                {
                    RemoveSetting(Assignments.ReviewSettings.AllowDownloadAfterPublish);
                }
            }
        }

        [Display(Name = "Anonymize comments after review")]
        [NotMapped]
        public bool AnonymizeCommentsAfterPublish
        {
            get
            {
                return HasSetting(Assignments.ReviewSettings.AnonymizeCommentsAfterPublish);
            }
            set
            {
                if (value == true)
                {
                    AddSetting(Assignments.ReviewSettings.AnonymizeCommentsAfterPublish);
                }
                else
                {
                    RemoveSetting(Assignments.ReviewSettings.AnonymizeCommentsAfterPublish);
                }
            }
        }

        /// <summary>
        /// Returns true if the discussion has the specified anonymity setting
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        protected bool HasSetting(ReviewSettings setting)
        {
            int result = ReviewSettings & (byte)setting;
            return result == (int)setting;
        }

        protected void AddSetting(ReviewSettings setting)
        {
            ReviewSettings = (byte)(ReviewSettings | (byte)setting);
        }

        protected void RemoveSetting(ReviewSettings level)
        {
            //~ is a bitwise not in c#
            //Doing a bitwise AND on a NOTed level should result in the level being removed
            ReviewSettings = (byte)(ReviewSettings & (~(byte)level));
        }


        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CriticalReviewSettings>()
                .HasRequired(crs => crs.Assignment)
                .WithOptional(a => a.CriticalReviewSettings)
                .WillCascadeOnDelete(true);
        }
    }
}
