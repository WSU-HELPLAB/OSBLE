using System.Data.Entity;
using System.Web.Security;

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

        public DbSet<CourseMeeting> CourseMeetings { get; set; }

        public DbSet<CourseBreak> CourseBreaks { get; set; }

        public DbSet<CoursesUsers> CoursesUsers { get; set; }

        public DbSet<School> Schools { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<AbstractCourse> AbstractCourses { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<Community> Communities { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        public DbSet<DashboardPost> DashboardPosts { get; set; }

        public DbSet<DashboardReply> DashboardReplies { get; set; }

        public DbSet<Mail> Mails { get; set; }

        public DbSet<SubmissionActivitySetting> SubmissionActivitySettings { get; set; }

        public DbSet<Weight> Weights { get; set; }

        public DbSet<Assignment> Assignments { get; set; }

        public DbSet<AssignmentActivity> AssignmentActivities { get; set; }

        public DbSet<GradableScore> GradableScores { get; set; }

        public DbSet<AbstractGradable> AbstractGradables { get; set; }

        public DbSet<Gradable> Gradables { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Notification>()
                .HasRequired(n => n.Sender)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Mail>()
                .HasRequired(n => n.ToUserProfile)
                .WithMany()
                .WillCascadeOnDelete(false);

        }

        private void createSampleUser(string username, string password, string firstname, string lastname, string ident, int school, bool isAdmin, bool canCreateCourses)
        {
            Membership.CreateUser(username, password, username);

            UserProfile up = new UserProfile();
            up.FirstName = firstname;
            up.LastName = lastname;
            up.IsAdmin = isAdmin;
            up.Identification = ident;
            up.SchoolID = school;
            up.UserName = username;
            up.CanCreateCourses = canCreateCourses;

            this.UserProfiles.Add(up);
        }

        /// <summary>
        /// Adds course roles to db
        /// </summary>
        public void SeedRoles()
        {
            // Set up "static" values for Course Roles.

            // Instructor: Can Modify Course, See All, Can Grade
            this.CourseRoles.Add(new CourseRole("Instructor", true, true, true, false, false));
            // TA: Can See All, Can Grade
            this.CourseRoles.Add(new CourseRole("TA", false, true, true, false, false));
            // Student: Can Submit Assignments, All Anonymized
            this.CourseRoles.Add(new CourseRole("Student", false, false, false, true, false));
            // Moderator: No Special Privileges
            this.CourseRoles.Add(new CourseRole("Moderator", false, false, false, false, false));
            // Observer: Can See All, All Anonymized
            this.CourseRoles.Add(new CourseRole("Observer", false, true, false, false, true));
        }

        /// <summary>
        /// Creates sample data for OSBLE for development purposes.
        /// </summary>
        public void SeedTestData()
        {
            FormsAuthentication.SignOut();

            FileSystem.WipeOutFileSystem();

            // Sample Schools
            School s1 = new School();
            s1.Name = "Washington State University";

            School s2 = new School();
            s2.Name = "Somewhere Else University";

            this.Schools.Add(s1);
            this.Schools.Add(s2);

            this.SaveChanges();

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

            Community c3 = new Community();
            c3.Nickname = "Comm";
            c3.Name = "A Community";
            c3.Description = "This is a course community.";
            
            this.Courses.Add(c1);
            this.Courses.Add(c2);
            this.Communities.Add(c3);

            this.SaveChanges();

            MembershipUserCollection muc = Membership.GetAllUsers();
            foreach (MembershipUser mu in muc)
            {
                Membership.DeleteUser(mu.UserName);
            }

            this.SaveChanges();

            createSampleUser("bob@smith.com", "123123", "Bob", "Smith", "1", 1, false, true);
            createSampleUser("stu@dent.com", "123123", "Stu", "Dent", "2", 1, false, false);
            createSampleUser("me@me.com", "123123", "Ad", "Min", "3", 1, true, true);

            this.SaveChanges();
            
            CoursesUsers cu = new CoursesUsers();
            cu.CourseID = 1;
            cu.UserProfileID = 1;
            cu.CourseRoleID = (int)CourseRole.OSBLERoles.Instructor;
            cu.Section = 0;

            CoursesUsers cu2 = new CoursesUsers();
            cu2.CourseID = 2;
            cu2.UserProfileID = 1;
            cu2.CourseRoleID = (int)CourseRole.OSBLERoles.Observer;
            cu2.Section = 0;

            CoursesUsers cu3 = new CoursesUsers();
            cu3.CourseID = 1;
            cu3.UserProfileID = 2;
            cu3.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu3.Section = 1;

            CoursesUsers cu4 = new CoursesUsers();
            cu4.CourseID = 2;
            cu4.UserProfileID = 3;
            cu4.CourseRoleID = (int)CourseRole.OSBLERoles.Instructor;
            cu4.Section = 0;

            CoursesUsers cu5 = new CoursesUsers();
            cu5.CourseID = 2;
            cu5.UserProfileID = 2;
            cu5.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu5.Section = 2;

            CoursesUsers cu6 = new CoursesUsers();
            cu6.CourseID = 3;
            cu6.UserProfileID = 1;
            cu6.CourseRoleID = (int)CourseRole.OSBLERoles.Instructor;
            cu6.Section = 0;

            CoursesUsers cu7 = new CoursesUsers();
            cu7.CourseID = 3;
            cu7.UserProfileID = 2;
            cu7.CourseRoleID = (int)CourseRole.OSBLERoles.TA;
            cu7.Section = 0;
            
            this.CoursesUsers.Add(cu);
            this.CoursesUsers.Add(cu2);
            this.CoursesUsers.Add(cu3);
            this.CoursesUsers.Add(cu4);
            this.CoursesUsers.Add(cu5);
            this.CoursesUsers.Add(cu6);
            this.CoursesUsers.Add(cu7);
            
            this.SaveChanges();
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

            context.SeedRoles();
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

            context.SeedRoles();
            context.SeedTestData();
        }
    }
}