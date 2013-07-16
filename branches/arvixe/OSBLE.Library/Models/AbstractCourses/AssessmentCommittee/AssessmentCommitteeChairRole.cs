using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Courses
{
    public class AssessmentCommitteeChairRole : AssessmentCommitteeRole
    {
        public AssessmentCommitteeChairRole()
        {
            this.CanModify = true;
            this.CanSeeAll = true;
            this.CanGrade = true;
            this.CanSubmit = true;
            this.Anonymized = false;
            this.CanUploadFiles = true;
            Name = "Assessment Committee Chair";
        }

        // Roles defined elsewhere go up to 9, so we start at 10 here
        public const int RoleID = 10;
    }
}
