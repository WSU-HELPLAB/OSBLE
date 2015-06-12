namespace OSBLE.Models.Assignments.Activities
{
    public abstract class IssueVotingActivity : StudioActivity
    {
        //Anonymity of issue voting
        // Need reference to assignment activity

        // Requires a PeerReview
        public virtual AbstractAssignmentActivity PreviousActivity { get; set; }

        public enum SetGrade
        {
            PercentOfIssues,
            PercentAgreementWModerator,
            Manually
        };

        public SetGrade Setgrade { get; set; }
    }
}