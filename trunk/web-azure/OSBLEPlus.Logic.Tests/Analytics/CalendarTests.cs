using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.DataAccess.Analytics;
using OSBLEPlus.Logic.DomainObjects.Analytics;

namespace OSBLEPlus.Logic.Tests.Analytics
{
    [TestClass]
    public class CalendarTests
    {
        [TestMethod]
        public void TestGetDailyAggregates()
        {
            var results = Calendar.GetDailyAggregates(
                                    new DateTime(2014,1,1),
                                    new DateTime(2014,4,1), 
                                    null,
                                    1,
                                    "ActiveStudents,LinesOfCodeWritten,TimeSpent,NumberOfCompilations,NumberOfErrorsPerCompilation,NumberOfNoDebugExecutions,NumberOfDebugExecutions,NumberOfBreakpointsSet,NumberOfRuntimeExceptions,NumberOfPosts,NumberOfReplies,TimeToFirstReply",
                                    false);

            Assert.IsNotNull(results);
            Assert.IsTrue(results.GetType() == typeof(DailyAggregations));
        }

        [TestMethod]
        public void GetHourlyAggregates()
        {
            var results = Calendar.GetHourlyAggregates(
                                    new DateTime(2014,2,16),
                                    null,
                                    1,
                                    "ActiveStudents,LinesOfCodeWritten,TimeSpent,NumberOfCompilations,NumberOfErrorsPerCompilation,NumberOfNoDebugExecutions,NumberOfDebugExecutions,NumberOfBreakpointsSet,NumberOfRuntimeExceptions,NumberOfPosts,NumberOfReplies,TimeToFirstReply",
                                    false);

            Assert.IsNotNull(results);
            Assert.IsTrue(results.GetType() == typeof(HourlyAggregations));
        }
    }
}
