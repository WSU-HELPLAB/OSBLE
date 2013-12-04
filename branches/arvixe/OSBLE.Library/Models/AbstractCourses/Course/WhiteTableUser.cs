using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using OSBLE.Models.Users;

namespace OSBLE.Models.AbstractCourses.Course
{
    public class WhiteTableUser
    {
        [Key]
        [Required]
        public int ID
        {
            set;
            get;
        }
        [Required]
        public int CourseId
        {
            set;
            get;
        }
        [Required]
        public int SchoolId
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
           
        }
    }
}
