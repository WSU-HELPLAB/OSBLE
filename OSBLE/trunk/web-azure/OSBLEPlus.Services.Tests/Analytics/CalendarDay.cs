using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OSBLEPlus.Logic.DomainObjects;
using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Services.Controllers;

namespace OSBLEPlus.Services.Tests.Analytics
{
    [TestClass]
    public class CalendarDay
    {
        [TestMethod]
        public void CanGetHourlyAggregations()
        {
            var results = new CalendarDayController().Get(
                new CalendarAttributes
                {
                    AggregateFunctionId = AggregateFunction.Total,
                    CourseId = 1,
                    ReferenceDate = new DateTime(2014, 2, 16),
                    SelectedMeasures = "ActiveStudents,LinesOfCodeWritten,TimeSpent,NumberOfCompilations,NumberOfErrorsPerCompilation,NumberOfNoDebugExecutions,NumberOfDebugExecutions,NumberOfBreakpointsSet,NumberOfRuntimeExceptions,NumberOfPosts,NumberOfReplies,TimeToFirstReply",
                    SubjectUsers = null
                });

            Assert.IsNotNull(results);
            Assert.IsTrue(results.GetType() == typeof(HourlyAggregations));
        }
    }
}
