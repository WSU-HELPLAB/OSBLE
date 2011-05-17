using System.Data.Entity;
using System.Web.Security;
using System.Data.Entity.ModelConfiguration.Conventions;

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

        public OSBLEContext()
            : base("OSBLEData")
        { }

        public DbSet<CourseRole> CourseRoles { get; set; }

        public DbSet<CoursesUsers> CoursesUsers { get; set; }

        public DbSet<School> Schools { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<Notifications> Notifications { get; set; }

        public DbSet<DashboardPost> DashboardPosts { get; set; }

        public DbSet<DashboardReply> DashboardReplies { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TODO: Investigate getting cascades to work properly.
            modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
        }

        /// <summary>
        /// Creates sample data for OSBLE for development purposes.
        /// </summary>
        public void SeedTestData()
        {

            #region Course Roles

            // Set up "static" values for Course Roles.

            // Instructor: Can Modify Course, See All, Can Grade
            this.CourseRoles.Add(new CourseRole("Instructor", true, true, true, false, false));
            // TA: Can See All, Can Grade
            this.CourseRoles.Add(new CourseRole("TA", false, true, true, false, false));
            // Student: Can Submit Assignments, All Anonymized
            this.CourseRoles.Add(new CourseRole("Student", false, false, false, true, true));
            // Moderator: No Special Privileges
            this.CourseRoles.Add(new CourseRole("Moderator", false, false, false, false, false));
            // Observer: Can See All, All Anonymized
            this.CourseRoles.Add(new CourseRole("Observer", false, true, false, false, true));

            #endregion Course Roles

            #region Test Data

            // Sample Schools
            School s1 = new School();
            s1.Name = "Washington State University";

            School s2 = new School();
            s2.Name = "Somewhere Else University";

            this.Schools.Add(s1);
            this.Schools.Add(s2);

            // Sample Courses
            Course c1 = new Course();
            c1.Prefix = "Cpt S";
            c1.Number = "111";
            c1.Semester = "Spring";
            c1.Year = "2011";
            c1.Name = "Introduction to Programming";

            Course c2 = new Course();
            c2.Prefix = "Art E";
            c2.Number = "345";
            c2.Semester = "Fall";
            c2.Year = "2011";
            c2.Name = "Underwater Basketweaving";

            this.Courses.Add(c1);
            this.Courses.Add(c2);

            MembershipUserCollection muc = Membership.GetAllUsers();
            foreach (MembershipUser mu in muc)
            {
                Membership.DeleteUser(mu.UserName);
            }

            #endregion Test Data


        }

    }

    /// <summary>
    /// Meant to be called everytime the database is accessed. By default OSBLEContextModelChangeInitializer is being used.
    /// Change the SetInitializer entry in Global.asax to this if you want to force a database recreate.
    /// </summary>
    public class OSBLEContextAlwaysCreateInitializer : DropCreateDatabaseAlways<OSBLEContext>
    {
        protected override void Seed(OSBLEContext context)
        {
            base.Seed(context);

            context.SeedTestData();
        }
    }

    /// <summary>
    /// Called when the model changes which causes the database to recreate. Can be disabled by commenting out the
    /// SetInitializer call in Global.asax .
    /// </summary>
    public class OSBLEContextModelChangeInitializer : DropCreateDatabaseIfModelChanges<OSBLEContext>
    {
        protected override void Seed(OSBLEContext context)
        {
            base.Seed(context);

            context.SeedTestData();
        }
    }
}