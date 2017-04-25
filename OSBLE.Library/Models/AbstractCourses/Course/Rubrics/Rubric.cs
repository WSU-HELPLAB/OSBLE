using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;

namespace OSBLE.Models.Courses.Rubrics
{
    public class Rubric : IModelBuilderExtender
    {
        [Required]
        [Key]
        [Editable(true)]
        public int ID { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public bool HasCriteriaComments { get; set; }

        [Required]
        public bool HasGlobalComments { get; set; }

        [Required]
        public bool EnableHalfStep { get; set; }

        [Required]
        public bool EnableQuarterStep { get; set; }

        [Association("Levels", "ID", "RubricID")]
        public virtual IList<Level> Levels { get; set; }

        [Association("Criteria", "ID", "RubricID")]
        public virtual IList<Criterion> Criteria { get; set; }

        [Association("CellDescription", "ID", "RubricID")]
        public virtual IList<CellDescription> CellDescriptions { get; set; }

        public Rubric()
        {
           Levels = new List<Level>();
           Criteria = new List<Criterion>();
           EnableHalfStep = false;
           EnableQuarterStep = false;
        }

        public void BuildRelationship(DbModelBuilder modelBuilder)
        {
            
        }
    }
}