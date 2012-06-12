using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System;

namespace OSBLE.Models
{
    [Serializable]
    public class School
    {
        [Required]
        [Key]
        public int ID { get; set; }

        [StringLength(128)]
        public string Name { get; set; }

        public School() : base() { }

        public School(string Name)
            : base()
        {
            this.Name = Name;
        }
    }
}