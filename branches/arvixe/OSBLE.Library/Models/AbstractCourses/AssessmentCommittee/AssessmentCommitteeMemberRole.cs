using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Courses
{
    public class AssessmentCommitteeMemberRole : AssessmentCommitteeRole
    {
        public AssessmentCommitteeMemberRole()
        {
            this.CanModify = false;
            this.CanSeeAll = true;
            this.CanGrade = true;
            this.CanSubmit = true;
            this.Anonymized = false;
            this.CanUploadFiles = false;
            Name = "Assessment Committee Member";
        }

        // Do not change, required to match in DB
        public const int RoleID = 11;
    }
}
