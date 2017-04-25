using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Logic.Utility;
using OSBLEPlus.Services.Controllers;

namespace OSBLEPlus.Services.Tests.Analytics
{
    [TestClass]
    public class Calendar
    {
        [TestMethod]
        public void CanGetDailyAggregations()
        {
            var results = new CalendarController().Get(
                new CalendarAttributes
                {
                    AggregateFunctionId = AggregateFunction.Total,
                    CourseId = 1,
                    ReferenceDate = new DateTime(2014, 1, 1),
                    SelectedMeasures = "ActiveStudents,LinesOfCodeWritten,TimeSpent,NumberOfCompilations,NumberOfErrorsPerCompilation,NumberOfNoDebugExecutions,NumberOfDebugExecutions,NumberOfBreakpointsSet,NumberOfRuntimeExceptions,NumberOfPosts,NumberOfReplies,TimeToFirstReply",
                    SubjectUsers = null
                });

            Assert.IsNotNull(results);
            Assert.IsTrue(results.GetType() == typeof(DailyAggregations));
        }
    }
}
