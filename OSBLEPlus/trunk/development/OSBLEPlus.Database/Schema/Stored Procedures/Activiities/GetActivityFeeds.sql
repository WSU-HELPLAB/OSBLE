-------------------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------------------
-- sproc [GetActivityFeeds]
-------------------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[GetActivityFeeds] @DateReceivedMin DATETIME='01-01-2010',
                                          @DateReceivedMax DATETIME,
                                          @EventLogIds     NVARCHAR(max)='',
                                          @EventTypes      NVARCHAR(max)='',
                                          @CourseId        INT=-1,
                                          @RoleId          INT=99,
                                          @CommentFilter   NVARCHAR(max)='',
                                          @SenderIds       NVARCHAR(max)='',
                                          @TopN            INT=20
AS
  BEGIN
      SET NOCOUNT ON;

      -------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------
      -- Subject Eventlogs
      -------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------
      IF Object_id('tempdb..#eventTypesFilter') IS NOT NULL
        DROP TABLE #eventTypesFilter

      IF Object_id('tempdb..#senderIdFilter') IS NOT NULL
        DROP TABLE #senderIdFilter

      IF Object_id('tempdb..#eventLogIdFilter') IS NOT NULL
        DROP TABLE #eventLogIdFilter

      CREATE TABLE #eventTypesFilter
        (
           EventTypeId INT
        )

      INSERT INTO #eventTypesFilter
      SELECT a.EventTypeId
      FROM   [dbo].[EventTypes] a
             INNER JOIN [dbo].[Split](@EventTypes, ',') b
                     ON Cast(b.Items AS INT) = a.EventTypeId
      WHERE  a.IsFeedEvent = 1

      IF NOT EXISTS(SELECT 1
                    FROM   #eventTypesFilter)
        INSERT INTO #eventTypesFilter
        SELECT EventTypeId
        FROM   [dbo].[EventTypes]
        WHERE  IsFeedEvent = 1

      CREATE CLUSTERED INDEX IX_Temp_EventTypes
        ON #eventTypesFilter(EventTypeId)

      CREATE TABLE #senderIdFilter
        (
           Id INT
        )

      INSERT INTO #senderIdFilter
      SELECT Id=Cast(Items AS INT)
      FROM   [dbo].[Split](@SenderIds, ',')

      CREATE CLUSTERED INDEX IX_Temp_SenderFilter
        ON #senderIdFilter(Id)

      CREATE TABLE #eventLogIdFilter
        (
           Id INT
        )

      INSERT INTO #eventLogIdFilter
      SELECT Id=Cast(Items AS INT)
      FROM   [dbo].[Split](@EventLogIds, ',')

      CREATE CLUSTERED INDEX IX_Temp_LogFilter
        ON #eventLogIdFilter(Id)

      CREATE TABLE #events
        (
           EventLogId     INT NOT NULL,
           EventTypeId    INT NOT NULL,
           EventDate      DATETIME NOT NULL,
           SenderId       INT NOT NULL,
           IsPrimaryEvent BIT NOT NULL
        )

	  DECLARE @anyCourse INT = -1

      IF Len(@CommentFilter) > 0
        BEGIN
            INSERT INTO #events
            SELECT TOP(@TopN) EventLogId = s.Id,
                              s.EventTypeId,
                              s.EventDate,
                              s.SenderId,
                              1
            FROM   [dbo].[EventLogs] s WITH (NOLOCK)
                   INNER JOIN #eventTypesFilter ef
                           ON ef.EventTypeId = s.EventTypeId
                   INNER JOIN [dbo].[UserProfiles] u WITH (NOLOCK)
                           ON u.ID = s.SenderId
                   LEFT JOIN [dbo].[CourseUsers] cr1 WITH (NOLOCK)
                          ON cr1.UserProfileID = s.SenderId
                             AND cr1.AbstractCourseID = @CourseId
                             AND cr1.AbstractRoleID = @RoleId
                   LEFT JOIN [dbo].[CourseUsers] cr2 WITH (NOLOCK)
                          ON cr2.UserProfileID = s.SenderId
                             AND @CourseId = @anyCourse
                             AND cr2.AbstractRoleID = @RoleId
                   LEFT JOIN [dbo].[CourseUsers] cr3 WITH (NOLOCK)
                          ON cr3.UserProfileID = s.SenderId
                             AND cr3.AbstractCourseID = @CourseId
                             AND @RoleId = 99
                   LEFT JOIN [dbo].[CourseUsers] cr WITH (NOLOCK)
                          ON cr.UserProfileID = s.SenderId
                             AND @CourseId = @anyCourse
                             AND @RoleId = 99
                   LEFT JOIN (SELECT buildErrors=Count(BuildErrorTypeId),
                                     LogId
                              FROM   [dbo].[BuildErrors] WITH (NOLOCK)
                              GROUP  BY LogId) be
                          ON s.Id = be.LogId
                             AND ef.EventTypeId = 2
                             AND be.buildErrors > 0
                   LEFT JOIN #senderIdFilter sf1
                          ON sf1.Id = s.SenderId
                   LEFT JOIN #senderIdFilter sf2
                          ON Len(@SenderIds) = 0
                   LEFT JOIN #eventLogIdFilter eif1
                          ON eif1.Id = s.Id
                   LEFT JOIN #eventLogIdFilter eif2
                          ON Len(@EventLogIds) = 0
                   LEFT JOIN [dbo].[FeedPostEvents] fp
                          ON fp.EventLogId = s.Id
                             AND fp.Comment LIKE @CommentFilter
                             AND ef.EventTypeId = 7
                             AND fp.Id > 0
            WHERE  s.CreatedDate BETWEEN @DateReceivedMin AND @DateReceivedMax
            ORDER  BY s.CreatedDate DESC
        END
      ELSE
        BEGIN
            INSERT INTO #events
            SELECT TOP(@TopN) EventLogId = s.Id,
                              s.EventTypeId,
                              s.EventDate,
                              s.SenderId,
                              1
            FROM   [dbo].[EventLogs] s WITH (NOLOCK)
                   INNER JOIN #eventTypesFilter ef
                           ON ef.EventTypeId = s.EventTypeId
                   INNER JOIN [dbo].[UserProfiles] u WITH (NOLOCK)
                           ON u.ID = s.SenderId
                   LEFT JOIN [dbo].[CourseUsers] cr1 WITH (NOLOCK)
                          ON cr1.UserProfileID = s.SenderId
                             AND cr1.AbstractCourseID = @CourseId
                             AND cr1.AbstractRoleID = @RoleId
                   LEFT JOIN [dbo].[CourseUsers] cr2 WITH (NOLOCK)
                          ON cr2.UserProfileID = s.SenderId
                             AND @CourseId = 0
                             AND cr2.AbstractRoleID = @RoleId
                   LEFT JOIN [dbo].[CourseUsers] cr3 WITH (NOLOCK)
                          ON cr3.UserProfileID = s.SenderId
                             AND cr3.AbstractCourseID = @CourseId
                             AND @RoleId = 99
                   LEFT JOIN [dbo].[CourseUsers] cr WITH (NOLOCK)
                          ON cr.UserProfileID = s.SenderId
                             AND @CourseId = 0
                             AND @RoleId = 99
                   LEFT JOIN (SELECT buildErrors=Count(BuildErrorTypeId),
                                     LogId
                              FROM   [dbo].[BuildErrors] WITH (NOLOCK)
                              GROUP  BY LogId) be
                          ON s.Id = be.LogId
                             AND ef.EventTypeId = 2
                             AND be.buildErrors > 0
                   LEFT JOIN #senderIdFilter sf1
                          ON sf1.Id = s.SenderId
                   LEFT JOIN #senderIdFilter sf2
                          ON Len(@SenderIds) = 0
                   LEFT JOIN #eventLogIdFilter eif1
                          ON eif1.Id = s.Id
                   LEFT JOIN #eventLogIdFilter eif2
                          ON Len(@EventLogIds) = 0
            WHERE  s.CreatedDate BETWEEN @DateReceivedMin AND @DateReceivedMax
            ORDER  BY s.CreatedDate DESC
        END

      -- event logs' comments
      INSERT INTO #events
      SELECT s.Id,
             s.EventTypeId,
             s.EventDate,
             s.SenderId,
             0
      FROM   #events e
             INNER JOIN [dbo].[LogCommentEvents] cs WITH (NOLOCK)
                     ON cs.SourceEventLogId = e.EventLogId
                        AND ( Len(@CommentFilter) = 0
                               OR cs.Content LIKE @CommentFilter )
             INNER JOIN [dbo].[EventLogs] s WITH (NOLOCK)
                     ON s.Id = cs.EventLogId

      -- event log comments' helpful marks
      INSERT INTO #events
      SELECT EventLogId = s.Id,
             s.EventTypeId,
             s.EventDate,
             s.SenderId,
             0
      FROM   #events e
             INNER JOIN [dbo].[LogCommentEvents] cs WITH (NOLOCK)
                     ON cs.SourceEventLogId = e.EventLogId
                        AND ( Len(@CommentFilter) = 0
                               OR cs.Content LIKE @CommentFilter )
             INNER JOIN [dbo].[HelpfulMarkGivenEvents] hm WITH (NOLOCK)
                     ON hm.LogCommentEventId = cs.Id
             INNER JOIN [dbo].[EventLogs] s WITH (NOLOCK)
                     ON s.Id = hm.EventLogId

      CREATE CLUSTERED INDEX IX_Temp_Events
        ON #events(EventLogId)

      -------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------
      -- Top level results
      -------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------
      -- EventLogs 
      SELECT EventLogId,
             EventTypeId,
             EventDate,
             SenderId,
             IsPrimaryEvent
      FROM   #events

      -- Event and Comment Users 
      SELECT DISTINCT UserId = a.ID,
                      Email=a.UserName,
                      a.FirstName,
                      a.LastName,
                      a.SchoolID,
                      a.Identification,
                      a.EmailAllActivityPosts,
                      a.EmailAllNotifications,
                      a.EmailNewDiscussionPosts,
                      DefaultCourseId = a.DefaultCourse,
                      DefaultCourseNumber=c.Number,
                      DefaultCourseNamePrefix=c.Prefix
      FROM   [dbo].[UserProfiles] a WITH (NOLOCK)
             INNER JOIN [dbo].[AbstractCourses] c WITH (NOLOCK)
                     ON c.ID = a.DefaultCourse
             INNER JOIN #events b
                     ON b.SenderId = a.ID

      -- UserSubscriptions
  /* need more requirement clarifications
     SELECT a.UserId,
            a.LogId
     FROM   [dbo].[EventLogSubscriptions] a WITH (NOLOCK)
            INNER JOIN #events b
                    ON b.EventLogId = a.LogId
  */
      -------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------
      -- Detailed event types
      -------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------
      SELECT EventId = a.Id,
             a.EventLogId,
             a.EventDate,
             a.Code,
             a.SolutionName,
             a.UserComment
      FROM   [dbo].[AskForHelpEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId

      SELECT EventId = a.Id,
             a.EventLogId,
             a.EventDate,
             a.SolutionName
      FROM   [dbo].[BuildEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId

      SELECT EventId = a.Id,
             a.EventLogId,
             a.EventDate,
             a.DocumentName,
             a.ExceptionAction,
             a.ExceptionCode,
             a.ExceptionDescription,
             a.ExceptionName,
             a.ExceptionType,
             a.LineContent,
             a.LineNumber,
             a.SolutionName
      FROM   [dbo].[ExceptionEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId

      SELECT EventId = a.Id,
             a.EventLogId,
             a.EventDate,
             a.Comment,
             a.SolutionName
      FROM   [dbo].[FeedPostEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId
      WHERE  a.Comment LIKE @CommentFilter
              OR Len(@CommentFilter) = 0

      SELECT EventId = a.Id,
             a.EventLogId,
             a.SourceEventLogId,
             a.SolutionName,
             a.Content
      FROM   [dbo].[LogCommentEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.SourceEventLogId

      SELECT EventId = a.Id,
             a.EventLogId,
             a.LogCommentEventId,
             a.SolutionName
      FROM   [dbo].[HelpfulMarkGivenEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId

      SELECT EventId = a.Id,
             a.EventLogId,
             a.AssignmentId,
             a.SolutionName
      FROM   [dbo].[SubmitEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId
  END
/*

DBCC FREEPROCCACHE
exec [dbo].[GetActivityFeeds] @DateReceivedMax='9-6-2014'

*/
