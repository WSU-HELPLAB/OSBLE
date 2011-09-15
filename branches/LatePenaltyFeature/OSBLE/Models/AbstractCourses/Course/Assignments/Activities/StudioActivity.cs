namespace OSBLE.Models.Assignments.Activities
{
    public abstract class StudioActivity : AbstractAssignmentActivity
    {
        // Late Policy
        public StudioActivity ShallowCopy()
        {
            return this.MemberwiseClone() as StudioActivity;
        }
    }
}