using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLEPlus.Logic.DomainObjects;
using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Services.Controllers;

namespace OSBLEPlus.Services.Tests.Analytics
{
    [TestClass]
    public class Calendar
    {
        [TestMethod]
        public void CanGetDailyAggregations()
        {
            var results = new CalendarController().GetMeasures(
                                    AggregateFunction.Total,
                                    1,
                                    new DateTime(2014, 1, 1),
                                    new DateTime(2014, 4, 1),
                                    "ActiveStudents,LinesOfCodeWritten,TimeSpent,NumberOfCompilations,NumberOfErrorsPerCompilation,NumberOfNoDebugExecutions,NumberOfDebugExecutions,NumberOfBreakpointsSet,NumberOfRuntimeExceptions,NumberOfPosts,NumberOfReplies,TimeToFirstReply",
                                    null);

            Assert.IsNotNull(results);
            Assert.IsTrue(results.GetType() == typeof(DailyAggregations));
        }

        [TestMethod]
        public void CanGetHourlyAggregations()
        {
            var results = new CalendarController().GetHourlyMeasures(
                                    AggregateFunction.Total,
                                    1,
                                    2014, 2, 16,
                                    "ActiveStudents,LinesOfCodeWritten,TimeSpent,NumberOfCompilations,NumberOfErrorsPerCompilation,NumberOfNoDebugExecutions,NumberOfDebugExecutions,NumberOfBreakpointsSet,NumberOfRuntimeExceptions,NumberOfPosts,NumberOfReplies,TimeToFirstReply",
                                    null);

            Assert.IsNotNull(results);
            Assert.IsTrue(results.GetType() == typeof(HourlyAggregations));
        }
    }
}
