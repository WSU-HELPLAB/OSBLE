using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Intervention
{
    public class UserStatus
    {
        public int UserProfileId { get; set; }
        public int CourseId { get; set; }
        public string StatusMessage { get; set; }
        public DateTime AvailableStartTime { get; set; }
        public DateTime AvailableEndTime { get; set; }
        public bool IsAvailableToHelp { get; set; }
    }
}
