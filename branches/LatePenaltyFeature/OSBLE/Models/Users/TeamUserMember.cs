using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Users
{
    public abstract class TeamUserMember
    {
        [Key]
        [Required]
        public int ID { get; set; }

        public abstract bool Contains(UserProfile user);

        public string Name
        {
            get
            {
                return GetName();
            }
        }

        public abstract string GetName();
    }
}