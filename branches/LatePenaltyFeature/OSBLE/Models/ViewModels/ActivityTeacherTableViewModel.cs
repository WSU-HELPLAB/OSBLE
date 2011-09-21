using System;
using System.Collections.Generic;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;

namespace OSBLE.Models.ViewModels
{
    public class ActivityTeacherTableViewModel
    {
        public ActivityTeacherTableViewModel(AbstractAssignment assignment, AbstractAssignmentActivity activity)
        {
            Assignment = assignment;
            Activity = activity;
            SubmissionsInfo = new List<SubmissionInfo>();
        }

        public AbstractAssignment Assignment { get; set; }

        public AbstractAssignmentActivity Activity { get; set; }

        public List<SubmissionInfo> SubmissionsInfo { get; set; }

        public class SubmissionInfo
        {
            public bool isTeam { get; set; }

            public string TeamList { get; set; }

            public int SubmitterID { get; set; }

            public string Name { get; set; }

            public DateTime? Time { get; set; }

            public bool Graded { get; set; }

            public double LatePenaltyPercent { get; set; }

            public double ManualLatePenaltyPercent { get; set; }
        }
    }
}