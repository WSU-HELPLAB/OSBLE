using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Intervention
{
    public class MakeAPostViewModel
    {
        public InterventionItem Intervention { get; set; }
        public List<string> PopularHashtags { get; set; }

        public MakeAPostViewModel()
        {
            Intervention = new InterventionItem();
            PopularHashtags = new List<string>();
        }
    }
}
