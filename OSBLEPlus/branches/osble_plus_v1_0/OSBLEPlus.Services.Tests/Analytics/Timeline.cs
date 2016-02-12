using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Services.Controllers;

namespace OSBLEPlus.Services.Tests.Analytics
{
    [TestClass]
    public class Timeline
    {
        [TestMethod]
        public void CanGetTimeline()
        {
            string uids = "";
            for (int i = 15; i <= 20; i++)
            {
                uids += i.ToString();
                if (i < 20)
                    uids += ",";
            }

            var result = new TimelineController().Get(new TimelineCriteria()
            {
                courseId = 1,
                grayscale = false,
                timeFrom = new DateTime(2014, 1, 1),
                timeTo = new DateTime(2014, 2, 28),
                timeScale = TimeScale.Days,
                timeout = 3,
                userIds = uids
            });

            Assert.IsNotNull(result);
            Assert.IsTrue(result.GetType() == typeof(System.Collections.Generic.List<TimelineChartData>));
        }
    }
}
