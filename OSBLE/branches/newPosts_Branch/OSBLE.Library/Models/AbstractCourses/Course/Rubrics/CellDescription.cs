using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses.Rubrics
{
    
    public class CellDescription : IModelBuilderExtender
    {
        [Required]
        [Key]
        [Column(Order = 0)]
        public int CriterionID { get; set; }

        [Association("Criterion", "CriterionID", "ID")]
        public virtual Criterion Criterion { get; set; }

        [Required]
        [Key]
        [Column(Order = 1)]
        public int LevelID { get; set; }

        [Association("Level", "LevelID", "ID")]
        public virtual Level Level { get; set; }

        [Required]
        public int RubricID { get; set; }

        [Association("Rubric", "RubricID", "ID")]
        public virtual Rubric Rubric { get; set; }

        [Required]
        public string Description { get; set; }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CellDescription>()
               .HasRequired(cd => cd.Criterion)
               .WithMany()
               .WillCascadeOnDelete(false);

            modelBuilder.Entity<CellDescription>()
                .HasRequired(cd => cd.Level)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CellDescription>()
                .HasRequired(cd => cd.Rubric)
                .WithMany(r => r.CellDescriptions)
                .WillCascadeOnDelete(true);
        }
    }
}