CREATE PROCEDURE [dbo].[GetActivityFeedById]
				 @logId INT
AS
  BEGIN
      SET NOCOUNT ON;

      -------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------
      -- Subject Eventlog
      -------------------------------------------------------------------------------------
      -------------------------------------------------------------------------------------
	  IF Object_id('tempdb..#events') IS NOT NULL
	  DROP TABLE #events

      CREATE TABLE #events
        (
           EventLogId     INT NOT NULL,
           EventTypeId    INT NOT NULL,
           EventDate      DATETIME NOT NULL,
           SenderId       INT NOT NULL,
		   CourseId		  INT,
           IsPrimaryEvent BIT NOT NULL,
        )

      INSERT INTO #events
	  SELECT EventLogId=Id, EventTypeId, EventDate, SenderId, CourseId, IsPrimaryEvent=CAST(1 AS BIT)
	  FROM EventLogs
	  WHERE Id=@logId

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
             INNER JOIN [dbo].[HelpfulMarkGivenEvents] hm WITH (NOLOCK)
                     ON hm.LogCommentEventId = cs.Id
             INNER JOIN [dbo].[EventLogs] s WITH (NOLOCK)
                     ON s.Id = hm.EventLogId
					 AND (s.[IsDeleted] IS NULL OR s.[IsDeleted] = 0)

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
      FROM   [dbo].[UserProfiles] a WITH (NOLOCK)
             INNER JOIN #events b
                     ON b.SenderId = a.ID

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
exec [dbo].[GetActivityFeedById] @logId=14909

*/
