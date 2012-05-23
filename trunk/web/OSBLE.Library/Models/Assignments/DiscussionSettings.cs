using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

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
        AnonymousPosts = 1,
        AnonymousRoles = 2,
        RequiresPostBeforeView = 4,
    };

    public class DiscussionSetting
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

        [Required]
        [Display(Name = "Minimum length for first post (in characters)")]
        public int MinimumFirstPostLength { get; set; }

        public DiscussionSetting()
        {
            AnonymitySettings = 0;
            InitialPostDueDate = DateTime.Now;
            MinimumFirstPostLength = 0;
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

        /// <summary>
        /// Returns true if posts in the discussion are anonymous
        /// </summary>
        [NotMapped]
        [Display(Name = "Anonymize names of participants in the discussion")]
        public bool HasAnonymousPosts
        {
            get
            {
                return HasAnonymityLevel(DiscussionSettings.AnonymousPosts);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(DiscussionSettings.AnonymousPosts);
                }
                else
                {
                    RemoveAnonymityLevel(DiscussionSettings.AnonymousPosts);
                }
            }
        }

        /// <summary>
        /// Returns true if user roles are hidden in discussions
        /// </summary>
        [NotMapped]
        [Display(Name = "Hide roles of discussion participants")]
        public bool HasAnonymousRoles
        {
            get
            {
                return HasAnonymityLevel(DiscussionSettings.AnonymousRoles);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(DiscussionSettings.AnonymousRoles);
                }
                else
                {
                    RemoveAnonymityLevel(DiscussionSettings.AnonymousRoles);
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
    }
}