-------------------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------------------
-- sproc [GetActivityFeeds]
-------------------------------------------------------------------------------------------------
-------------------------------------------------------------------------------------------------
CREATE PROCEDURE [dbo].[GetActivityFeeds] @DateReceivedMin DATETIME='01-01-2010',
                                          @DateReceivedMax DATETIME,
										  @MinEventLogId   INT=-1,
										  @MaxEventLogId   INT=2147483647,
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
	  IF Object_id('tempdb..#events') IS NOT NULL
	  DROP TABLE #events

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
		   CourseId		  INT,
           IsPrimaryEvent BIT NOT NULL,
        )

	  DECLARE @anyCourse INT = -1

      IF Len(@CommentFilter) > 0
        BEGIN
			IF Len(@SenderIds) > 0 AND Len(@EventLogIds) > 0
				BEGIN

				INSERT INTO #events
				EXEC [dbo].[GetActivityFeedsWithCommentSenderLogFilters] @DateReceivedMin,
                                                                     @DateReceivedMax,
                                                                     @MinEventLogId,
                                                                     @MaxEventLogId,
                                                                     @CourseId,
                                                                     @RoleId,
                                                                     @CommentFilter,
                                                                     @TopN,
                                                                     @anyCourse
				END
			ELSE
			IF Len(@SenderIds) > 0
				BEGIN

				INSERT INTO #events
				EXEC [dbo].[GetActivityFeedsWithCommentSenderFilters] @DateReceivedMin,
                                                                     @DateReceivedMax,
                                                                     @MinEventLogId,
                                                                     @MaxEventLogId,
                                                                     @CourseId,
                                                                     @RoleId,
                                                                     @CommentFilter,
                                                                     @TopN,
                                                                     @anyCourse
				END
			ELSE
			IF Len(@EventLogIds) > 0
				BEGIN

				INSERT INTO #events
				EXEC [dbo].[GetActivityFeedsWithCommentLogFilters] @DateReceivedMin,
                                                                     @DateReceivedMax,
                                                                     @MinEventLogId,
                                                                     @MaxEventLogId,
                                                                     @CourseId,
                                                                     @RoleId,
                                                                     @CommentFilter,
                                                                     @TopN,
                                                                     @anyCourse
				END
			ELSE
				BEGIN

				INSERT INTO #events
				EXEC [dbo].[GetActivityFeedsWithCommentFilters] @DateReceivedMin,
                                                                @DateReceivedMax,
                                                                @MinEventLogId,
                                                                @MaxEventLogId,
                                                                @CourseId,
                                                                @RoleId,
                                                                @CommentFilter,
                                                                @TopN,
                                                                @anyCourse
				END
		END
	  ELSE
		BEGIN
			IF Len(@SenderIds) > 0 AND Len(@EventLogIds) > 0
				BEGIN

				INSERT INTO #events
				EXEC [dbo].[GetActivityFeedsWithSenderLogFilters] @DateReceivedMin,
                                                                     @DateReceivedMax,
                                                                     @MinEventLogId,
                                                                     @MaxEventLogId,
                                                                     @CourseId,
                                                                     @RoleId,
                                                                     @TopN,
                                                                     @anyCourse
				END
			ELSE
			IF Len(@SenderIds) > 0
				BEGIN

				INSERT INTO #events
				EXEC [dbo].[GetActivityFeedsWithSenderFilters] @DateReceivedMin,
                                                                @DateReceivedMax,
                                                                @MinEventLogId,
                                                                @MaxEventLogId,
                                                                @CourseId,
                                                                @RoleId,
                                                                @TopN,
                                                                @anyCourse
				END
			ELSE
			IF Len(@EventLogIds) > 0
				BEGIN

				INSERT INTO #events
				EXEC [dbo].[GetActivityFeedsWithLogFilters] @DateReceivedMin,
                                                            @DateReceivedMax,
                                                            @MinEventLogId,
                                                            @MaxEventLogId,
                                                            @CourseId,
                                                            @RoleId,
                                                            @TopN,
                                                            @anyCourse
				END
			ELSE
				BEGIN

				INSERT INTO #events
				EXEC [dbo].[GetActivityFeedsBasic] @DateReceivedMin,
                                                    @DateReceivedMax,
                                                    @MinEventLogId,
                                                    @MaxEventLogId,
                                                    @CourseId,
                                                    @RoleId,
                                                    @TopN,
                                                    @anyCourse
				END
		END

      -- event logs' comments
      INSERT INTO #events
      SELECT s.Id,
             s.EventTypeId,
             s.EventDate,
             s.SenderId,
			 s.CourseId,
             0
      FROM   #events e
             INNER JOIN [dbo].[LogCommentEvents] cs WITH (NOLOCK)
                     ON cs.SourceEventLogId = e.EventLogId
                        AND ( Len(@CommentFilter) = 0
                               OR cs.Content LIKE @CommentFilter )
						
             INNER JOIN [dbo].[EventLogs] s WITH (NOLOCK)
                     ON s.Id = cs.EventLogId
					 AND (s.[IsDeleted] IS NULL OR s.[IsDeleted] = 0)

      -- event log comments' helpful marks
      INSERT INTO #events
      SELECT EventLogId = s.Id,
             s.EventTypeId,
             s.EventDate,
             s.SenderId,
			 s.CourseId,
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
					 AND (s.[IsDeleted] IS NULL OR s.[IsDeleted] = 0)
	  WHERE EXISTS 
	         (SELECT * FROM #eventTypesFilter WHERE EventTypeId = 8)
			 -- eventTypesFilter = 8 for HelpfulMarkGivenEvents
			 

      CREATE CLUSTERED INDEX IX_Temp_Events
        ON #events(EventLogId)

      -------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------
      -- Top level results
	  -- number next to header to table is the number read from Dapper QueryMultiple
      -------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------
      -- EventLogs (1)
      SELECT EventLogId,
             EventTypeId,
             EventDate,
             SenderId,
			 CourseId,
             IsPrimaryEvent
      FROM   #events

      -- Event and Comment Users (2)
      SELECT DISTINCT IUserId = a.ID,
                      Email=a.UserName,
                      a.FirstName,
                      a.LastName,
                      a.SchoolID,
                      a.Identification,
                      a.EmailAllActivityPosts,
                      a.EmailAllNotifications,
                      a.EmailNewDiscussionPosts
                      --a.DefaultCourseId, = a.DefaultCourse,
                      --a.DefaultCourseNumber,=c.Number,
                      --a.DefaultCourseNamePrefix=,c.Prefix
      FROM   [dbo].[UserProfiles] a WITH (NOLOCK)
             --INNER JOIN [dbo].[AbstractCourses] c WITH (NOLOCK)
             --        ON c.ID = a.DefaultCourse
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
	  -- Ask For Help (3)
      SELECT EventId = a.Id,
             a.EventLogId,
             a.EventDate,
             a.Code,
             a.SolutionName,
             a.UserComment
      FROM   [dbo].[AskForHelpEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId
      WHERE  a.UserComment LIKE @CommentFilter
              OR Len(@CommentFilter) = 0

	  -- Build (4)
      SELECT EventId = a.Id,
             a.EventLogId,
             a.EventDate,
             a.SolutionName
      FROM   [dbo].[BuildEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId

	  -- Cut Copy Paste (5)
      SELECT EventId = a.Id,
             a.EventLogId,
			 a.DocumentName,
			 a.EventAction,
			 a.Content,
             a.EventDate,
             a.SolutionName
      FROM   [dbo].[CutCopyPasteEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId

	  -- Debug (6)
      SELECT EventId = a.Id,
             a.EventLogId,
			 a.DocumentName,
			 a.ExecutionAction,
			 a.DebugOutput,
			 a.LineNumber,
             a.EventDate,
             a.SolutionName
      FROM   [dbo].[DebugEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId

      -- Editor Activity (7)
	  SELECT EventId = a.Id,
			 a.EventLogId,
			 a.EventDate,
			 a.SolutionName
	  FROM   [dbo].[EditorActivityEvents] a WITH (NOLOCK)
		     INNER JOIN #events b
			         ON b.EventLogId = a.EventLogId

	  -- Exception (8)
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

	  -- Feed Posts (9)
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

	  -- Helpful Marks (10)
	  SELECT EventId = a.Id,
             a.EventLogId,
			 a.EventDate,
             a.LogCommentEventId,
             a.SolutionName
      FROM   [dbo].[HelpfulMarkGivenEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId

	  -- Log Comments (11)
      SELECT EventId = a.Id,
             a.EventLogId,
			 a.EventDate,
             a.SourceEventLogId,
             a.SolutionName,
             a.Content
      FROM   [dbo].[LogCommentEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.SourceEventLogId

	  -- Save (12)
      SELECT EventId = a.Id,
             a.EventLogId,
			 a.DocumentId,
			 a.EventDate,
			 a.SolutionName
      FROM   [dbo].[SaveEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId

	  -- Submit (13)
      SELECT EventId = a.Id,
             a.EventLogId,
             a.AssignmentId,
             a.SolutionName,
			 a.EventDate
      FROM   [dbo].[SubmitEvents] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.EventLogId = a.EventLogId

  END
/*

DBCC FREEPROCCACHE
exec [dbo].[GetActivityFeeds] @DateReceivedMax='9-6-2014'

*/
