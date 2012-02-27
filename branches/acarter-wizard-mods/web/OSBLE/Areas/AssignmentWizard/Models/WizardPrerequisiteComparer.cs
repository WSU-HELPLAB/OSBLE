using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Areas.AssignmentWizard.Controllers;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public class WizardPrerequisiteComparer : IComparer<WizardBaseController>
    {
        public int Compare(WizardBaseController x, WizardBaseController y)
        {
            //if x thinks y is a prereq, then y must go first
            if (x.Prerequisites.Contains(y))
            {
                return 1;
            }

            //if y thinks x is a prereq, then x most go first
            if (y.Prerequisites.Contains(x))
            {
                return -1;
            }

            //if the item is required, then it should go first
            if (x.IsRequired && !y.IsRequired)
            {
                return 1;
            }
            if (!x.IsRequired && y.IsRequired)
            {
                return -1;
            }

            //otherwise, sort based on controller name
            return x.CompareTo(y);
        }
    }
}