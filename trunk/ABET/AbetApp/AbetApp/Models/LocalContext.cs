using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;


namespace AbetApp.Models
{
    public class LocalContext : DbContext
    {
        public DbSet<Course> Courses { get; set; }
    }
}