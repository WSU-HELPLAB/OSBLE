using System;
using System.Collections.Generic;

namespace OSBLE.Models.ViewModels
{
    public class ActivityTeacherTableViewModel
    {
        public ActivityTeacherTableViewModel()
        {
            SubmissionsInfo = new List<SubmissionInfo>();
        }

        public List<SubmissionInfo> SubmissionsInfo { get; set; }

        public class SubmissionInfo
        {
            public bool isTeam { get; set; }

            public int SubmitterID { get; set; }

            public string Name { get; set; }

            public DateTime? Time { get; set; }

            public bool Graded { get; set; }
        }
    }
}