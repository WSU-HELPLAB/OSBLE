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
using OSBLE.Models.Annotate;
using System.Reflection;
using OSBLE.Models.Triggers;

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
            init();
        }

        public ContextBase(DbConnection existingConnection, DbCompiledModel model, bool contextOwnsConnection)
            : base(existingConnection, model, contextOwnsConnection)
        {
            init();
        }

        public ContextBase(string nameOrConnectionString, DbCompiledModel model)
            : base(nameOrConnectionString, model)
        {
            init();
        }

        public ContextBase(ObjectContext objectContext, bool dbContextOwnsObjectContext)
            : base(objectContext, dbContextOwnsObjectContext)
        {
            init();
        }

        public ContextBase(DbConnection existingConnection, bool contextOwnsConnection)
            : base(existingConnection, contextOwnsConnection)
        {
            init();
        }

        public ContextBase(string nameOrConnectionString)
            : base(nameOrConnectionString)
        {
            init();
        }

        public ContextBase(DbCompiledModel model)
            : base(model)
        {
            init();
        }

        private void init()
        {
            //AC: Not sure if the best method to accomplish this, but I'd like to 
            //set up database triggers automatically.  As I couldn't find any
            //EF-based event handler that I could hook into, my solution is to 
            //create a trigger-based setting to track whether or not triggers
            //have been set up.
            Setting setting = this.Settings.Where(s => s.Key == "TriggerInit").FirstOrDefault();
            if (setting == null)
            {
                setting = new Setting();
                setting.Value = "0";
                setting.Key = "TriggerInit";
            }
            if (setting.Value == "0")
            {
                try
                {
                    //load all triggers using reflection
                    List<Type> componentObjects = (from type in Assembly.GetExecutingAssembly().GetTypes()
                                                   where
                                                   type.IsSubclassOf(typeof(ModelTrigger)) == true
                                                   &&
                                                   type.IsAbstract == false
                                                   select type).ToList();
                    foreach (Type component in componentObjects)
                    {
                        ModelTrigger trigger = Activator.CreateInstance(component) as ModelTrigger;
                        trigger.CreateTrigger(this);
                    }
                    setting.Value = "1";
                }
                catch (Exception)
                {
                }
            }
            this.SaveChanges();
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

            // Withdrawn: The student has withdrawn from the course
            this.CourseRoles.Add(new CourseRole(CourseRole.CourseRoles.Withdrawn.ToString(), false, false, false, false, false, false));

            // Community Roles

            // Leader: Can Modify Community
            this.CommunityRoles.Add(new CommunityRole(CommunityRole.OSBLERoles.Leader.ToString(), true, true, true, true));

            // Participant: Cannot Modify Community
            this.CommunityRoles.Add(new CommunityRole(CommunityRole.OSBLERoles.Participant.ToString(), false, true, true, false));

            //trusted communityt member: same as participant, but can upload files to the server
            this.CommunityRoles.Add(new CommunityRole(CommunityRole.OSBLERoles.TrustedCommunityMember.ToString(), false, true, true, true));

            // Assessment committee roles (don't change statement order here)
            this.CommitteeRoles.Add(new AssessmentCommitteeChairRole());
            this.CommitteeRoles.Add(new AssessmentCommitteeMemberRole());
            this.CommitteeRoles.Add(new ABETEvaluatorRole());
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
            List<Type> componentObjects = (from type in Assembly.GetExecutingAssembly().GetTypes()
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
