using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Areas.AssignmentWizard.Controllers;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public class WizardSortOrderComparer : IComparer<IWizardBaseController>
    {
        public int Compare(IWizardBaseController x, IWizardBaseController y)
        {
            return x.SortOrder.CompareTo(y.SortOrder);
        }
    }
}