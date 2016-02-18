using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.ViewModels
{
    public class OSBLECommunityOnlineViewModel
    {
        public Dictionary <string, int> OnlineUser { get; set; }
        
        public OSBLECommunityOnlineViewModel()
        {
            OnlineUser = new Dictionary<string, int>();
        }
    }
}
