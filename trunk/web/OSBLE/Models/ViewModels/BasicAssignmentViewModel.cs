using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using System.ComponentModel;

namespace OSBLE.Models.ViewModels
{
    public class BasicAssignmentViewModel
    {
        public BasicAssignmentViewModel()
        {
            Submission = new SubmissionActivity();
            Stop = new StopActivity();
            Assignment = new BasicAssignment();
        }

        [DisplayName("Use Rubric?")]
        public bool UseRubric { get; set; }

        public SubmissionActivity Submission { get; set; }

        public StopActivity Stop { get; set; }

        public BasicAssignment Assignment { get; set; }

        public SilverlightObject TeamCreation { get; set; }

        public SilverlightObject RubricCreation { get; set; }

        public string SerializedTeamMembersJSON { get; set; }
    }
}