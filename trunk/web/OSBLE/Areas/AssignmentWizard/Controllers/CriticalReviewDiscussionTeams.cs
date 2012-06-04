using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models.Assignments;

namespace OSBLE.Areas.AssignmentWizard.Controllers
{
    public class CriticalReviewDiscussionTeams : WizardBaseController
    {

        public override string ControllerName
        {
            get { return "CriticalReviewDiscussionTeams"; }
        }

        public override string PrettyName
        {
            get
            {
                return "CR Discussion Teams";
            }
        }

        public override string ControllerDescription
        {
            get { return "CriticalReviewDiscussionTeams"; }
        }

        public override bool IsRequired
        {
            get
            {
                return true;
            }
        }

        public override WizardBaseController Prerequisite
        {
            get { return new PreviousAssignmentController(); }
        }

        public override ICollection<AssignmentTypes> ValidAssignmentTypes
        {
            get
            {
                List<AssignmentTypes> prereqs = new List<AssignmentTypes>();

                prereqs.Add(AssignmentTypes.CriticalReviewDiscussion);

                return prereqs;
            }
        }
    }
}
