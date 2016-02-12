using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.ViewModels
{
    public class GetItemUpdatesViewModel
    {
        public int LogId { get; set; }
        public long LastPollTick { get; set; }
    }
}