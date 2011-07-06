using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Users
{
    public class TeamMember
    {
        [Key]
        [Required]
        public int ID { get; set; }

        /// <summary>
        /// This says whether a team is a team of users or team of teams
        /// if team of users then Users must not be null
        /// if team of teams then Teams must not be null
        /// </summary>
        ///
        [Required]
        public TeamOrUser TeamUser
        {
            get;
            set;
        }

        public int? UserProfileID { get; set; }

        public virtual UserProfile User
        {
            get;
            set;
        }

        public int? TeamID { get; set; }

        /// <summary>
        /// TeamMember cannot contain a team that is its 'parent' as this would cause a circular reference
        /// </summary>
        public virtual Team Team
        {
            get;
            set;
        }

        public bool Contains(UserProfile user)
        {
            if (user.ID == UserProfileID)
            {
                return true;
            }
            else
            {
                return Team.Contains(user);
            }
        }
    }
}