using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using OSBLE.Models.Assignments;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Models.Courses;
using OSBLE.Models.HomePage;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.Users;
using OSBLE.Models.AbstractCourses;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Data.Objects;

namespace OSBLE.Models
{
    /// <summary>
    /// Contains all of the tables used in the OSBLE database.
    /// </summary>
    public abstract class ContextBase : DbContext
    {
        public ContextBase()
            : base()
        {
        }

        public ContextBase(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
        }

        public ContextBase(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
        }

        public ContextBase(ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
        }

        public ContextBase(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
        }

        public ContextBase(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public ContextBase(DbCompiledModel model)
            : base(model)
        {
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

        public DbSet<TeamEvaluationComment> TeamEvaluationComments { get; set; }

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

        //misc
        public DbSet<ActivityLog> ActivityLogs { get; set; }

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

            modelBuilder.Entity<DiscussionPost>()
                .HasRequired(n => n.DiscussionTeam)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<DiscussionSetting>()
                .HasRequired(ds => ds.Assignment)
                .WithOptional(a => a.DiscussionSettings)
                .WillCascadeOnDelete(false);

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

            //We want cascade delete on Rubric_Level
            //                          Rubric_Criterion
            //                          Criterion_CellDescription
            //                          Level_CellDescription (FALSE)

            modelBuilder.Entity<Level>()
                .HasRequired(l => l.Rubric)
                .WithMany(r => r.Levels)
                .HasForeignKey(l => l.RubricID)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<Criterion>()
                .HasRequired(c => c.Rubric)
                .WithMany(r => r.Criteria)
                .HasForeignKey(c => c.RubricID)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<CellDescription>()
                .HasRequired(cd => cd.Criterion)
                .WithMany()
                .HasForeignKey(cd => cd.CriterionID)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<CellDescription>()
                .HasRequired(cd => cd.Level)
                .WithMany()
                .HasForeignKey(cd => cd.LevelID)
                .WillCascadeOnDelete(true);
            /////

            modelBuilder.Entity<RubricEvaluation>()
                .HasRequired(re => re.Recipient)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamEvaluationSettings>()
                .HasRequired(tes => tes.Assignment)
                .WithOptional(a => a.TeamEvaluationSettings)
                .WillCascadeOnDelete(true);

            modelBuilder.Entity<TeamEvaluation>()
                .HasRequired(tm => tm.Evaluator)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamEvaluation>()
                .HasRequired(tm => tm.Recipient)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamEvaluation>()
                .HasRequired(tm => tm.AssignmentUnderReview)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamEvaluation>()
                .HasRequired(tm => tm.TeamEvaluationAssignment)
                .WithMany()
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<TeamMember>()
                .HasRequired(tm => tm.CourseUser)
                .WithMany(cu => cu.TeamMemberships)
                .WillCascadeOnDelete(true);

        }
    }
}
