using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OSBLE.Models.Users;
using OSBLE.Models.Courses;


namespace OSBLE.Models.AbstractCourses.Course
{
    public class WhiteTable
    {
        [Key]
        [Required]
        [Column(Order = 0)]
        public int ID { get; set; }

        [Required]
        [Column(Order = 1)]
        public int WhiteTableUserID { get; set; }

        public virtual WhiteTableUser WhiteTableUser { get; set; }

        [Required]
        [Column(Order = 2)]
        public int AbstractCourseID { get; set; }

        public virtual AbstractCourse AbstractCourse { get; set; }



        [Required]
        public int Section { get; set; }

        public bool Hidden { get; set; }

        public WhiteTable ()
            : base()
        {
            Hidden = false;
        }

        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            //The CourseUser class utilizes a custom trigger to handle the intricices of deleting users from courses.  As 
            //a SQL Server requirement, all FK relations must not cascade on delete.
            modelBuilder.Entity<WhiteTable>()
                .HasRequired(m => m.AbstractCourse)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<WhiteTable>()
                .HasRequired(m => m.WhiteTableUser)
                .WithMany()
                .WillCascadeOnDelete(false);
        }
    }
}
