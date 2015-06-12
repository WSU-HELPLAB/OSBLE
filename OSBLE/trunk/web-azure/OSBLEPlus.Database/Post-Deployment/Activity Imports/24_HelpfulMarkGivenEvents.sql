-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE HelpfulMarkGivenEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical HelpfulMarkGivenEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='HelpfulMarkGivenEvents')
BEGIN

	CREATE TABLE HelpfulMarkGivenEvents
	  (
		 Id					INT IDENTITY NOT NULL,
		 EventLogId			INT NOT NULL,
		 LogCommentEventId	INT NOT NULL,
		 EventDate			DATETIME NOT NULL,
		 SolutionName		VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_HelpfulMarkGivenEvents PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_HelpfulMarkGivenEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
		 CONSTRAINT FK_HelpfulMarkGivenSourceEvents_LogCommentEvents FOREIGN KEY (LogCommentEventId) REFERENCES LogCommentEvents(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#HelpfulMarkGivenEvents') IS NOT NULL
    DROP TABLE #HelpfulMarkGivenEvents

SELECT	a.Id,
		a.EventLogId,
		a.LogCommentEventId,
		a.EventDate,
		a.SolutionName
INTO #HelpfulMarkGivenEvents
FROM [OSBIDE.Helplab].dbo.HelpfulMarkGivenEvents a
INNER JOIN dbo.EventLogs b ON b.Id = a.EventLogId
INNER JOIN dbo.LogCommentEvents c ON c.Id = a.LogCommentEventId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[HelpfulMarkGivenEvents] ON
MERGE [dbo].[HelpfulMarkGivenEvents] AS Target
USING [#HelpfulMarkGivenEvents] AS Source ON (Target.Id = Source.Id)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id, EventLogId, LogCommentEventId, EventDate, SolutionName)
	VALUES
	(
		Source.Id,
		Source.EventLogId,
		Source.LogCommentEventId,
		Source.EventDate,
		Source.SolutionName
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[HelpfulMarkGivenEvents] OFF

