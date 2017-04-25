using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Areas.AssignmentWizard.Controllers;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public class WizardComparer : IComparer<WizardBaseController>, IEqualityComparer<WizardBaseController>
    {
        public int Compare(WizardBaseController x, WizardBaseController y)
        {
            //otherwise, sort based on controller name
            return x.CompareTo(y);
        }

        public bool Equals(WizardBaseController x, WizardBaseController y)
        {
            return x.CompareTo(y) == 0;
        }

        public int GetHashCode(WizardBaseController obj)
        {
            return obj.ControllerName.GetHashCode();
        }
    }
}