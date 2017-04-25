using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Intervention
{
    public class ClassmatesAvailableViewModel
    {
        public InterventionItem Intervention { get; set; }
        public Dictionary<int, string> AvailableUsers { get; set; } //userId, userName

        public ClassmatesAvailableViewModel()
        {
            Intervention = new InterventionItem();
            AvailableUsers = new Dictionary<int, string>();
        }
    }
}
