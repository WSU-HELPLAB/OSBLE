using OSBLE.Models.Courses;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.ViewModels
{
    public class CourseSettingsViewModel
    {
        public Course Course { get; set; }

        [Display(Name = "Set course as a 'programming' oriented course.")]
        public bool IsProgrammingCourse { get; set; }

        public CourseSettingsViewModel()
        {
            Course = new Course();
            IsProgrammingCourse = false;
        }
    }
}
