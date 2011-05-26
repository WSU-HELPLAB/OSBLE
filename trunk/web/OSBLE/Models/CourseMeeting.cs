using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;


namespace OSBLE.Models
{
    public class CourseMeeting
    {
        [Required]
        [Key]
        public int ID { get; set; }

        public bool Sunday { get; set; }
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }

        [DataType(DataType.Time)]
        public DateTime StartTime { get; set; }

        [DataType(DataType.Time)]
        public DateTime EndTime { get; set; }

        public string Location { get; set; }

        public CourseMeeting()
            : base()
        {
            Sunday = Monday = Tuesday = Wednesday = Thursday = Friday = Saturday = false;
        }
    }
}