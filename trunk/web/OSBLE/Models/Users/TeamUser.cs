using System.ComponentModel.DataAnnotations;
using System.Linq;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Models.Users
{
    public enum TeamOrUser
    {
        Team,
        User
    }

    /// <summary>
    /// This is a temporary class
    /// </summary>
    public class TeamUser
    {
        [Key]
        [Required]
        public int ID { get; set; }

        /// <summary>
        /// This must be safely castable to TeamOrUser enum
        /// </summary>
        [Required]
        public int TeamOrUser { get; set; }

        public int? TeamID { get; set; }

        public virtual Team Team { get; set; }

        public int? UserProfileID { get; set; }

        public virtual UserProfile UserProfile { get; set; }

        /// <summary>
        /// This will create the TeamUser with all the need stuff being set
        /// </summary>
        /// <param name="user"></param>
        public TeamUser(Team team)
        {
            TeamOrUser = (int)Users.TeamOrUser.Team;
            Team = team;
            TeamID = team.ID;

            UserProfile = null;
            UserProfileID = null;
        }

        /// <summary>
        /// This will create the TeamUser with all the need stuff being set
        /// </summary>
        /// <param name="user"></param>
        public TeamUser(UserProfile user)
        {
            TeamOrUser = (int)Users.TeamOrUser.User;
            UserProfile = user;
            UserProfileID = user.ID;

            Team = null;
            TeamID = null;
        }

        //Only to be used by DB
        public TeamUser()
        {
        }

        public bool Contains(UserProfile user)
        {
            if (UserProfileID == user.ID)
            {
                return true;
            }
            else
            {
                return Team.Contains(user);
            }
        }

        public static TeamUser GetTeamUser(StudioActivity activity, UserProfile user)
        {
            var teamUser = (from c in activity.TeamUsers where c.Contains(user) == true select c).FirstOrDefault();

            return teamUser;
        }
    }
}