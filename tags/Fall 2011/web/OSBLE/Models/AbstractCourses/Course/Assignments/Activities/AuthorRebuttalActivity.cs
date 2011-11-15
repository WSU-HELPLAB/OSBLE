using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Assignments.Activities
{
    public class AuthorRebuttalActivity : StudioActivity
    {
        //Need Link to Previous activity
        // Requires a PeerReview
        public virtual AbstractAssignmentActivity PreviousActivity { get; set; }

        public enum PresentationOptions
        {
            PresentAllIssuesLogged,
            PresentIssuesXLogged,
            PresentIssuesXPercentLogged,
            PresentOnlyModeratorVoted
        };

        public PresentationOptions Presentation { get; set; }

        public int RequiredNumberOfIssues { get; set; }

        public int RequiredNumberOfPercentage { get; set; }

        [Display(Name = "Author must accept or refute each issue")]
        public bool AuthorMustAcceptorRefuteEachIssue { get; set; }

        [Display(Name = "Author must provide written rationale for issues refuted")]
        public bool AuthorMustProvideRationale { get; set; }

        [Display(Name = "Author must specify whether each issue was addressed in the resubmission")]
        public bool AuthorMustSayIfIssueWasAddressed { get; set; }

        [Display(Name = "Author must describe how each issue was addressed in the resubmission")]
        public bool AuthorMustDescribeHowAddressed { get; set; }
    }
}