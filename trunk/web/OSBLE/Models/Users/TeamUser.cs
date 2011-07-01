namespace OSBLE.Models.Users
{
    public enum TeamOrUser
    {
        Team,
        User
    }

    public class TeamUser
    {
        public TeamOrUser TeamOrUser { get; set; }

        public Team Team { get; set; }

        public UserProfile UserProfile { get; set; }
    }
}