using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Courses
{
    public class ABETEvaluatorRole : AssessmentCommitteeRole
    {
        public ABETEvaluatorRole()
        {
            this.CanModify = false;
            this.CanSeeAll = true;
            this.CanGrade = false;
            this.CanSubmit = false;
            this.Anonymized = false;
            this.CanUploadFiles = false;
            Name = "ABET Evaluator";
        }

        // Do not change, required to match in DB
        public const int RoleID = 14;
    }
}
