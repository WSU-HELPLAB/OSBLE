namespace OSBLE.Models.Courses
{
    public class CommunityRole : AbstractRole
    {
        public CommunityRole()
            : base()
        {
        }

        public CommunityRole(string Name, bool CanModify, bool CanSeeAll, bool CanGrade, bool CanUploadFiles)
            : base()
        {
            this.Name = Name;
            this.CanModify = CanModify;
            this.CanSeeAll = CanSeeAll;
            this.CanGrade = CanGrade;
            this.CanSubmit = false;
            this.Anonymized = false;
            this.CanUploadFiles = CanUploadFiles;
        }

        public enum OSBLERoles : int
        {
            //Leader being the first one is used in RosterController so any new roles add at the end.
            //Also note that this 6 is one past the end of CourseRole.
            Leader = 6,
            Participant,
            TrustedCommunityMember
        }
    }
}