using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Reflection;

using OSBLE.Models.AbstractCourses;
using OSBLE.Models.AbstractCourses.Course;
using OSBLE.Models.Annotate;
using OSBLE.Models.Assessments;
using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.DiscussionAssignment;
using OSBLE.Models.HomePage;
using OSBLE.Models.Users;

namespace OSBLE.Models
{
    /// <summary>
    /// Contains all of the tables used in the OSBLE database.
    /// </summary>
    public abstract class ContextBase : DbContext
    {
        protected ContextBase()
        {
        }

        protected ContextBase(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
        }

        public DbSet<Setting> Settings { get; set; }

        public DbSet<School> Schools { get; set; }

        // Assignments

        public DbSet<CommentCategory> CommentCategories { get; set; }

        public DbSet<CommentCategoryConfiguration> CommentCategoryConfigurations { get; set; }

        public DbSet<CommentCategoryOption> CommentCategoryOptions { get; set; }

        public DbSet<Assignment> Assignments { get; set; }

        public DbSet<AssignmentTeam> AssignmentTeams { get; set; }

        public DbSet<DiscussionTeam> DiscussionTeams { get; set; }

        public DbSet<ReviewTeam> ReviewTeams { get; set; }

        public DbSet<Team> Teams { get; set; }

        public DbSet<TeamMember> TeamMembers { get; set; }

        public DbSet<DiscussionSetting> DiscussionSettings { get; set; }

        public DbSet<CriticalReviewSettings> CriticalReviewSettings { get; set; }

        public DbSet<TeamEvaluation> TeamEvaluations { get; set; }

        public DbSet<TeamEvaluationComment> TeamEvaluationComments { get; set; }

        public DbSet<TeamEvaluationSettings> TeamEvaluationSettings { get; set; }

        public DbSet<AbetSubmissionTag> AbetSubmissionLevels { get; set; }

        public DbSet<AbetAssignmentOutcome> AbetSubmissionOutcomes { get; set; }

        public DbSet<Deliverable> Deliverables { get; set; }

        // Assessments
        public DbSet<Assessment> Assessments { get; set; }

        // Annotate stuff
        public DbSet<AnnotateDocumentReference> AnnotateDocumentReferences { get; set; }

        // DiscussionAssignments

        public DbSet<DiscussionPost> DiscussionPosts { get; set; }

        public DbSet<DiscussionAssignmentMetaInfo> DiscussionAssignmentMetaTable { get; set; }

        // Courses

        public DbSet<AbstractCourse> AbstractCourses { get; set; }

        public DbSet<AbstractRole> AbstractRoles { get; set; }

        public DbSet<AssessmentCommittee> Committees { get; set; }

        public DbSet<AssessmentCommitteeRole> CommitteeRoles { get; set; }

        public DbSet<Community> Communities { get; set; }

        public DbSet<CommunityRole> CommunityRoles { get; set; }

        public DbSet<Course> Courses { get; set; }

        public DbSet<CourseBreak> CourseBreaks { get; set; }

        public DbSet<CourseMeeting> CourseMeetings { get; set; }

        public DbSet<CourseRole> CourseRoles { get; set; }

        public DbSet<CourseUser> CourseUsers { get; set; }

        public DbSet<WhiteTable> WhiteTable { get; set; }

        public DbSet<WhiteTableUser> WhiteTableUsers { get; set; }

        // HomePage

        public DbSet<AbstractDashboard> AbstractDashboards { get; set; }

        public DbSet<DashboardPost> DashboardPosts { get; set; }

        public DbSet<DashboardReply> DashboardReplies { get; set; }

        public DbSet<Event> Events { get; set; }

        //ical
        public DbSet<icalEvent> icalEvents { get; set; }

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

        public DbSet<ProfileImage> ProfileImages { get; set; }

        //misc
        public DbSet<ActivityLog> ActivityLogs { get; set; }


        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

#if !DEBUG
            modelBuilder.Conventions.Remove<IncludeMetadataConvention>();
#endif

            //load in any model builder extensions (usually foreign key relationships)
            //from the models
            var componentObjects = (from type in Assembly.GetExecutingAssembly().GetTypes()
                                           where
                                           type.GetInterface("IModelBuilderExtender") != null
                                           &&
                                           type.IsInterface == false
                                           &&
                                           type.IsAbstract == false
                                           select type).ToList();
            foreach (Type component in componentObjects)
            {
                IModelBuilderExtender builder = Activator.CreateInstance(component) as IModelBuilderExtender;
                builder.BuildRelationship(modelBuilder);
            }
        }
    }
}
