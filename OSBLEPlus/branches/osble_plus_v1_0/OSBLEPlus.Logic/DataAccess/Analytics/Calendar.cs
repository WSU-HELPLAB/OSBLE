using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using Dapper;

using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DataAccess.Analytics
{
    public class Calendar
    {
        public class MeasureByDay
        {
            public DateTime EventDay { get; set; }
            public int Value { get; set; }
            public string Measure { get; set; }
            public MeasureByDay() { } // NOTE!! This is required by Dapper ORM
        }

        public class MeasureByHour
        {
            public int EventHour { get; set; }
            public int Value { get; set; }
            public string Measure { get; set; }
            public MeasureByHour() { } // NOTE!! This is required by Dapper ORM
        }

        public static DailyAggregations GetDailyAggregates(DateTime startDate, DateTime endDate, List<int> users, int courseId, string selectedMeasures, bool isAvg)
        {
            using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {
                sqlConnection.Open();

                var dailyAggregatesRaw = sqlConnection.Query<MeasureByDay>(@"GetCalendarMeasuresByDay",
                    new
                    {
                        StartDate = startDate,
                        EndDate = endDate,
                        Students = string.Join(",", users != null ? users.Where(i => i > 0).ToArray() : new int[0]),
                        CourseId = courseId,
                        Measures = string.IsNullOrWhiteSpace(selectedMeasures) ? string.Empty : selectedMeasures,
                        IsAvg = isAvg
                    }, commandType: CommandType.StoredProcedure)
                    .ToList()
                    .GroupBy(x => x.Measure)
                    .ToDictionary(x => x.Key, x => x.ToList());

                sqlConnection.Close();

                if (dailyAggregatesRaw.Count <= 0) return null;

                var measureDictionary = MeasureDefinitions.All.Values.SelectMany(x => x).ToList();

                var activities = dailyAggregatesRaw.Values.SelectMany(x => x).ToList()
                    .Where(x => x.Value < 0)
                    // month starts from 0 to 11 in JavaScript!!!
                    .Select(x => new Activity { Day = x.EventDay.Day, Month = x.EventDay.Month - 1, Name = x.Measure })
                    .ToList();

                var measures = (from key in dailyAggregatesRaw.Keys
                                where dailyAggregatesRaw[key].Any(x => x.Value > 0)
                                let ms = measureDictionary.Single(x => x.MeasureType.ToString() == key)
                                select new Measure
                                {
                                    Title = key.ToDisplayText(),
                                    DataPointShape = ms.DataPointShape,
                                    Color = ms.Color,
                                    // month starts from 0 to 11 in JavaScript!!!
                                    Aggregates = dailyAggregatesRaw[key].Select(x => new Aggregate
                                    {
                                        Day = x.EventDay.Day,
                                        Month = x.EventDay.Month - 1,
                                        Value = x.Value
                                    }).ToList(),
                                    FirstDataPointMonth = dailyAggregatesRaw[key].Min(x => x.EventDay).Month - 1,
                                    FirstDataPointDay = dailyAggregatesRaw[key].Min(x => x.EventDay).Day,
                                    LastDataPointMonth = dailyAggregatesRaw[key].Max(x => x.EventDay).Month - 1,
                                    LastDataPointDay = dailyAggregatesRaw[key].Max(x => x.EventDay).Day,
                                    Avg = dailyAggregatesRaw[key].Average(x => x.Value),
                                    Max = dailyAggregatesRaw[key].Max(x => x.Value),
                                    Min = dailyAggregatesRaw[key].Min(x => x.Value),
                                })
                    .ToList();

                return new DailyAggregations { Year = startDate.Year, Month = startDate.Month, Activities = activities, Measures = measures };
            }
        }

        public static HourlyAggregations GetHourlyAggregates(DateTime date, List<int> users, int courseId, string selectedMeasures, bool isAvg)
        {
            using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {
                sqlConnection.Open();

                var hourlyAggregatesRaw = sqlConnection.Query<MeasureByHour>(@"GetCalendarMeasuresByHour",
                    new
                    {
                        EventDate = date,
                        Students = string.Join(",", users != null ? users.Where(i => i > 0).ToArray() : new int[0]),
                        CourseId = courseId,
                        Measures = string.IsNullOrWhiteSpace(selectedMeasures) ? string.Empty : selectedMeasures,
                        IsAvg = isAvg
                    }, commandType: CommandType.StoredProcedure)
                    .ToList()
                    .GroupBy(x => x.Measure)
                    .ToDictionary(x => x.Key, x => x.ToList());

                sqlConnection.Close();

                if (hourlyAggregatesRaw.Count <= 0) return null;

                var measureDictionary = MeasureDefinitions.All.Values.SelectMany(x => x).ToList();
                var hoursInADay = new List<int>(); for (var i = 1; i < 25; i++) hoursInADay.Add(i);

                var measures = new List<HourlyMeasures>();
                foreach (var key in hourlyAggregatesRaw.Keys)
                {
                    if (!hourlyAggregatesRaw[key].Any(x => x.Value > 0)) continue;

                    var ms = measureDictionary.Single(x => x.MeasureType.ToString() == key);
                    var hv = new List<HourlyMeasure>();
                    hoursInADay.ForEach(x =>
                    {
                        var v = hourlyAggregatesRaw[key].SingleOrDefault(y => y.EventHour == x);
                        hv.Add(new HourlyMeasure { Hour = x, Value = v != null ? v.Value : 0 });
                    });

                    measures.Add(
                        new HourlyMeasures
                        {
                            Title = key.ToDisplayText(),
                            Color = ms.Color,
                            Values = hv,
                            Avg = hourlyAggregatesRaw[key].Average(x => x.Value),
                            Max = hourlyAggregatesRaw[key].Max(x => x.Value),
                            Min = hourlyAggregatesRaw[key].Min(x => x.Value),
                        });
                }

                return new HourlyAggregations { Measures = measures, Max = (int)measures.Max(x => x.Max) + 1 };
            }
        }
    }
}
