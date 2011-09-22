using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace OSBLE.Models.Users
{
    [KnownType(typeof(OldTeamMember))]
    [KnownType(typeof(UserMember))]
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