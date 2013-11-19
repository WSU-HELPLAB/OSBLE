using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace OSBLE.Models.AbstractCourses.Course
{
    public class WhiteTableUser
    {
        [Required]
        [Key]
        public int ID
        {
            set;
            get;
        }
        public string Name1
        {
            get;
            set;
        }
        public string Name2
        {
            get;
            set;
        }

        [Required]
        public string Email
        {
            get;
            set;
        }

        [Required]
        public bool Verify
        {
            get;
            set;
        }

        public WhiteTableUser()
            : base ()
        {
            Email = "";
            Verify = false;
            Name1 = "";
            Name2 = "";
        }

        // yc
        //im not sure if this is needed
        // also check WebApiConfig.cs
        // do we need to add something there?
        public void BuildRelationship(System.Data.Entity.DbModelBuilder modelBuilder)
        {
            /*
            //The CourseUser class utilizes a custom trigger to handle the intricices of deleting users from courses.  As 
            //a SQL Server requirement, all FK relations must not cascade on delete.
            modelBuilder.Entity<CourseWhiteTable>()
                .HasRequired(m => m.AbstractCourse)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CourseWhiteTable>()
                .HasRequired(m => m.AbstractRole)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CourseUser>()
                .HasRequired(m => m.UserProfile)
                .WithMany()
                .WillCascadeOnDelete(false);
             */
        }
    }
}
