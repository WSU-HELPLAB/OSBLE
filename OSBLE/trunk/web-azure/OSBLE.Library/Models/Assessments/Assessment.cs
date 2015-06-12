using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSBLE.Models.Assessments
{
    public class Assessment
    {
        [Key]
        public int ID { get; set; }

        [Required]
        public string AssessmentName { get; set; }

        public static IList<AssessmentType> AllAssessmentTypes
        {
            get
            {
                return Enum.GetValues(typeof(AssessmentType)).Cast<AssessmentType>().ToList();
            }
        }

        [Required(ErrorMessage = "Please specify this assessment's type")]
        public int AssessmentTypeID { get; set; }


        [Display(Name = "Assignment Type")]
        [NotMapped]
        public AssessmentType Type
        {
            get
            {
                return (AssessmentType)AssessmentTypeID;
            }
            set
            {
                AssessmentTypeID = (int)value;
            }
        }
    }
}
