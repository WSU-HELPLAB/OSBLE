using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Intervention
{
    public class InterventionItem
    {
        public int Id { get; set; }
        public int UserProfileId { get; set; }
        public string InterventionTrigger { get; set; }
        public bool InterventionMarkedHelpful { get; set; }
        public DateTime InterventionDateTime { get; set; }
        public string InterventionType { get; set; }
        public string Icon1 { get; set; }
        public string Icon2 { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string LinkText { get; set; }
        public bool ContentFirst { get; set; }
        public string ListItemContent { get; set; }
        public string InterventionTemplateText { get; set; }
        public string InterventionSuggestedCode { get; set; }
        public bool IsDismissed { get; set; }

        public InterventionItem()
        {
            Id = -1;
            InterventionType = "";
        }
    }
}
