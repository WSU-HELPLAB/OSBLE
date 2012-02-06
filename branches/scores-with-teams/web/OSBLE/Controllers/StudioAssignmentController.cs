using System;
using System.Collections.Generic;
using System.Web.Mvc;
using OSBLE.Attributes;
using OSBLE.Models;
using OSBLE.Models.ViewModels;

namespace OSBLE.Controllers
{
    [Authorize]
    [RequireActiveCourse]
    [NotForCommunity]
    public class StudioAssignmentController : OSBLEController
    {
        //
        // GET: /Studio/

        public ActionResult Index()
        {
            return View();
        }

        public ViewResult SilverlightTest()
        {
            StudioAssignmentViewModel viewModel = new StudioAssignmentViewModel();

            viewModel.StudioTimelineCreation = new SilverlightObject
            {
                CSSId = "studio_timeline",
                XapName = "StudioTimelineCreation",
                Width = "820",
                Height = "430",
                Parameters = new Dictionary<string, string>()
                {
                }
            };

            /*
            // Create sample activities
            List<SerializableActivity> activities = new List<SerializableActivity>();

            activities.Add(new SerializableActivity()
            {
                DateTime = DateTime.Now,
                ActivityType = ActivityTypes.Submission
            });

            activities.Add(new SerializableActivity()
            {
                DateTime = DateTime.Now.AddDays(3).AddHours(3),
                ActivityType = ActivityTypes.Rebuttal
            });

            activities.Add(new SerializableActivity()
            {
                DateTime = DateTime.Now.AddDays(7),
                ActivityType = ActivityTypes.PeerReview
            });

            activities.Add(new SerializableActivity()
            {
                DateTime = DateTime.Now.AddDays(11),
                ActivityType = ActivityTypes.Stop
            });

            //gives the sample data to the viewModel
            viewModel.SerializedActivitiesJSON = viewModel.StudioTimelineCreation.Parameters["activities"] = Uri.EscapeDataString(JsonConvert.SerializeObject(activities));
            */

            return View(viewModel);
        }

        private class SerializableActivity
        {
            public DateTime DateTime { get; set; }

            public ActivityTypes ActivityType { get; set; }
        }

        private enum ActivityTypes
        {
            Submission,
            PeerReview,
            Voting,
            Rebuttal,
            Stop
        }
    }
}