using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.UI;
using OSBLE.Models.OSBLECommunity;

namespace OSBLE.Models.ViewModels
{
    public class OSBLECommunityViewModel
    {
        /// <summary>
        /// List of grid views for the current user
        /// </summary>
        public List<OSBLECommunityGrid> Grids { get; set; }

        /// <summary>
        /// Current CourseUser
        /// </summary>
        public int UserProfileId { get; set; }

        public int AbstractCourseId { get; set; }
        
        
        public OSBLECommunityViewModel()
        {
            Grids = new List<OSBLECommunityGrid>();            
        }        
    }
}
