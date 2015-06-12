/*

exec [dbo].[GetCalendarMeasuresByHour] @eventDate='2014-02-16',@students='',@courseId=1,@measures='NumberOfCompilations',@isAvg=0

*/
---------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------
-- sproc [dbo].[GetCalendarMeasuresByHour]
---------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[Getcalendarmeasuresbyhour] @eventDate DATETIME='02-01-2014',
                                                   @students  VARCHAR(max)='',
                                                   @courseId  INT=-1,
                                                   @measures  VARCHAR(2000)='',
                                                   @isAvg     BIT=0
AS
  BEGIN
      SET NOCOUNT ON;

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Base events
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- the @students list is never empty since the view is launched for a specific search result set
      IF Object_id('tempdb..#UserIdsTable') IS NOT NULL
        DROP TABLE #UserIdsTable

      CREATE TABLE #UserIdsTable
        (
           UserId INT
        )

      INSERT INTO #UserIdsTable
      SELECT UserId=Cast(Items AS INT)
      FROM   [dbo].[Split](Isnull(@students, ''), ',')

      IF NOT EXISTS (SELECT 1
                     FROM   #UserIdsTable)
         AND @courseId > 0
        INSERT INTO #UserIdsTable
        SELECT UserId=UserProfileID
        FROM   dbo.CourseUsers
        WHERE  AbstractCourseID = @courseId

      CREATE CLUSTERED INDEX IX_Temp_Users_Hour
        ON #UserIdsTable(UserId)

      IF Object_id('tempdb..#EventLogs') IS NOT NULL
        DROP TABLE #EventLogs

      CREATE TABLE #EventLogs
        (
           EventLogId   INT,
           EventTypeId  INT,
           EventTime    DATETIME,
           UserId       INT,
           BuildEventId INT,
           EventHour    DATETIME
        )

      DECLARE @eventDateEnd DATETIME=Dateadd(day, 1, @eventDate);

      INSERT INTO #EventLogs
      SELECT EventLogId=l.Id,
             l.EventTypeId,
             EventTime=l.EventDate,
             UserId=l.SenderId,
             BuildEventId=b.Id,
             EventHour=Dateadd(hh, Datediff(hh, 0, l.EventDate), 0)
      FROM   [dbo].[EventLogs] l
             INNER JOIN #UserIdsTable u
                     ON u.UserId = l.SenderId
             LEFT JOIN [dbo].[BuildEvents] b
                    ON b.EventLogId = l.Id
      WHERE  EventTypeId IN ( 1, 2, 4, 6,
                              7, 9, 10 )
             AND l.EventDate >= @eventDate
             AND l.EventDate < @eventDateEnd
      ORDER  BY EventTime

      CREATE NONCLUSTERED INDEX IX_Temp_EventLogs_Hour
        ON [#EventLogs] ([EventTypeId], [EventTime], [BuildEventId])
        include ([UserId])

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Base programming events with event timespane of users (learn about lag)
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Object_id('tempdb..#ProgrammingEvents') IS NOT NULL
        DROP TABLE #ProgrammingEvents

      --select UserId, EventTypeId, EventTime,
      --		lag(EventTime, 1, null) over(partition by UserId order by EventTime desc) as NextEventTime,
      --		EventHour
      --into #ProgrammingEvents
      --from #EventLogs
      --where EventTypeId in (2, 4, 10)
      IF Object_id('tempdb..#ProgrammingEvents') IS NOT NULL
        DROP TABLE #ProgrammingEvents

      CREATE TABLE #ProgrammingEvents
        (
           UserId        INT,
           EventTime     DATETIME,
           NextEventTime DATETIME,
           EventHour     DATETIME
        )

      INSERT INTO #ProgrammingEvents
      SELECT e.UserId,
             e.EventTime,
             NextEventTime=en.EventTime,
             e.EventHour
      FROM   (SELECT UserId,
                     EventTime,
                     EventHour,
                     Row_number()
                       OVER(
                         partition BY UserId
                         ORDER BY EventTime DESC) AS eRow
              FROM   #EventLogs e
              WHERE  EventTypeId IN ( 2, 4, 10 )) e
             INNER JOIN (SELECT UserId,
                                EventTime,
                                Row_number()
                                  OVER(
                                    partition BY UserId
                                    ORDER BY EventTime DESC) AS eRow
                         FROM   #EventLogs e
                         WHERE  EventTypeId IN ( 2, 4, 10 )) en
                     ON en.eRow + 1 = e.eRow
                        AND en.UserId = e.UserId

      CREATE NONCLUSTERED INDEX TempProgrammingEvents
        ON [#ProgrammingEvents] ([NextEventTime])
        include ([UserId], [EventTime])

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Return measures
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Object_id('tempdb..#Measures') IS NOT NULL
        DROP TABLE #Measures

      CREATE TABLE #Measures
        (
           EventHour INT,
           Value     FLOAT,
           Measure   VARCHAR(200)
        )

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Active Students: distinct student counts for a day
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF @isAvg = 0
         AND Charindex('ActiveStudents', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=Count(DISTINCT UserId),
               Measure='ActiveStudents'
        FROM   #ProgrammingEvents
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Time Spent: total compiling related event timespans, timespan > 5 second count as idle
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('TimeSpent', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(TotalTimeSpent)
                         ELSE Sum(TotalTimeSpent)
                       END ),
               Measure='TimeSpent'
        FROM   (SELECT EventHour,
                       UserId,
                       TotalTimeSpent=Sum(Datediff(second, EventTime, NextEventTime))
                FROM   #ProgrammingEvents
                WHERE  NextEventTime IS NOT NULL
                       AND Datediff(second, EventTime, NextEventTime) < 6
                GROUP  BY EventHour,
                          UserId) sub
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Number of Compilations: total compiling related event count
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfCompilations', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(TotalCompilations)
                         ELSE Sum(TotalCompilations)
                       END ),
               Measure='NumberOfCompilations'
        FROM   (SELECT EventHour,
                       UserId,
                       TotalCompilations=Count(*)
                FROM   #ProgrammingEvents
                GROUP  BY EventHour,
                          UserId) sub
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Lines of Code Written: number of modified lines
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('LinesOfCodeWritten', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfModified)
                         ELSE Sum(NumberOfModified)
                       END ),
               MeasureType='LinesOfCodeWritten'
        FROM   (SELECT EventHour,
                       l.UserId,
                       NumberOfModified=Sum(Isnull(b.NumberOfModified, 0))
                FROM   [dbo].[BuildDocuments] b
                       INNER JOIN #EventLogs l
                               ON l.BuildEventId = b.BuildId
                GROUP  BY EventHour,
                          l.UserId) sub
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Number of Errors Per Compilations: total build errors
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfErrorsPerCompilation', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(CompilationErrors)
                         ELSE Sum(CompilationErrors)
                       END ),
               Measure='NumberOfErrorsPerCompilation'
        FROM   (SELECT EventHour,
                       l.UserId,
                       b.LogId,
                       CompilationErrors=Count(b.BuildErrorTypeId)
                FROM   [dbo].[BuildErrors] b
                       INNER JOIN #EventLogs l
                               ON l.EventLogId = b.LogId
                GROUP  BY EventHour,
                          l.UserId,
                          b.LogId) sub
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Avg. number of executions without debug per (active) student
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfNoDebugExecutions', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfExecutions)
                         ELSE Sum(NumberOfExecutions)
                       END ),
               Measure='NumberOfNoDebugExecutions'
        FROM   (SELECT EventHour,
                       l.UserId,
                       NumberOfExecutions=Sum(CASE
                                                WHEN e.ExecutionAction = 5 THEN 1
                                                ELSE 0
                                              END)
                FROM   [dbo].[DebugEvents] e
                       INNER JOIN #EventLogs l
                               ON l.EventLogId = e.EventLogId
                GROUP  BY EventHour,
                          l.UserId) sub
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Avg. number of executions with debug per (active) student
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfDebugExecutions', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfDebuggings)
                         ELSE Sum(NumberOfDebuggings)
                       END ),
               Measure='NumberOfDebugExecutions'
        FROM   (SELECT EventHour,
                       l.UserId,
                       NumberOfDebuggings=Sum(CASE
                                                WHEN e.ExecutionAction = 0 THEN 1
                                                ELSE 0
                                              END)
                FROM   [dbo].[DebugEvents] e
                       INNER JOIN #EventLogs l
                               ON l.EventLogId = e.EventLogId
                GROUP  BY EventHour,
                          l.UserId) sub
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Avg. number of breakpoints set per (active) student
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfBreakpointsSet', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfBreakPoints)
                         ELSE Sum(NumberOfBreakPoints)
                       END ),
               Measure='NumberOfBreakpointsSet'
        FROM   (SELECT EventHour,
                       l.UserId,/*b.BuildEventId,*/
                       NumberOfBreakPoints=Count(b.BreakPointId)
                FROM   [dbo].[BuildEventBreakPoints] b
                       INNER JOIN #EventLogs l
                               ON l.BuildEventId = b.BuildEventId
                GROUP  BY EventHour,
                          l.UserId/*, b.BuildEventId*/
               ) sub
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Avg. number of runtime exceptions obtained per (active) student
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfRuntimeExceptions', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfExceptions)
                         ELSE Sum(NumberOfExceptions)
                       END ),
               Measure='NumberOfRuntimeExceptions'
        FROM   (SELECT EventHour,
                       l.UserId,
                       NumberOfExceptions=Count(l.EventLogId)
                FROM   #EventLogs l
                WHERE  l.EventTypeId = 6
                GROUP  BY EventHour,
                          l.UserId) sub
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Number of new threads started
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfPosts', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfNewThread)
                         ELSE Sum(NumberOfNewThread)
                       END ),
               Measure='NumberOfPosts'
        FROM   (SELECT EventHour,
                       l.UserId,
                       NumberOfNewThread=Count(l.EventLogId)
                FROM   #EventLogs l
                WHERE  EventTypeId IN ( 1, 7 )
                GROUP  BY EventHour,
                          l.UserId) sub
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Number of replies (when averaging, do on per thread basis)
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfReplies', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfReplies)
                         ELSE Max(NumberOfReplies)
                       END ),
               Measure='NumberOfReplies'
        FROM   (SELECT EventHour,
                       l.EventLogId,
                       NumberOfReplies=Count(e.EventLogId)
                FROM   #EventLogs l
                       INNER JOIN [dbo].[LogCommentEvents] e
                               ON e.SourceEventLogId = l.EventLogId
                WHERE  l.EventTypeId IN ( 1, 7 )
                GROUP  BY EventHour,
                          l.EventLogId) sub
        GROUP  BY EventHour

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Time to first reply (when averaging, do on per thread basis)
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('TimeToFirstReply', @measures) > 0
        INSERT INTO #Measures
        SELECT Datepart(hh, EventHour),
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(FirstReplyTime)
                         ELSE Min(FirstReplyTime)
                       END ),
               Measure='TimeToFirstReply'
        FROM   (SELECT EventHour,
                       FirstReplyTime=Min(Datediff(second, e.EventTime, r.EventDate))
                FROM   #EventLogs e
                       INNER JOIN [dbo].[LogCommentEvents] r
                               ON r.SourceEventLogId = e.EventLogId
                WHERE  e.EventTypeId IN ( 1, 7 )
                GROUP  BY EventHour) sub
        GROUP  BY EventHour

      -- proc return
      -------------------------------------------------------------------------------------------------
      SELECT *
      FROM   #Measures
      ORDER  BY Measure,
                EventHour
  END 
