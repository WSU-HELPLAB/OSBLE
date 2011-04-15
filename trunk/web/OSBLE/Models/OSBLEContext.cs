﻿using System.Data.Entity;

namespace OSBLE.Models
{
    public class OSBLEContext : DbContext
    {
        // You can add custom code to this file. Changes will not be overwritten.
        // 
        // If you want Entity Framework to drop and regenerate your database
        // automatically whenever you change your model schema, add the following
        // code to the Application_Start method in your Global.asax file.
        // Note: this will destroy and re-create your database with every model change.
        // 
        // System.Data.Entity.Database.SetInitializer(new System.Data.Entity.DropCreateDatabaseIfModelChanges<OSBLE.Models.OSBLEContext>());

        public OSBLEContext() : base("OSBLEData")
        {

        }

        public DbSet<CourseRole> CourseRoles { get; set; }
        public DbSet<CoursesUsers> CoursesUsers { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<Course> Courses { get; set; }

    }
}
