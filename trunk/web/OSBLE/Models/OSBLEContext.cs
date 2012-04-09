using System.Data.Entity;
using System.Web;
using System.Web.Security;
using OSBLE.Models.AbstractCourses;
using OSBLE.Models.Assignments;

using OSBLE.Models.Assignments;

//using OSBLE.Models.Assignments.Activities.CommentCategories;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.HomePage;
using OSBLE.Models.Users;
using OSBLE.Utility;
using System.Data.Entity.Infrastructure;
using OSBLE.Models.DiscussionAssignment;

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
        {
            //Needed for EF 4.1 CF RIA Services.  See:
            //http://varunpuranik.wordpress.com/2011/06/29/wcf-ria-services-support-for-ef-4-1-and-ef-code-first/
            if (HttpContext.Current == null)
            {
                Database.SetInitializer<OSBLEContext>(null);
            }
        }

        public DbSet<School> Schools { get; set; }

        // Assignments

        public DbSet<CommentCategory> CommentCategories { get; set; }

        public DbSet<CommentCategoryConfiguration> CommentCategoryConfigurations { get; set; }

        public DbSet<CommentCategoryOption> CommentCategoryOptions { get; set; }

        // **NEW** Assignment Tables

        public DbSet<Assignment> Assignments { get; set; }

        public DbSet<AssignmentTeam> AssignmentTeams { get; set; }

        public DbSet<DiscussionTeam> DiscussionTeams { get; set; }

        public DbSet<ReviewTeam> ReviewTeams { get; set; }

        public DbSet<Team> Teams { get; set; }

        public DbSet<TeamMember> TeamMembers { get; set; }

        public DbSet<DiscussionSetting> DiscussionSettings { get; set; }

        public DbSet<TeamEvaluation> TeamEvaluations { get; set; }

        public DbSet<TeamMemberEvaluation> TeamMemberEvaluations { get; set; }

        public DbSet<DiscussionReply> DiscussionReplies { get; set; }

        public DbSet<TeamEvaluationSettings> TeamEvaluationSettings { get; set; }

        // Assignments

        public DbSet<Score> Scores { get; set; }

        // DiscussionAssignments

        public DbSet<DiscussionPost> DiscussionPosts { get; set; }

        // Courses

        public DbSet<AbstractCourse> AbstractCourses { get; set; }

        public DbSet<AbstractRole> AbstractRoles { get; set; }

        public DbSet<Community> Communities { get; set; }

        public DbSet<CommunityRole> CommunityRoles { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<CourseBreak> CourseBreaks { get; set; }

        public DbSet<CourseMeeting> CourseMeetings { get; set; }

        public DbSet<CourseRole> CourseRoles { get; set; }

        public DbSet<CourseUser> CourseUsers { get; set; }

        public DbSet<Category> Categories { get; set; }

        public DbSet<LetterGrade> LetterGrades { get; set; }

        // HomePage

        public DbSet<AbstractDashboard> AbstractDashboards { get; set; }

        public DbSet<DashboardPost> DashboardPosts { get; set; }

        public DbSet<DashboardReply> DashboardReplies { get; set; }

        public DbSet<Event> Events { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        // Rubrics

        public DbSet<Rubric> Rubrics { get; set; }

        public DbSet<Level> Levels { get; set; }

        public DbSet<Criterion> Criteria { get; set; }

        public DbSet<CellDescription> CellDescriptions { get; set; }

        public DbSet<CourseRubric> CourseRubrics { get; set; }

        public DbSet<RubricEvaluation> RubricEvaluations { get; set; }

        public DbSet<CriterionEvaluation> CriterionEvaluations { get; set; }

        // Users

        public DbSet<Mail> Mails { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
           
#if !DEBUG
            modelBuilder.Conventions.Remove<IncludeMetadataConvention>();
#endif
            //try to keep modelbuilder stuff in alphabetical order
            modelBuilder.Entity<Deliverable>()
                .HasRequired(d => d.Assignment)
                .WithMany(a => a.Deliverables)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<DiscussionPost>()
                .HasRequired(cu => cu.CourseUser)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DiscussionSetting>()
                .HasRequired(ds => ds.Assignment)
                .WithOptional(a => a.DiscussionSettings)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<DiscussionReply>()
                .HasRequired(cu => cu.CourseUser)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Mail>()
                .HasRequired(n => n.ToUserProfile)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Notification>()
                .HasRequired(n => n.Sender)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Score>()
                .HasRequired(s => s.Assignment)
                .WithMany(a => a.Scores)
                .WillCascadeOnDelete(false);

            //In a critical review, students will be reviewing an existing assignment.
            //Therefore, AuthorTeams are fixed while the reviewing team might still change.
            modelBuilder.Entity<ReviewTeam>()
                .HasRequired(rt => rt.ReviewingTeam)
                .WithMany()
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<ReviewTeam>()
                .HasRequired(rt => rt.AuthorTeam)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<Rubric>()
                .HasMany(r => r.Levels)
                .WithRequired(l => l.Rubric)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<RubricEvaluation>()
                .HasRequired(re => re.Recipient)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamEvaluationSettings>()
                .HasRequired(tes => tes.Assignment)
                .WithOptional(a => a.TeamEvaluationSettings)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<TeamMemberEvaluation>()
                .HasRequired(tm => tm.Evaluator)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamMemberEvaluation>()
                .HasRequired(tm => tm.Recipient)
                .WithMany()
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<TeamMemberEvaluation>()
                .HasRequired(tm => tm.TeamEvaluation)
                .WithMany(e => e.TeamMemberEvaluations)
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
            up.AspNetUserName = username;
            up.CanCreateCourses = canCreateCourses;

            this.UserProfiles.Add(up);
        }

        public void SeedSchools()
        {

        }

        /// <summary>
        /// Adds course roles to db
        /// </summary>
        public void SeedRoles()
        {
            // Set up "static" values for Course Roles.

            // Instructor: Can Modify Course, See All, Can Grade
            this.CourseRoles.Add(new CourseRole(CourseRole.CourseRoles.Instructor.ToString(), true, true, true, false, true, false));

            // TA: Can See All, Can Grade
            this.CourseRoles.Add(new CourseRole(CourseRole.CourseRoles.TA.ToString(), false, true, true, false, true, false));

            // Student: Can Submit Assignments, All Anonymized
            this.CourseRoles.Add(new CourseRole(CourseRole.CourseRoles.Student.ToString(), false, false, false, true, false, false));

            // Moderator: No Special Privileges
            this.CourseRoles.Add(new CourseRole(CourseRole.CourseRoles.Moderator.ToString(), false, false, false, false, false, false));

            // Observer: Can See All, All Anonymized
            this.CourseRoles.Add(new CourseRole(CourseRole.CourseRoles.Observer.ToString(), false, true, false, false, false, true));

            // Community Roles

            // Leader: Can Modify Community
            this.CommunityRoles.Add(new CommunityRole(CommunityRole.OSBLERoles.Leader.ToString(), true, true, true, true));

            // Participant: Cannot Modify Community
            this.CommunityRoles.Add(new CommunityRole(CommunityRole.OSBLERoles.Participant.ToString(), false, true, true, false));

            //trusted communityt member: same as participant, but can upload files to the server
            this.CommunityRoles.Add(new CommunityRole(CommunityRole.OSBLERoles.TrustedCommunityMember.ToString(), false, true, true, true));
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

            Course c4 = new Course();
            c4.Prefix = "OSBLE";
            c4.Number = "101";
            c4.Semester = "Fall";
            c4.Year = "2011";
            c4.Name = "Intro to OSBLE";

            this.Courses.Add(c1);
            this.Courses.Add(c2);
            this.Communities.Add(c3);
            this.Courses.Add(c4);

            this.SaveChanges();

            Category w1 = new Category();
            w1.Name = "Homework";
            w1.Points = 40;

            Category w2 = new Category();
            w2.Name = "Exams";
            w2.Points = 60;

            Category w3 = new Category();
            w3.Name = Constants.UnGradableCatagory;
            w3.Points = 0;

            c1.Categories.Add(w3);
            c1.Categories.Add(w1);
            c1.Categories.Add(w2);

            w1 = new Category();
            w1.Name = "Homework";
            w1.Points = 40;

            w2 = new Category();
            w2.Name = "Exams";
            w2.Points = 60;

            w3 = new Category();
            w3.Name = Constants.UnGradableCatagory;
            w3.Points = 0;

            c2.Categories.Add(w3);
            c2.Categories.Add(w1);
            c2.Categories.Add(w2);

            w1 = new Category();
            w1.Name = "Homework";
            w1.Points = 40;

            w2 = new Category();
            w2.Name = "Exams";
            w2.Points = 60;

            w3 = new Category();
            w3.Name = Constants.UnGradableCatagory;
            w3.Points = 0;

            c4.Categories.Add(w3);
            c4.Categories.Add(w1);
            c4.Categories.Add(w2);

            MembershipUserCollection muc = Membership.GetAllUsers();
            foreach (MembershipUser mu in muc)
            {
                Membership.DeleteUser(mu.UserName);
            }

            this.SaveChanges();

            createSampleUser("bob@smith.com", "123123", "Bob", "Smith", "1", 1, true, true);
            createSampleUser("stu@dent.com", "123123", "Stu", "Dent", "2", 1, false, false);
            createSampleUser("me@me.com", "123123", "Ad", "Min", "3", 1, true, true);
            createSampleUser("John@Morgan.com", "123123", "John", "Morgan", "4", 1, false, false);
            createSampleUser("Margaret@Bailey.com", "123123", "Margaret", "Bailey", "5", 1, false, false);
            createSampleUser("Carol@Jackson.com", "123123", "Carol", "Jackson", "6", 1, false, false);
            createSampleUser("Donald@Robinson.com", "123123", "Donald", "Robinson", "7", 1, false, false);
            createSampleUser("Paul@Sanders.com", "123123", "Paul", "Sanders", "8", 1, false, false);
            createSampleUser("Anthony@Stewart.com", "123123", "Anthony", "Stewart", "9", 1, false, false);
            createSampleUser("Paul@Harris.com", "123123", "Paul", "Harris", "10", 1, false, false);
            createSampleUser("Donald@White.com", "123123", "Donald", "White", "12", 1, false, false);
            createSampleUser("Christopher@Sanders.com", "123123", "Christopher", "Sanders", "13", 1, false, false);
            createSampleUser("Robert@Wright.com", "123123", "Robert", "Wright", "14", 1, false, false);
            createSampleUser("Betty@Rogers.com", "123123", "Betty", "Rogers", "15", 1, false, false);
            createSampleUser("Nancy@Russell.com", "123123", "Nancy", "Russell", "16", 1, false, false);
            createSampleUser("Jason@Robinson.com", "123123", "Jason", "Robinson", "17", 1, false, false);

            this.SaveChanges();

            #region add course users

            CourseUser cu = new CourseUser();
            cu.AbstractCourseID = 1;
            cu.UserProfileID = 1;
            cu.AbstractRoleID = (int)CourseRole.CourseRoles.Instructor;
            cu.Section = 0;

            CourseUser cu2 = new CourseUser();
            cu2.AbstractCourseID = 2;
            cu2.UserProfileID = 1;
            cu2.AbstractRoleID = (int)CourseRole.CourseRoles.Observer;
            cu2.Section = 0;

            CourseUser cu3 = new CourseUser();
            cu3.AbstractCourseID = 1;
            cu3.UserProfileID = 2;
            cu3.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu3.Section = 1;

            CourseUser cu4 = new CourseUser();
            cu4.AbstractCourseID = 2;
            cu4.UserProfileID = 3;
            cu4.AbstractRoleID = (int)CourseRole.CourseRoles.Instructor;
            cu4.Section = 0;

            CourseUser cu5 = new CourseUser();
            cu5.AbstractCourseID = 2;
            cu5.UserProfileID = 2;
            cu5.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu5.Section = 2;

            CourseUser cu6 = new CourseUser();
            cu6.AbstractCourseID = 3;
            cu6.UserProfileID = 1;
            cu6.AbstractRoleID = (int)CommunityRole.OSBLERoles.Leader;
            cu6.Section = 0;

            CourseUser cu7 = new CourseUser();
            cu7.AbstractCourseID = 3;
            cu7.UserProfileID = 2;
            cu7.AbstractRoleID = (int)CommunityRole.OSBLERoles.Participant;
            cu7.Section = 0;

            CourseUser cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 4;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 5;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 1;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Instructor;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 6;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 7;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 8;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 9;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 10;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 11;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 12;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 13;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 14;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 15;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CourseUser();
            cu8.AbstractCourseID = 4;
            cu8.UserProfileID = 16;
            cu8.AbstractRoleID = (int)CourseRole.CourseRoles.Student;
            cu8.Section = 0;
            this.CourseUsers.Add(cu8);
            this.SaveChanges();

            this.CourseUsers.Add(cu);
            this.CourseUsers.Add(cu2);
            this.CourseUsers.Add(cu3);
            this.CourseUsers.Add(cu4);
            this.CourseUsers.Add(cu5);
            this.CourseUsers.Add(cu6);
            this.CourseUsers.Add(cu7);

            this.SaveChanges();

            #endregion add course users

            this.SaveChanges();
        }
    }

    /// <summary>
    /// Meant to be called every time the database is accessed. By default OSBLEContextModelChangeInitializer is being used.
    /// Change the SetInitializer entry in Global.asax to this if you want to force a database recreate.
    /// </summary>
    public class OSBLEContextAlwaysCreateInitializer : DropCreateDatabaseAlways<OSBLEContext>
    {
        protected override void Seed(OSBLEContext context)
        {
            base.Seed(context);
            context.SeedRoles();
            context.SeedSchools();
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
            context.SeedSchools();
            context.SeedTestData();
        }
    }
}
