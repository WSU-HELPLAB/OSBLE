using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Intervention
{
    public class AvailableDetailsViewModel
    {
        public InterventionItem Intervention { get; set; }
        public Dictionary<int, string> AvailableUsers { get; set; } //userId, userName
        public List<UserStatus> UsersStatus { get; set; }
        public UserStatus CurrentUserStatus { get; set; }
        public AvailableDetailsViewModel()
        {
            Intervention = new InterventionItem();
            AvailableUsers = new Dictionary<int, string>();
            UsersStatus = new List<UserStatus>();
            CurrentUserStatus = new UserStatus();
        }
    }
}
