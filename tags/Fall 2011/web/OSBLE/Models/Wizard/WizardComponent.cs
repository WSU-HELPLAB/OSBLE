using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Controllers.Assignments.Wizard;

namespace OSBLE.Models.Wizard
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