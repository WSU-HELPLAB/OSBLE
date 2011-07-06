﻿using System.Data.Entity;
using System.Web.Security;
using OSBLE.Models.AbstractCourses;
using OSBLE.Models.AbstractCourses.Course;
using OSBLE.Models.Assignments;
using OSBLE.Models.Assignments.Activities;
using OSBLE.Models.Assignments.Activities.Scores;

//using OSBLE.Models.Assignments.Activities.CommentCategories;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.HomePage;
using OSBLE.Models.Users;
using OSBLE.Models.AbstractCourses;
using System.Web;
using OSBLE.Utility;

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

        public DbSet<AbstractAssignment> AbstractAssignments { get; set; }

        public DbSet<BasicAssignment> BasicAssignments { get; set; }

        public DbSet<GradeAssignment> GradeAssignments { get; set; }

        public DbSet<StudioAssignment> StudioAssignments { get; set; }

        // Assignments.Activities

        public DbSet<AbstractAssignmentActivity> AbstractAssignmentActivity { get; set; }

        public DbSet<Deliverable> Deliverables { get; set; }

        public DbSet<GradeActivity> GradeActivities { get; set; }

        public DbSet<SingleSubmissionActivity> SingleSubmissionActivities { get; set; }

        public DbSet<StopActivity> StopActivities { get; set; }

        public DbSet<SubmissionActivity> SubmissionActivities { get; set; }

        // Assignments.Activities.Scores

        public DbSet<Score> Scores { get; set; }

        public DbSet<RubricScore> RubricScores { get; set; }

        // Assignments.Activities.CommentCategories

        //public DbSet<CommentCategory> CommentCategories { get; set; }

        //public DbSet<CommentCategoryTag> CommentCategoryTags { get; set; }

        // Courses

        public DbSet<AbstractCourse> AbstractCourses { get; set; }

        public DbSet<AbstractRole> AbstractRoles { get; set; }

        public DbSet<Community> Communities { get; set; }

        public DbSet<CommunityRole> CommunityRoles { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<CourseBreak> CourseBreaks { get; set; }

        public DbSet<CourseMeeting> CourseMeetings { get; set; }

        public DbSet<CourseRole> CourseRoles { get; set; }

        public DbSet<CoursesUsers> CoursesUsers { get; set; }

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

        public DbSet<CellDescription> LevelDescriptions { get; set; }

        public DbSet<CourseRubric> CourseRubrics { get; set; }

        
        public DbSet<RubricEvaluation> RubricEvaluations { get; set; }

        public DbSet<CriterionEvaluation> CriterionEvaluations { get; set; }


        // Users

        public DbSet<Team> Teams { get; set; }

        public DbSet<TeamMember> TeamMembers { get; set; }

        public DbSet<Mail> Mails { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<TeamUser> TeamUsers { get; set; }

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

            modelBuilder.Entity<Rubric>()
                .HasMany(r => r.Levels)
                .WithRequired(l => l.Rubric)
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

        public void SeedSchools()
        {
            this.Schools.Add(new School("Professional"));
        }

        /// <summary>
        /// Adds course roles to db
        /// </summary>
        public void SeedRoles()
        {
            // Set up "static" values for Course Roles.

            // Instructor: Can Modify Course, See All, Can Grade
            this.CourseRoles.Add(new CourseRole(CourseRole.OSBLERoles.Instructor.ToString(), true, true, true, false, true, false));

            // TA: Can See All, Can Grade
            this.CourseRoles.Add(new CourseRole(CourseRole.OSBLERoles.TA.ToString(), false, true, true, false, true, false));

            // Student: Can Submit Assignments, All Anonymized
            this.CourseRoles.Add(new CourseRole(CourseRole.OSBLERoles.Student.ToString(), false, false, false, true, false, false));

            // Moderator: No Special Privileges
            this.CourseRoles.Add(new CourseRole(CourseRole.OSBLERoles.Moderator.ToString(), false, false, false, false, false, false));

            // Observer: Can See All, All Anonymized
            this.CourseRoles.Add(new CourseRole(CourseRole.OSBLERoles.Observer.ToString(), false, true, false, false, false, true));

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

            createSampleUser("bob@smith.com", "123123", "Bob", "Smith", "1", 1, false, true);
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
            cu6.CourseRoleID = (int)CommunityRole.OSBLERoles.Leader;
            cu6.Section = 0;

            CoursesUsers cu7 = new CoursesUsers();
            cu7.CourseID = 3;
            cu7.UserProfileID = 2;
            cu7.CourseRoleID = (int)CommunityRole.OSBLERoles.Participant;
            cu7.Section = 0;

            CoursesUsers cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 4;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 5;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 1;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Instructor;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 6;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 7;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 8;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 9;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 10;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 11;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 12;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 13;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 14;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 15;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            cu8 = new CoursesUsers();
            cu8.CourseID = 4;
            cu8.UserProfileID = 16;
            cu8.CourseRoleID = (int)CourseRole.OSBLERoles.Student;
            cu8.Section = 0;
            this.CoursesUsers.Add(cu8);
            this.SaveChanges();

            this.CoursesUsers.Add(cu);
            this.CoursesUsers.Add(cu2);
            this.CoursesUsers.Add(cu3);
            this.CoursesUsers.Add(cu4);
            this.CoursesUsers.Add(cu5);
            this.CoursesUsers.Add(cu6);
            this.CoursesUsers.Add(cu7);

            this.SaveChanges();

            #endregion add course users
        }

        public DbSet<PeerReviewActivity> PeerReviewActivities { get; set; }

        public DbSet<IssueVotingActivity> IssueVotingActivities { get; set; }

        public DbSet<AuthorRebuttalActivity> AuthorRebuttalActivities { get; set; }

        public DbSet<AsyncIssueVotingActivity> AsyncIssueVotingActivities { get; set; }

        public DbSet<Submission> Submissions { get; set; }
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
