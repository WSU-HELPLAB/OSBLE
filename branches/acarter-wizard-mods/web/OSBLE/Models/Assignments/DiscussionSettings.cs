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
    public enum AnonymityLevels : byte 
    { 
        AnonymousPosts = 1,
        AnonymousReplies = 2,
        AnonymousRoles = 4
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
        [Display(Name = "Minimum length for first post")]
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
        [Display(Name = "Allow anonymous posting")]
        public bool HasAnonymousPosts
        {
            get
            {
                return HasAnonymityLevel(AnonymityLevels.AnonymousPosts);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(AnonymityLevels.AnonymousPosts);
                }
                else
                {
                    RemoveAnonymityLevel(AnonymityLevels.AnonymousPosts);
                }
            }
        }

        /// <summary>
        /// Returns true if replies in the discussion are marked anonymous
        /// </summary>
        [NotMapped]
        [Display(Name="Allow anonymous replies to posts")]
        public bool HasAnonymousReplies
        {
            get
            {
                return HasAnonymityLevel(AnonymityLevels.AnonymousReplies);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(AnonymityLevels.AnonymousReplies);
                }
                else
                {
                    RemoveAnonymityLevel(AnonymityLevels.AnonymousReplies);
                }
            }
        }

        /// <summary>
        /// Returns true if user roles are hidden in discussions
        /// </summary>
        [NotMapped]
        [Display(Name = "Anonymize Roles")]
        public bool HasAnonymousRoles
        {
            get
            {
                return HasAnonymityLevel(AnonymityLevels.AnonymousRoles);
            }
            set
            {
                if (value == true)
                {
                    AddAnonymityLevel(AnonymityLevels.AnonymousRoles);
                }
                else
                {
                    RemoveAnonymityLevel(AnonymityLevels.AnonymousRoles);
                }
            }
        }

        /// <summary>
        /// Returns true if the discussion has the specified anonymity setting
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        protected bool HasAnonymityLevel(AnonymityLevels level)
        {
            int result = AnonymitySettings & (byte)level;
            return result == (int)level;
        }

        protected void AddAnonymityLevel(AnonymityLevels level)
        {
            AnonymitySettings = (byte)(AnonymitySettings | (byte)level);
        }

        protected void RemoveAnonymityLevel(AnonymityLevels level)
        {
            //~ is a bitwise not in c#
            //Doing a bitwise AND on a NOTed level should result in the level being removed
            AnonymitySettings = (byte)(AnonymitySettings & (~(byte)level));
        }

        #endregion
    }
}