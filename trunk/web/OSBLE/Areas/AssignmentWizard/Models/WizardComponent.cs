using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Areas.AssignmentWizard.Controllers;

namespace OSBLE.Areas.AssignmentWizard.Models
{
    public class WizardComponent
    {
        public WizardBaseController Controller { get; set; }
        public bool IsSelected { get; set; }
        public bool IsRequired { get; set; }
        public string Name
        {
            get
            {
                return Controller.ControllerName;
            }
        }
    }
}