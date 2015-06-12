using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Areas.AssignmentWizard.Controllers;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public class WizardSortOrderComparer : IComparer<WizardBaseController>
    {
        public int Compare(WizardBaseController x, WizardBaseController y)
        {
            return x.SortOrder.CompareTo(y.SortOrder);
        }
    }
}