namespace OSBLE.Models.Courses
{
    public class CourseRole : AbstractRole
    {
        public CourseRole()
            : base()
        {
        }

        public CourseRole(string Name, bool CanModify, bool CanSeeAll, bool CanGrade, bool CanSubmit, bool Anonymized)
            : base()
        {
            this.Name = Name;
            this.CanModify = CanModify;
            this.CanSeeAll = CanSeeAll;
            this.CanGrade = CanGrade;
            this.CanSubmit = CanSubmit;
            this.Anonymized = Anonymized;
        }

        public enum OSBLERoles : int
        {
            Instructor = 1,
            TA,
            Student,
            Moderator,
            Observer
        }
    }
}