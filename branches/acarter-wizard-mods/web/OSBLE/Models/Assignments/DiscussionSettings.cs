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

        [Required]
        public DateTime InitialPostDueDate { get; set; }

        [Required]
        public int MinimumFirstPostLength { get; set; }

        public DiscussionSetting()
        {
            AnonymitySettings = 0;
            InitialPostDueDate = DateTime.MaxValue;
            MinimumFirstPostLength = 0;
        }

        public void AddAnonymityLevel(AnonymityLevels level)
        {
            AnonymitySettings = (byte)(AnonymitySettings | (byte)level);
        }

        public void RemoveAnonymityLevel(AnonymityLevels level)
        {
            //~ is a bitwise not in c#
            //Doing a bitwise AND on a NOTed level should result in the level being removed
            AnonymitySettings = (byte)(AnonymitySettings & (~(byte)level) );
        }

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
        public bool HasAnonymousPosts
        {
            get
            {
                return HasAnonymityLevel(AnonymityLevels.AnonymousPosts);
            }
        }

        /// <summary>
        /// Returns true if replies in the discussion are marked anonymous
        /// </summary>
        public bool HasAnonymousReplies
        {
            get
            {
                return HasAnonymityLevel(AnonymityLevels.AnonymousReplies);
            }
        }

        /// <summary>
        /// Returns true if user roles are hidden in discussions
        /// </summary>
        public bool HasAnonymousRoles
        {
            get
            {
                return HasAnonymityLevel(AnonymityLevels.AnonymousRoles);
            }
        }

        /// <summary>
        /// Returns true if the discussion has the specified anonymity setting
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public bool HasAnonymityLevel(AnonymityLevels level)
        {
            int result = AnonymitySettings & (byte)level;
            return result == (int)level;
        }
    }
}