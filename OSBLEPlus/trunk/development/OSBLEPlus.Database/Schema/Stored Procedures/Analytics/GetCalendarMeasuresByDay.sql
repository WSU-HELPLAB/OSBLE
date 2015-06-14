---------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------
-- sproc [dbo].[GetCalendarMeasuresByDay]
---------------------------------------------------------------------------------------------------------------
---------------------------------------------------------------------------------------------------------------
/*

 dbcc freeproccache with no_infomsgs;
 
 exec [dbo].[GetCalendarMeasuresByDay] @measures='ActiveStudents,LinesOfCodeWritten,TimeSpent,NumberOfCompilations,NumberOfErrorsPerCompilation,NumberOfNoDebugExecutions,NumberOfDebugExecutions,NumberOfBreakpointsSet,NumberOfRuntimeExceptions,NumberOfPosts,NumberOfReplies,TimeToFirstReply'
 ,@startDate='2014-01-01'
 ,@endDate='2014-03-01'
 ,@students=''
 ,@courseId=1
 ,@isAvg=0


*/
CREATE PROCEDURE [dbo].[Getcalendarmeasuresbyday] @startDate DATE='02-01-2014',
                                                  @endDate   DATE='04-01-2014',
                                                  @students  VARCHAR(max)='',
                                                  @courseId  INT=-1,
                                                  @measures  VARCHAR(2000)='',
                                                  @isAvg     BIT=0
AS
  BEGIN
      --declare @startDate datetime='01-01-2014'
      --declare @endDate datetime='03-01-2014'
      --declare @students varchar(max)=''
      --declare @isAvg bit=1
      SET NOCOUNT ON;

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Base events
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
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

      CREATE CLUSTERED INDEX IX_Temp_Users_Day
        ON #UserIdsTable(UserId)

      IF Object_id('tempdb..#EventLogs') IS NOT NULL
        DROP TABLE #EventLogs

      CREATE TABLE #EventLogs
        (
           EventLogId   INT,
           EventTypeId  INT,
           EventDate    DATETIME,
           UserId       INT,
           BuildEventId INT,
           EventDay     DATE
        )

      INSERT INTO #EventLogs
      SELECT EventLogId=l.Id,
             l.EventTypeId,
             l.EventDate,
             UserId=l.SenderId,
             BuildEventId=b.Id,
             EventDay=CONVERT(DATE, l.EventDate)
      FROM   [dbo].[EventLogs] l
             INNER JOIN #UserIdsTable u
                     ON u.UserId = l.SenderId
             LEFT JOIN [dbo].[BuildEvents] b
                    ON b.EventLogId = l.Id
      WHERE  EventTypeId IN ( 1, 2, 4, 6,
                              7, 9, 10 )
             AND l.EventDate >= @startDate
             AND l.EventDate < @endDate

      CREATE NONCLUSTERED INDEX IX_Temp_EventLogs_Day
        ON [#EventLogs] ([EventTypeId], [EventDate], [BuildEventId])
        include ([UserId])

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Base programming events with event timespane of users (learn about lag)
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Object_id('tempdb..#ProgrammingEvents') IS NOT NULL
        DROP TABLE #ProgrammingEvents

      CREATE TABLE #ProgrammingEvents
        (
           UserId        INT,
           EventDate     DATETIME,
           NextEventTime DATETIME,
           EventDay      DATE
        )

      INSERT INTO #ProgrammingEvents
      SELECT e.UserId,
             e.EventDate,
             NextEventTime=en.EventDate,
             EventDay
      FROM   (SELECT UserId,
                     EventDate,
                     EventDay,
                     Row_number()
                       OVER(
                         partition BY UserId
                         ORDER BY EventDate DESC) AS eRow
              FROM   #EventLogs e
              WHERE  EventTypeId IN ( 2, 4, 10 )) e
             INNER JOIN (SELECT UserId,
                                EventDate,
                                Row_number()
                                  OVER(
                                    partition BY UserId
                                    ORDER BY EventDate DESC) AS eRow
                         FROM   #EventLogs e
                         WHERE  EventTypeId IN ( 2, 4, 10 )) en
                     ON en.eRow + 1 = e.eRow
                        AND en.UserId = e.UserId

      CREATE NONCLUSTERED INDEX IX_Temp_ProgrammingEvents
        ON [#ProgrammingEvents] ([NextEventTime])
        INCLUDE ([UserId], [EventDate], [EventDay])

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Return measures
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Object_id('tempdb..#Measures') IS NOT NULL
        DROP TABLE #Measures

      CREATE TABLE #Measures
        (
           EventDay DATE,
           Value    INT,
           Measure  VARCHAR(200)
        )

      -- Assignments
      -------------------------------------------------------------------------------------------------
      IF @courseId = -1
        BEGIN
            INSERT INTO #Measures
            SELECT EventDay=CONVERT(DATE, a.ReleaseDate),
                   Value=-1,
                   Measure=c.Prefix + ' ' + c.Number + ' ' + a.AssignmentName
                           + ' Released'
            FROM   [dbo].[Assignments] a
                   INNER JOIN [dbo].[AbstractCourses] c
                           ON c.ID = a.CourseID
                              AND c.IsDeleted = 0
            WHERE  a.ReleaseDate BETWEEN @startDate AND @endDate
        END
      ELSE
        BEGIN
            INSERT INTO #Measures
            SELECT EventDay=CONVERT(DATE, a.ReleaseDate),
                   Value=-1,
                   Measure=c.Prefix + ' ' + c.Number + ' ' + a.AssignmentName
                           + ' Released'
            FROM   [dbo].[Assignments] a
                   INNER JOIN [dbo].[AbstractCourses] c
                           ON c.ID = a.CourseID
                              AND c.ID = @courseId
                              AND c.IsDeleted = 0
            WHERE  a.ReleaseDate BETWEEN @startDate AND @endDate
        END

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Active Students: distinct student counts for a day
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF @isAvg = 0
         AND Charindex('ActiveStudents', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=Count(DISTINCT UserId),
               Measure='ActiveStudents'
        FROM   #ProgrammingEvents
        WHERE  NextEventTime IS NOT NULL
               AND Datediff(second, eventDate, NextEventTime) < 6
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Time Spent: total compiling related event timespans, timespan > 5 second count as idle
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('TimeSpent', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(TotalTimeSpent)
                         ELSE Sum(TotalTimeSpent)
                       END ),
               Measure='TimeSpent'
        FROM   (SELECT EventDay,
                       UserId,
                       TotalTimeSpent=Sum(Datediff(second, EventDate, NextEventTime)) / 60
                FROM   #ProgrammingEvents
                WHERE  NextEventTime IS NOT NULL
                       AND Datediff(second, EventDate, NextEventTime) < 6
                GROUP  BY EventDay,
                          UserId) sub
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Number of Compilations: total compiling related event count
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfCompilations', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(TotalCompilations)
                         ELSE Sum(TotalCompilations)
                       END ),
               Measure='NumberOfCompilations'
        FROM   (SELECT EventDay,
                       UserId,
                       TotalCompilations=Count(*)
                FROM   #ProgrammingEvents
                GROUP  BY EventDay,
                          UserId) sub
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Lines of Code Written: number of modified lines
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('LinesOfCodeWritten', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfModified)
                         ELSE Sum(NumberOfModified)
                       END ),
               MeasureType='LinesOfCodeWritten'
        FROM   (SELECT l.EventDay,
                       l.UserId,
                       NumberOfModified=Sum(Isnull(b.NumberOfModified, 0))
                FROM   [dbo].[BuildDocuments] b
                       INNER JOIN #EventLogs l
                               ON l.BuildEventId = b.BuildId
                GROUP  BY l.EventDay,
                          l.UserId) sub
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Number of Errors Per Compilations: total build errors
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfErrorsPerCompilation', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(CompilationErrors)
                         ELSE Sum(CompilationErrors)
                       END ),
               Measure='NumberOfErrorsPerCompilation'
        FROM   (SELECT l.EventDay,
                       l.UserId,
                       b.LogId,
                       CompilationErrors=Count(b.BuildErrorTypeId)
                FROM   [dbo].[BuildErrors] b
                       INNER JOIN #EventLogs l
                               ON l.EventLogId = b.LogId
                GROUP  BY l.EventDay,
                          l.UserId,
                          b.LogId) sub
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Avg. number of executions without debug per (active) student
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfNoDebugExecutions', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfExecutions)
                         ELSE Sum(NumberOfExecutions)
                       END ),
               Measure='NumberOfNoDebugExecutions'
        FROM   (SELECT l.EventDay,
                       l.UserId,
                       NumberOfExecutions=Sum(CASE
                                                WHEN e.ExecutionAction = 5 THEN 1
                                                ELSE 0
                                              END)
                FROM   [dbo].[DebugEvents] e
                       INNER JOIN #EventLogs l
                               ON l.EventLogId = e.EventLogId
                GROUP  BY l.EventDay,
                          l.UserId) sub
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Avg. number of executions with debug per (active) student
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfDebugExecutions', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfDebuggings)
                         ELSE Sum(NumberOfDebuggings)
                       END ),
               Measure='NumberOfDebugExecutions'
        FROM   (SELECT l.EventDay,
                       l.UserId,
                       NumberOfDebuggings=Sum(CASE
                                                WHEN e.ExecutionAction = 0 THEN 1
                                                ELSE 0
                                              END)
                FROM   [dbo].[DebugEvents] e
                       INNER JOIN #EventLogs l
                               ON l.EventLogId = e.EventLogId
                GROUP  BY l.EventDay,
                          l.UserId) sub
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Avg. number of breakpoints set per (active) student
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfBreakpointsSet', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfBreakPoints)
                         ELSE Sum(NumberOfBreakPoints)
                       END ),
               Measure='NumberOfBreakpointsSet'
        FROM   (SELECT l.EventDay,
                       l.UserId,/*b.BuildEventId,*/
                       NumberOfBreakPoints=Count(b.BreakPointId)
                FROM   [dbo].[BuildEventBreakPoints] b
                       INNER JOIN #EventLogs l
                               ON l.BuildEventId = b.BuildEventId
                GROUP  BY l.EventDay,
                          l.UserId/*, b.BuildEventId*/
               ) sub
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Avg. number of runtime exceptions obtained per (active) student
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfRuntimeExceptions', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfExceptions)
                         ELSE Sum(NumberOfExceptions)
                       END ),
               Measure='NumberOfRuntimeExceptions'
        FROM   (SELECT l.EventDay,
                       l.UserId,
                       NumberOfExceptions=Count(l.EventLogId)
                FROM   #EventLogs l
                WHERE  l.EventTypeId = 6
                GROUP  BY l.EventDay,
                          l.UserId) sub
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Number of new threads started
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfPosts', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfNewThread)
                         ELSE Sum(NumberOfNewThread)
                       END ),
               Measure='NumberOfPosts'
        FROM   (SELECT l.EventDay,
                       l.UserId,
                       NumberOfNewThread=Count(l.EventLogId)
                FROM   #EventLogs l
                WHERE  EventTypeId IN ( 1, 7 )
                GROUP  BY l.EventDay,
                          l.UserId) sub
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Number of replies (when averaging, do on per thread basis)
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('NumberOfReplies', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(NumberOfReplies)
                         ELSE Max(NumberOfReplies)
                       END ),
               Measure='NumberOfReplies'
        FROM   (SELECT EventDay=CONVERT(DATE, e.EventDate),
                       l.EventLogId,
                       NumberOfReplies=Count(e.EventLogId)
                FROM   #EventLogs l
                       INNER JOIN [dbo].[LogCommentEvents] e
                               ON e.SourceEventLogId = l.EventLogId
                                  AND e.EventDate >= @startDate
                                  AND e.EventDate < @endDate
                WHERE  l.EventTypeId IN ( 1, 7 )
                GROUP  BY CONVERT(DATE, e.EventDate),
                          l.EventLogId) sub
        GROUP  BY EventDay

      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      -- Time to first reply (when averaging, do on per thread basis)
      -------------------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------------------
      IF Charindex('TimeToFirstReply', @measures) > 0
        INSERT INTO #Measures
        SELECT EventDay,
               Value=( CASE
                         WHEN @isAvg = 1 THEN Avg(FirstReplyTime)
                         ELSE Min(FirstReplyTime)
                       END ),
               Measure='TimeToFirstReply'
        FROM   (SELECT e.EventDay,
                       FirstReplyTime=Min(Datediff(second, e.EventDate, r.EventDate)) / 60
                FROM   #EventLogs e
                       INNER JOIN [dbo].[LogCommentEvents] r
                               ON r.SourceEventLogId = e.EventLogId
                                  AND r.EventDate >= @startDate
                                  AND r.EventDate < @endDate
                WHERE  e.EventTypeId IN ( 1, 7 )
                GROUP  BY e.EventDay) sub
        GROUP  BY EventDay

      -- proc return
      -------------------------------------------------------------------------------------------------
      SELECT *
      FROM   #Measures
      ORDER  BY Measure,
                EventDay
  END 
