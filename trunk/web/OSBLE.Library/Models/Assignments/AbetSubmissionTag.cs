using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using OSBLE.Models.Courses;
using System.ComponentModel.DataAnnotations.Schema;

namespace OSBLE.Models.Assignments
{
    public enum AbetSubmissionLevel : byte {
        Low = 1,
        Medium,
        High
    }

    public class AbetSubmissionTag
    {
        [Key]
        public int ID { get; set; }

        public int AssignmentID { get; set; }
        public virtual Assignment Assignment { get; set; }

        public int CourseUserID { get; set; }
        public virtual CourseUser CourseUser { get; set; }

        public byte SubmissionLevelByte { get; set; }

        [NotMapped]
        public AbetSubmissionLevel SubmissionLevel
        {
            get
            {
                return (AbetSubmissionLevel)SubmissionLevelByte;
            }
            set
            {
                SubmissionLevelByte = (byte)value;
            }
        }

    }
}
