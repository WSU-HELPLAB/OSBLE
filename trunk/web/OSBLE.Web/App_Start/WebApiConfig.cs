using OSBLE.Models.Assignments;
using OSBLE.Models.Courses;
using OSBLE.Models.Courses.Rubrics;
using OSBLE.Models.HomePage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.OData.Builder;

namespace OSBLE.Web
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            ODataModelBuilder modelBuilder = new ODataConventionModelBuilder();
            modelBuilder.EntitySet<Course>("Courses");
            modelBuilder.EntitySet<CourseMeeting>("CourseMeetings");
            modelBuilder.EntitySet<CourseBreak>("CourseBreaks");
            modelBuilder.EntitySet<Assignment>("Assignments");
            modelBuilder.EntitySet<Rubric>("Rubric");
            modelBuilder.EntitySet<CommentCategoryConfiguration>("CommentCategory");
            modelBuilder.EntitySet<Deliverable>("Deliverables");
            modelBuilder.EntitySet<AssignmentTeam>("AssignmentTeams");
            modelBuilder.EntitySet<DiscussionTeam>("DiscussionTeams");
            modelBuilder.EntitySet<DiscussionSetting>("DiscussionSettings");
            modelBuilder.EntitySet<ReviewTeam>("ReviewTeams");
            modelBuilder.EntitySet<CriticalReviewSettings>("CriticalReviewSettings");
            modelBuilder.EntitySet<TeamEvaluationSettings>("TeamEvaluationSettings");
            modelBuilder.EntitySet<AbetAssignmentOutcome>("AbetOutcomes");
            modelBuilder.EntitySet<Event>("AssociatedEvent");


            modelBuilder.Namespace = "OSBLE.Controllers.Odata";
            Microsoft.Data.Edm.IEdmModel model = modelBuilder.GetEdmModel();
            config.Routes.MapODataRoute("ODataRoute", "odata", model);
        }
    }
}
