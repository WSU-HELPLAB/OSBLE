using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses.Rubrics
{
    public class CriterionEvaluation
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        [Column(Order = 1)]
        public int CriterionID { get; set; }

        public Criterion Criterion { get; set; }

        public int? Score { get; set; }

        [StringLength(4000)]
        public string Comment { get; set; }
    }
}