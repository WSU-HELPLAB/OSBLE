using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace OSBLE.Models.Courses.Rubrics
{
    public class CriterionEvaluation : IModelBuilderExtender
    {
        [Key]
        [Required]
        public int ID { get; set; }

        [Required]
        [Column(Order = 1)]
        public int CriterionID { get; set; }
        public virtual Criterion Criterion { get; set; }

        [Required]
        public int RubricEvaluationID { get; set; }
        public virtual RubricEvaluation RubricEvaluation { get; set; }

        public double? Score { get; set; }

        [StringLength(4000)]
        public string Comment { get; set; }

        public void BuildRelationship(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CriterionEvaluation>()
                .HasRequired(ce => ce.RubricEvaluation)
                .WithMany(re => re.CriterionEvaluations)
                .WillCascadeOnDelete(true);
        }
    }
}