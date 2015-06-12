-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE LogCommentEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical LogCommentEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='LogCommentEvents')
BEGIN

	CREATE TABLE LogCommentEvents
	  (
		 Id					INT IDENTITY NOT NULL,
		 EventLogId			INT NOT NULL,
		 SourceEventLogId	INT NOT NULL,
		 EventDate			DATETIME NOT NULL,
		 SolutionName		VARCHAR(MAX) NOT NULL,
		 Content			VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_LogCommentEvents PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_LogCommentEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
		 CONSTRAINT FK_LogCommentSourceEvents_EventLogs FOREIGN KEY (SourceEventLogId) REFERENCES EventLogs(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#LogCommentEvents') IS NOT NULL
    DROP TABLE #LogCommentEvents

SELECT	a.Id,
		a.EventLogId,
		a.SourceEventLogId,
		a.EventDate,
		a.SolutionName,
		a.Content
INTO #LogCommentEvents
FROM [OSBIDE.Helplab].dbo.LogCommentEvents a
INNER JOIN dbo.EventLogs b ON b.Id = a.EventLogId
INNER JOIN dbo.EventLogs c ON c.Id = a.SourceEventLogId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[LogCommentEvents] ON
MERGE [dbo].[LogCommentEvents] AS Target
USING [#LogCommentEvents] AS Source ON (Target.Id = Source.Id)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id, EventLogId, SourceEventLogId, EventDate, SolutionName, Content)
	VALUES
	(
		Source.Id,
		Source.EventLogId,
		Source.SourceEventLogId,
		Source.EventDate,
		Source.SolutionName,
		Source.Content
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[LogCommentEvents] OFF

