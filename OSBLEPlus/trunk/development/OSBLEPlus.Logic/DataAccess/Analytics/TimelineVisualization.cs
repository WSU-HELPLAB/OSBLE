using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using OSBIDE.Data.SQLDatabase;
using OSBLEPlus.Logic.DomainObjects.Analytics;
using OSBLEPlus.Logic.Utility;

namespace OSBLEPlus.Logic.DataAccess.Analytics
{
    public class TimelineVisualization
    {
        public static List<TimelineChartData> Get(TimelineCriteria criteria, bool? realtime)
        {
            using (var sqlConnection = new SqlConnection(StringConstants.ConnectionString))
            {
                sqlConnection.Open();

                var unspecifiedDate = new DateTime(2000, 1, 1);
                var timeFrom = criteria.timeFrom ?? unspecifiedDate;
                var timeTo = criteria.timeTo ?? unspecifiedDate;

                var timeoutVal = criteria.timeout ?? VisualizationParams.DEFAULT_TIMEOUT;


                var rawR = sqlConnection.Query<StateMachineEvent>(@"GetStateMachineEvents",
                    new
                    {
                        dateFrom = timeFrom,
                        dateTo = timeTo,
                        courseId = criteria.courseId
                    }, commandType: CommandType.StoredProcedure)
                    .ToList();

                sqlConnection.Close();

                if (timeFrom == unspecifiedDate) timeFrom = rawR.Min(x => x.EventDate);
                if (timeTo == unspecifiedDate) timeTo = rawR.Max(x => x.EventDate);

                var totalMinutes = (int)(timeTo - timeFrom).TotalMinutes;

                // chart data timeline
                var chartData = new List<TimelineChartData>();
                var currentSolution = string.Empty;

                // previous build error is used for figuring out the next state of start without debugging event
                var lastBuildErrorLogId = rawR.First().BuildErrorLogId;

                foreach (var r in rawR)
                {
                    // processing the flat events and compose a list of programming states for each user
                    // the composed list of programming states are ordered by the event time
                    // so the output is a collection of state timelines for the list of users
                    var currEventTime = r.EventDate;
                    var currEventPoint = (currEventTime - timeFrom).TotalMinutes;

                    // init local variables
                    ProgrammingState prevStateName;
                    if (string.Compare(r.SolutionName, currentSolution, StringComparison.OrdinalIgnoreCase) != 0)
                    {
                        currentSolution = r.SolutionName;
                        prevStateName = ProgrammingState.edit_syn_u_sem_u;
                    }

                    // process one user at a time
                    var userData = chartData.SingleOrDefault(x => x.UserId == r.SenderId);

                    // a new user's timeline?
                    if (userData == null)
                    {
                        // yes, create a new timeline entry
                        #region a new user timeline

                        // since each bullet svg is drawn independently
                        // the high level information needs to be passed into each svg drawing routine
                        userData = new TimelineChartData
                        {
                            UserId = r.SenderId,
                            title = string.Format("{0} {1}", r.FirstName, r.LastName),
                            markers = new List<Point>(),
                            measures = new List<State>(),
                        };

                        // is the first event in idle timeout range?
                        if ((currEventTime - timeFrom).TotalMinutes > timeoutVal)
                        {
                            // no, insert an idle state from the beginning to now
                            prevStateName = ProgrammingState.idle;
                            var uiproperties = TimelineStateDictionaries.UIProperties[prevStateName];
                            var idleState = new State
                            {
                                ProgrammingState = prevStateName,
                                Name = uiproperties.Label,
                                CssClass = criteria.grayscale ? uiproperties.CssGray : uiproperties.Css,
                                StartTime = timeFrom,
                                ShiftedStartTime = timeFrom,
                                StartPoint = 0,
                                EndTime = currEventTime,
                                EndPoint = currEventPoint,
                            };
                            userData.measures.Add(idleState);
                        }
                        else
                        {
                            // yes, insert a default programming state
                            prevStateName = ProgrammingState.edit_syn_u_sem_u;
                            var uiproperties = TimelineStateDictionaries.UIProperties[prevStateName];
                            var defaultState = new State
                            {
                                ProgrammingState = prevStateName,
                                Name = uiproperties.Label,
                                CssClass = criteria.grayscale ? uiproperties.CssGray : uiproperties.Css,
                                StartTime = timeFrom,
                                ShiftedStartTime = timeFrom,
                                StartPoint = 0,
                                EndTime = timeTo,
                                EndPoint = totalMinutes,
                            };
                            userData.measures.Add(defaultState);
                        }

                        chartData.Add(userData);

                        #endregion

                        // reset for a new user
                        lastBuildErrorLogId = null;
                    }

                    // is this a social event?
                    if (!string.IsNullOrWhiteSpace(r.MarkerType))
                    {
                        // yes, add social media event marker
                        userData.markers.Add(new Point { Name = r.MarkerType, Position = currEventPoint, EventTime = r.EventDate });
                    }
                    else
                    {
                        // no, transition to next state based on the previous state
                        // non-social event could be a state transition trigger

                        // first to check the timespan between current and previous non-idle state events

                        #region insert an idle state if the timespan between this event and previous event is too long

                        var prevState = userData.measures.Last();
                        var prevEventTime = prevState.ShiftedStartTime;
                        prevStateName = prevState.ProgrammingState;

                        if ((currEventTime - prevEventTime).TotalMinutes > timeoutVal)
                        {
                            if (prevStateName == ProgrammingState.idle)
                            {
                                // extend the initial idle state to avoid the extra rectangle
                                prevState.EndTime = currEventTime;
                                prevState.EndPoint = currEventPoint;
                            }
                            else
                            {
                                #region add a new idle state

                                prevStateName = ProgrammingState.idle;
                                var uiproperties = TimelineStateDictionaries.UIProperties[prevStateName];
                                var idleState = new State
                                {
                                    ProgrammingState = prevStateName,
                                    Name = uiproperties.Label,
                                    CssClass = criteria.grayscale ? uiproperties.CssGray : uiproperties.Css,
                                    StartTime = prevEventTime.AddMinutes(timeoutVal),
                                    StartPoint = (prevEventTime - timeFrom).TotalMinutes + timeoutVal,
                                    EndTime = currEventTime,
                                    EndPoint = currEventPoint,
                                };
                                userData.measures.Add(idleState);

                                // terminate previous non-idle state
                                prevState.EndTime = idleState.StartTime;
                                prevState.EndPoint = idleState.StartPoint;

                                prevState = idleState;

                                #endregion
                            }
                        }

                        #endregion

                        // resolve next state name from current state and current event
                        #region get next programming state from state dictionaries

                        var nextStateName = prevStateName;
                        if (prevStateName == ProgrammingState.idle && prevState.StartTime > timeFrom)
                        {
                            // if currently in idle but not the first idle state, use the last non-idle state to calculate the next state
                            prevStateName = userData.measures.Last(x => x.ProgrammingState != ProgrammingState.idle).ProgrammingState;
                        }
                        // look up next programming state from the transition tables
                        if (String.Compare(r.LogType, "BuildEvent", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            nextStateName = r.BuildErrorLogId.HasValue
                                            ? TimelineStateDictionaries.NextStateForBuildWithError[prevStateName]
                                            : TimelineStateDictionaries.NextStateForBuildWithoutError[prevStateName];

                            lastBuildErrorLogId = r.BuildErrorLogId.HasValue ? (int?)r.BuildErrorLogId.Value : null;
                        }
                        else if (String.Compare(r.LogType, "DebugEvent", StringComparison.OrdinalIgnoreCase) == 0 && r.ExecutionActionId.HasValue)
                        {
                            nextStateName = GetNextStateForDebugEvent(userData, prevStateName, r.ExecutionActionId.Value, lastBuildErrorLogId);

                            // the next event's previous event (this one) is not a build event
                            lastBuildErrorLogId = null;
                        }
                        else if (String.Compare(r.LogType, "ExceptionEvent", StringComparison.OrdinalIgnoreCase) == 0 && (prevStateName == ProgrammingState.debug_sem_u || prevStateName == ProgrammingState.idle))
                        {
                            nextStateName = TimelineStateDictionaries.NextStateForExceptionEvent[prevStateName];

                            // the next event's previous event (this one) is not a build event
                            lastBuildErrorLogId = null;
                        }
                        else if (TimelineStateDictionaries.EditorEvents.Any(x => String.Compare(x, r.LogType, StringComparison.OrdinalIgnoreCase) == 0))
                        {
                            nextStateName = TimelineStateDictionaries.NextStateForEditorEvent[prevStateName];
                        }

                        #endregion

                        // terminate previous state and append the next state if it's changed
                        if (nextStateName != prevState.ProgrammingState)
                        {
                            #region append new state

                            // idle state is instantiated with a valid termination date
                            // when idle state is created, it also terminates the previous state (timeout)
                            if (prevState.EndTime == timeTo)
                            {
                                // terminate previous non-idle state
                                prevState.EndTime = currEventTime;
                                prevState.EndPoint = currEventPoint;
                            }

                            var uiproperties = TimelineStateDictionaries.UIProperties[nextStateName];
                            userData.measures.Add(new State
                            {
                                // new state
                                ProgrammingState = nextStateName,
                                Name = uiproperties.Label,
                                CssClass = criteria.grayscale ? uiproperties.CssGray : uiproperties.Css,
                                StartTime = currEventTime,
                                ShiftedStartTime = currEventTime,
                                StartPoint = currEventPoint,
                                EndTime = timeTo,
                                EndPoint = totalMinutes,
                            });

                            #endregion
                        }
                        else
                        {
                            // update continous state start time so it won't timeout to idle state
                            prevState.ShiftedStartTime = currEventTime;
                        }
                    }
                }

                #region append idle state to the end if necessary

                chartData.ForEach(x =>
                {
                    var lastMeasure = x.measures.Last();
                    if (lastMeasure.EndTime == timeTo && (timeTo - lastMeasure.ShiftedStartTime).TotalMinutes > timeoutVal)
                    {
                        // prepare a new idle state for the end
                        var uiproperties = TimelineStateDictionaries.UIProperties[ProgrammingState.idle];
                        var idleState = new State
                        {
                            ProgrammingState = ProgrammingState.idle,
                            Name = uiproperties.Label,
                            CssClass = criteria.grayscale ? uiproperties.CssGray : uiproperties.Css,
                            StartTime = lastMeasure.StartTime.AddMinutes(timeoutVal),
                            StartPoint = lastMeasure.StartPoint + timeoutVal,
                            EndTime = lastMeasure.EndTime,
                            EndPoint = lastMeasure.EndPoint,
                        };
                        x.measures.Add(idleState);

                        // terminate the lastMeasure state
                        lastMeasure.EndTime = idleState.StartTime;
                        lastMeasure.EndPoint = idleState.StartPoint;
                    }
                    else if (lastMeasure.ProgrammingState == ProgrammingState.idle)
                    {
                        lastMeasure.EndTime = timeTo;
                        lastMeasure.EndPoint = totalMinutes;
                    }
                });

                #endregion

                // only show horizontal x-axis for the last series
                if (chartData.Count > 0) chartData.Last().showTicks = true;

                return chartData;
            }
        }

        private static ProgrammingState GetNextStateForDebugEvent(TimelineChartData userData, ProgrammingState prevStateName, int executionAction, int? prevBuildErrorLogId)
        {
            ProgrammingState nextStateName;

            if (executionAction == (int)DebugActions.StopDebugging)
            {
                // set the next state for stop debugging event
                // based on the editing state before current debugging state
                if (userData.measures.LastOrDefault(x => x.ProgrammingState == ProgrammingState.edit_syn_y_sem_n) != null)
                {
                    nextStateName = ProgrammingState.edit_syn_y_sem_n;
                }
                else
                {
                    nextStateName = ProgrammingState.edit_syn_y_sem_u;
                }
            }
            else if (executionAction == (int)DebugActions.StartWithoutDebugging)
            {
                if (prevBuildErrorLogId.HasValue)
                {
                    nextStateName = ProgrammingState.run_last_success;
                }
                else
                {
                    nextStateName = TimelineStateDictionaries.NextStateForStartWithoutDebugging[prevStateName];
                }
            }
            else
            {
                // the rest of the exectution actions including start, stepOver, stepInto, ans stepOut
                var lastState = userData.measures.Last();
                if (lastState.ProgrammingState == ProgrammingState.debug_sem_n)
                {
                    // should not have semantic error if the debugging state can continue
                    lastState.ProgrammingState = ProgrammingState.debug_sem_u;
                }

                nextStateName = TimelineStateDictionaries.NextStateForDebug[prevStateName];
            }

            return nextStateName;
        }
    }
}
