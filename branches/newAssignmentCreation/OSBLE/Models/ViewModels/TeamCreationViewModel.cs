using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Assignments;
using OSBLE.Models.Users;

namespace OSBLE.Models.ViewModels
{
    public class TeamCreationViewModel
    {
        public Assignment Assignment { get; set; }
        public List<UserProfile> Students { get; set; }
    }
}