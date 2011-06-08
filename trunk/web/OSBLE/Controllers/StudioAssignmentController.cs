using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OSBLE.Models;
using System.Runtime.Serialization;
using OSBLE.Models.ViewModels;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Text;

namespace OSBLE.Controllers
{
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
                Height = "430"
            };

            // Create sample activities
            List<SerializableActivity> activities = new List<SerializableActivity>();

            activities.Add(new SerializableActivity() {
                DateTime = DateTime.Now,
                ActivityType = ActivityTypes.Submission
            });

            activities.Add(new SerializableActivity()
            {
                DateTime = DateTime.Now.AddDays(7),
                ActivityType = ActivityTypes.PeerReview
            });

            //viewModel.SerializedActivitiesJSON = JsonSerializer.SerializeToString(activities);

            return View(viewModel);
        }

        [DataContract]
        private class SerializableActivity
        {
            [DataMember]
            public DateTime DateTime { get; set; }

            [DataMember]
            public ActivityTypes ActivityType { get; set; }
        }

        [DataContract]
        private enum ActivityTypes
        {
            [EnumMember]
            Submission,
            [EnumMember]
            PeerReview,
            [EnumMember]
            Voting,
            [EnumMember]
            Rebuttal,
            [EnumMember]
            Stop
        }

    }
}
