using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OSBLE.Models.Courses;
using OSBLE.Models.Users;

namespace OSBLE.Areas.AssignmentDetails.ViewModels
{
    public class CriticalReviewsReceivedTeam
    {
        public string TeamName { get; set; }
        public CourseUser CourseUser { get; set; }
        public UserProfile UserProfile { get; set; }

        public CriticalReviewsReceivedTeam()
        {
        }
    }
}