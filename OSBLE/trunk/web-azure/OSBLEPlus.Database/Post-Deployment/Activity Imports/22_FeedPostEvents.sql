-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE FeedPostEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical FeedPostEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='FeedPostEvents')
BEGIN

	CREATE TABLE FeedPostEvents
	  (
		 Id				INT IDENTITY NOT NULL,
		 EventLogId		INT NOT NULL,
		 EventDate		DATETIME NOT NULL,
		 SolutionName	VARCHAR(MAX) NOT NULL,
		 Comment		VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_FeedPostEvents PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_FeedPostEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#FeedPostEvents') IS NOT NULL
    DROP TABLE #FeedPostEvents

SELECT	a.Id,
		a.EventLogId,
		a.EventDate,
		a.SolutionName,
		a.Comment
INTO #FeedPostEvents
FROM [OSBIDE.Helplab].dbo.FeedPostEvents a
INNER JOIN dbo.EventLogs b ON b.Id = a.EventLogId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[FeedPostEvents] ON
MERGE [dbo].[FeedPostEvents] AS Target
USING [#FeedPostEvents] AS Source ON (Target.Id = Source.Id)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id, EventLogId, EventDate, SolutionName, Comment)
	VALUES
	(
		Source.Id,
		Source.EventLogId,
		Source.EventDate,
		Source.SolutionName,
		Source.Comment
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[FeedPostEvents] OFF



