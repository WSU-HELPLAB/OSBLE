using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.HomePage;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlTypes;

namespace OSBLE.Models.Assignments
{
    /// <summary>
    /// Levels of anonymity available for discussion (and potentially other) assignment types.
    /// Note that I'm using bit masking to store all settings inside one variable.  As such,
    /// be sure to make each setting's integer value a power of 2.
    /// </summary>
    [Flags]
    public enum DiscussionSettings : byte
    {
        AnonymizeStudentsToStudents = 1,
        AnonymizeModeratorsToStudents = 2,
        AnonymizeInstructorsToStudents = 4,
        AnonymizeStudentsToModerators = 8,
        HideCourseRoles = 16,
        RequiresPostBeforeView = 32,
        TAsCanPostToAllDiscussions = 64,
        EmailInstructorOnModeratorPost = 128

    };

    public class DiscussionSetting : IModelBuilderExtender
    {
        [Key]
        [Required]
        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        /// <summary>
        /// Used for raw DB access.  Do not use if you're a programmer.  Instead, use the various 
        /// accessor methods (e.g. IsAnonymous, HasAnonymousPosts, etc.)
        /// </summary>
        [Required]
        public byte AnonymitySettings { get; set; }

        private int? associatedEventID;

        public int? AssociatedEventID
        {
            get
            {
                return associatedEventID;
            }
            set
            {
                associatedEventID = value;
            }
            
        }

        [ForeignKey("AssociatedEventID")]
        public virtual Event AssociatedEvent { get; set; }

        [Required(ErrorMessage = "Please specify when the first post should be due")]
        [Display(Name = "Due date for initial post")]
        [DataType(DataType.Date)]
        public DateTime InitialPostDueDate { get; set; }

        [NotMapped]
        [DataType(DataType.Time)]
        public DateTime InitialPostDueDueTime
        {
            get
            {
                return InitialPostDueDate;
            }
            set
            {
                //first, zero out the date's time component
                InitialPostDueDate = DateTime.Parse(InitialPostDueDate.ToShortDateString());

                InitialPostDueDate = InitialPostDueDate.AddHours(value.Hour);
                InitialPostDueDate = InitialPostDueDate.AddMinutes(value.Minute);
            }
        }

        [Required(ErrorMessage = "Please specify the minimum first post length, enter 0 if you do not want to require a min length")]
        [Display(Name = "Minimum length for first post (in words)")]
        public int MinimumFirstPostLength { get; set; }

        [Required(ErrorMessage = "Please specify the maximum first post length, enter 0 if you do not want to require a max length")]
        [Display(Name = "Maximum length for first post (in words)")]
        public int MaximumFirstPostLength { get; set; }

        public DiscussionSetting()
        {
            AnonymitySettings = 0;
            InitialPostDueDate = DateTime.UtcNow;
            MinimumFirstPostLength = 0;
        }

        public DiscussionSetting(DiscussionSetting other)
        {
            if (other == null)
            {
                // need to set this to the min value otherwise there is a conversion error on null
                this.InitialPostDueDate = (DateTime) SqlDateTime.MinValue;
                this.InitialPostDueDueTime = (DateTime) SqlDateTime.MinValue;
                return;
            }
            this.AnonymitySettings = other.AnonymitySettings;
            this.AssignmentID = other.AssignmentID;
            this.InitialPostDueDate = other.InitialPostDueDate;
            this.MinimumFirstPostLength = other.MinimumFirstPostLength;
            this.associatedEventID = null;
        }

        #region anonymity settings
        
        /// <summary>
        /// Returns true if the discussion is anonymous.
        /// </summary>
        public bool IsAnonymous
        {
            get
            {
                return AnonymitySettings == 0;
            }
        }

        [NotMapped]
        [Display(Name = "Email instructors on moderator post")]
        public bool WillEmailInstructorsOnModeratorPost
        {
            get
            {
                return HasAnonymityLevel(DiscussionSettings.EmailInstructorOnModeratorPost);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(DiscussionSettings.EmailInstructorOnModeratorPost);
                }
                else
                {
                    RemoveAnonymityLevel(DiscussionSettings.EmailInstructorOnModeratorPost);
                }
            }
        }

        /// <summary>
        /// Returns true if roles are to be hidden
        /// </summary>
        [NotMapped]
        [Display(Name = "Hide roles in discussion")]
        public bool HasHiddenRoles
        {
            get
            {
                return HasAnonymityLevel(DiscussionSettings.HideCourseRoles);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(DiscussionSettings.HideCourseRoles);
                }
                else
                {
                    RemoveAnonymityLevel(DiscussionSettings.HideCourseRoles);
                }
            }
        }

        /// <summary>
        /// Returns true if students posts in the discussion are to be anonymous to other students
        /// </summary>
        [NotMapped]
        [Display(Name = "Anonymize names of students in the discussion to other students")]
        public bool HasAnonymousStudentsToStudents
        {
            get
            {
                return HasAnonymityLevel(DiscussionSettings.AnonymizeStudentsToStudents);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(DiscussionSettings.AnonymizeStudentsToStudents);
                }
                else
                {
                    RemoveAnonymityLevel(DiscussionSettings.AnonymizeStudentsToStudents);
                }
            }
        }

        /// <summary>
        /// Returns true if Moderators posts in the discussion are to be anonymous to students
        /// </summary>
        [NotMapped]
        [Display(Name = "Anonymize names of moderators in the discussion to other students")]
        public bool HasAnonymousModeratorsToStudents
        {
            get
            {
                return HasAnonymityLevel(DiscussionSettings.AnonymizeModeratorsToStudents);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(DiscussionSettings.AnonymizeModeratorsToStudents);
                }
                else
                {
                    RemoveAnonymityLevel(DiscussionSettings.AnonymizeModeratorsToStudents);
                }
            }
        }

        /// <summary>
        /// Returns true if instructor posts in the discussion are to be anonymous to students
        /// </summary>
        [NotMapped]
        [Display(Name = "Anonymize names of instructors in the discussion to students")]
        public bool HasAnonymousInstructorsToStudents
        {
            get
            {
                return HasAnonymityLevel(DiscussionSettings.AnonymizeInstructorsToStudents);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(DiscussionSettings.AnonymizeInstructorsToStudents);
                }
                else
                {
                    RemoveAnonymityLevel(DiscussionSettings.AnonymizeInstructorsToStudents);
                }
            }
        }

        /// <summary>
        /// Returns true if student posts in the discussion are to be anonymous to moderators
        /// </summary>
        [NotMapped]
        [Display(Name = "Anonymize names of students in the discussion to moderators")]
        public bool HasAnonymousStudentsToModerators
        {
            get
            {
                return HasAnonymityLevel(DiscussionSettings.AnonymizeStudentsToModerators);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(DiscussionSettings.AnonymizeStudentsToModerators);
                }
                else
                {
                    RemoveAnonymityLevel(DiscussionSettings.AnonymizeStudentsToModerators);
                }
            }
        }

        /// <summary>
        /// Returns true if the assignment requires that students must first submit a post before
        /// they can view the posts of others
        /// </summary>
        [NotMapped]
        [Display(Name = "Students must make initial post before they can view the posts of others")]
        public bool RequiresPostBeforeView
        {
            get
            {
                return HasAnonymityLevel(DiscussionSettings.RequiresPostBeforeView);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(DiscussionSettings.RequiresPostBeforeView);
                }
                else
                {
                    RemoveAnonymityLevel(DiscussionSettings.RequiresPostBeforeView);
                }
            }
        }

        /// <summary>
        ///Returns true if TAs are allowed to post to all discussion assignments
        /// </summary>
        [NotMapped]
        [Display(Name = "TAs can participate in all discussions")]
        public bool TAsCanPostToAllDiscussions
        {
            get
            {
                return HasAnonymityLevel(DiscussionSettings.TAsCanPostToAllDiscussions);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(DiscussionSettings.TAsCanPostToAllDiscussions);
                }
                else
                {
                    RemoveAnonymityLevel(DiscussionSettings.TAsCanPostToAllDiscussions);
                }
            }
        }


        /// <summary>
        /// Returns true if the discussion has the specified anonymity setting
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        protected bool HasAnonymityLevel(DiscussionSettings level)
        {
            int result = AnonymitySettings & (byte)level;
            return result == (int)level;
        }

        protected void AddAnonymityLevel(DiscussionSettings level)
        {
            AnonymitySettings = (byte)(AnonymitySettings | (byte)level);
        }

        protected void RemoveAnonymityLevel(DiscussionSettings level)
        {
            //~ is a bitwise not in c#
            //Doing a bitwise AND on a NOTed level should result in the level being removed
            AnonymitySettings = (byte)(AnonymitySettings & (~(byte)level));
        }

        #endregion

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscussionSetting>()
                .HasRequired(ds => ds.Assignment)
                .WithOptional(a => a.DiscussionSettings)
                .WillCascadeOnDelete(true);
        }

    }
}