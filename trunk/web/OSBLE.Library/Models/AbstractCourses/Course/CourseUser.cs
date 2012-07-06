using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Users;
using System.Collections;
using System.Collections.Generic;
using OSBLE.Models.Assignments;
using System;

namespace OSBLE.Models.Courses
{
    public class CourseUser
    {
        [Key]
        [Required]
        [Column(Order = 0)]
        public int ID { get; set; }

        [Required]
        [Column(Order = 1)]
        public int UserProfileID { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        [Required]
        [Column(Order = 2)]
        public int AbstractCourseID { get; set; }

        public virtual AbstractCourse AbstractCourse { get; set; }

        [Required]
        public int AbstractRoleID { get; set; }

        public virtual AbstractRole AbstractRole { get; set; }

        [Required]
        public int Section { get; set; }

        public bool Hidden { get; set; }

        public virtual IList<TeamMember> TeamMemberships { get; set; }

        public CourseUser()
            : base()
        {
            Hidden = false;
            TeamMemberships = new List<TeamMember>();
        }

        /// <summary>
        /// Note this copy constructor doesn't copy virtual members.
        /// </summary>
        /// <param name="copyUser"></param>
        public CourseUser(CourseUser copyUser)
            : this()
        {
            this.AbstractCourseID = copyUser.AbstractCourseID;
            this.AbstractRoleID = copyUser.AbstractRoleID;
            this.Hidden = copyUser.Hidden;
            this.ID = copyUser.ID;
            this.Section = copyUser.Section;
            this.UserProfileID = copyUser.UserProfileID;
        }

        [Obsolete("Use non-obsolete DisplayName() method")]
        public string DisplayName(string separator = ", ")
        {
            return this.UserProfile.LastName + separator + this.UserProfile.FirstName;
        }

        [Obsolete("Use non-obsolete DisplayName() method")]
        public string DisplayName(AbstractRole viewerRole, string separator = ", ")
        {
            // not observer
            if (viewerRole.Anonymized == false)
            {
                return this.UserProfile.LastName + separator + this.UserProfile.FirstName;
            }
            else
            {
                // will want to change this.ID to this.ID % with course size
                return "Anonymous " + this.ID;
            }
        }

        [Obsolete("Use non-obsolete DisplayName() method")]
        public string DisplayNameFirstLast(AbstractRole viewerRole)
        {
            if (viewerRole.Anonymized) // observer
            {
                // will want to change this.ID to this.ID % with course size
                return "Anonymous " + this.ID;
            }
            else
            {
                return this.UserProfile.FirstName + " " + this.UserProfile.LastName;
            }
        }




        /// <summary>
        /// This is the only function that should be used to display a courseusers name (with the exception of those that use this). By default it displays the name as "LastName, FirstName" 
        /// </summary>
        /// <param name="AbstractRoleId">The AbstroleRoleId of the current user who will see this name</param>
        /// <param name="LastThenFirst">This is an optional boolean parameter that should be sent in as true when you want names displayed in "FirstName, LastName" format.</param>
        /// <param name="AssignmentHasAnonymousPosts">This is an optional boolean parameter that should be sent in when DisplayName is used within a specific assignment scenario (to mask users if anonymous settings turned on) Simply pass in <see cref="Assignment.DiscussionSettings.HasAnonymousPosts"/></param>
        /// <returns></returns>
        public string DisplayName(int AbstractRoleId, bool? FirstThenLast = false)
        {
            return UserProfile.DisplayName(AbstractRoleId, FirstThenLast, false);
        }

        public string DisplayName(int AbstractRoleId, DiscussionSetting discussionSetting, bool? FirstThenLast = false)
        {
            bool anonName = false;
            if (discussionSetting != null && discussionSetting.HasAnonymousPosts)
            {
                anonName = true;
            }
            return UserProfile.DisplayName(AbstractRoleId, FirstThenLast, anonName);
        }

        /// <summary>
        /// This function displays the users name as "(RoleAbbreviation) LastName, FirstName" i.e. "(TA) Morgan, John"
        /// </summary>
        /// <param name="AbstractRoleId">The AbstroleRoleId of the current user who will see this name</param>
        /// <param name="LastThenFirst">This is an optional boolean parameter that should be sent in as true when you want names displayed in "FirstName, LastName" format.</param>
        /// <param name="AssignmentHasAnonymousPosts">This is an optional boolean parameter that should be sent in when DisplayName is used within a specific assignment scenario (to mask users if anonymous settings turned on) Simply pass in <see cref="Assignment.DiscussionSettings.HasAnonymousPosts"/></param>
        /// <returns></returns>
        public string DisplayNameWithRole(int AbstractRoleId, bool? FirstThenLast = false)
        {
            string roleAbbreviation = "";
            switch (AbstractRoleID)
            {
                case (int)CourseRole.CourseRoles.Instructor:
                    roleAbbreviation = "I";
                    break;
                case (int)CourseRole.CourseRoles.Moderator:
                    roleAbbreviation = "M";
                    break;
                case (int)CourseRole.CourseRoles.Observer:
                    roleAbbreviation = "O";
                    break;
                case (int)CourseRole.CourseRoles.Student:
                    roleAbbreviation = "S";
                    break;
                case (int)CourseRole.CourseRoles.TA:
                    roleAbbreviation = "TA";
                    break;
            }
            return string.Format("({0}) {1}", roleAbbreviation, UserProfile.DisplayName(AbstractRoleId, FirstThenLast, false));
        }


        public string DisplayNameWithRole(int AbstractRoleId, DiscussionSetting discussionSetting, bool? FirstThenLast = null)
        {
            string returnValue = "";
            bool anonName = false;
            if (discussionSetting != null && discussionSetting.HasAnonymousPosts)
            {
                anonName = true;
            }
            if (discussionSetting != null && discussionSetting.HasAnonymousRoles)
            {
                returnValue = UserProfile.DisplayName(AbstractRoleId, FirstThenLast, anonName);
            }
            else
            {
                string roleAbbreviation = "";
                switch (AbstractRoleID)
                {
                    case (int)CourseRole.CourseRoles.Instructor:
                        roleAbbreviation = "I";
                        break;
                    case (int)CourseRole.CourseRoles.Moderator:
                        roleAbbreviation = "M";
                        break;
                    case (int)CourseRole.CourseRoles.Observer:
                        roleAbbreviation = "O";
                        break;
                    case (int)CourseRole.CourseRoles.Student:
                        roleAbbreviation = "S";
                        break;
                    case (int)CourseRole.CourseRoles.TA:
                        roleAbbreviation = "TA";
                        break;
                }
                returnValue = string.Format("({0}) {1}", roleAbbreviation, UserProfile.DisplayName(AbstractRoleId, FirstThenLast, anonName));
            }
            return returnValue;
        }
    }
}