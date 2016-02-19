using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.ViewModels
{
    public class OSBLECommunityRecentActivtyViewModel
    {
        public Dictionary <string, int> User { get; set; }
        public Dictionary<string, int> Event { get; set; }
        public DateTime EventDate { get; set; }

        public OSBLECommunityRecentActivtyViewModel()
        {
            User = new Dictionary<string, int>();
            Event = new Dictionary<string, int>();
            EventDate = new DateTime();
        }
    }
}
