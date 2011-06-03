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
            BasicAssignment = new BasicAssignment();
        }

        public SubmissionActivity Submission { get; set; }

        public StopActivity Stop { get; set; }

        public BasicAssignment BasicAssignment { get; set; }
    }
}