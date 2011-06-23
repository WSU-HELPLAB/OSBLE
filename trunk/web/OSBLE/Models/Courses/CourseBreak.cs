using System;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses
{
    public class CourseBreak
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [StringLength(50)]
        public string Name { get; set; }
    }
}