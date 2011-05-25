using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models
{

    public class CommunityRole : AbstractRole
    {
        public CommunityRole()
            : base()
        {
        }

        public CommunityRole(string Name, bool CanModify, bool CanSeeAll, bool CanGrade)
            : base()
        {
            this.Name = Name;
            this.CanModify = CanModify;
            this.CanSeeAll = CanSeeAll;
            this.CanGrade = CanGrade;
            this.CanSubmit = false;
            this.Anonymized = false;
        }


        public enum OSBLERoles : int
        {
            Leader = 6,
            Participant
        }

    }
}