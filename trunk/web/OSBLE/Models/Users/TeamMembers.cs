using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Users
{
    public enum TeamsOrUsers
    {
        Team,
        User
    }

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
        public TeamsOrUsers TeamUser
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

        public virtual Team Team
        {
            get;
            set;
        }
    }
}