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
        public int CourseID
        {
            set;
            get;
        }

        [Required]
        public int SchoolID
        {
            set;
            get;
        }
        public virtual School School { get; set; }
        [Required]
        public string Identification // student identification number NEEDS TO BE A STRING 
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

        
        public WhiteTableUser()
            : base ()
        {
            Email = "";
            Name1 = "";
            Name2 = "";
        }
        
        public WhiteTableUser(WhiteTableUser cpyuser)
            : this ()
        {
            this.Email = cpyuser.Email;
            this.Name1 = cpyuser.Name1;
            this.Name2 = cpyuser.Name2;
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
