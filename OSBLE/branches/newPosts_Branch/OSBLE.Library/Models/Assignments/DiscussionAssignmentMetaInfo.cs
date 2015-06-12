using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using OSBLE.Models.Courses;

namespace OSBLE.Models.Assignments
{
    public class DiscussionAssignmentMetaInfo : IModelBuilderExtender
    {

        [Key]
        [Column(Order = 0)]
        public int DiscussionTeamID { get; set; }
        public virtual DiscussionTeam DiscussionTeam { get; set; }

        [Key]
        [Column(Order = 1)]
        public int CourseUserID { get; set; }
        public virtual CourseUser CourseUser { get; set; }

        public DateTime LastVisit { get; set; }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            
        }
    }
}
