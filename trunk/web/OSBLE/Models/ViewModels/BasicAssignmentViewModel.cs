using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Assignments;
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

        public SubmissionActivity Submission { get; set; }

        public StopActivity Stop { get; set; }

        public BasicAssignment Assignment { get; set; }

        public SilverlightObject TeamCreation { get; set; }
    }
}