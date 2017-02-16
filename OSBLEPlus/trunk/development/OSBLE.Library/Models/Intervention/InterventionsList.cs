using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Intervention
{
    public class InterventionsList
    {
        public List<InterventionItem> InterventionItemList { get; set; }

        public InterventionsList()
        {
            InterventionItemList = new List<InterventionItem>();
        }
    }
}
