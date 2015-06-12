using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.Courses.Rubrics
{
    public class Level : IModelBuilderExtender
    {
        [Required]
        [Key]
        [Editable(true)]
        public int ID { get; set; }

        [Required]
        public int RubricID { get; set; }

        public virtual Rubric Rubric { get; set; }

        [Required]
        public int PointSpread { get; set; }

        [Required]
        public string LevelTitle { get; set; }

        public Level()
            : base()
        {
            PointSpread = 5;
        }


        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Level>()
                .HasRequired(l => l.Rubric)
                .WithMany(r => r.Levels)
                .WillCascadeOnDelete(true);
        }
    }

}