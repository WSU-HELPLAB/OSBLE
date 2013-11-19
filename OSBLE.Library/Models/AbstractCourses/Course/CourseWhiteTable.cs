using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.AbstractCourses.Course
{
    public class CourseWhiteTable
    {
        [Required]
        [Key]
        public int ID
        {
            set;
            get;
        }
        [Required]
        public int Section
        {
            get;
            set;

        }
        [Required]
        public virtual IList<WhiteTableUser> entries { set; get; }


        public CourseWhiteTable ()
            : base()
        {
            
        }

        
    }
}
