-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE BuildEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical BuildEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='BuildEvents')
BEGIN

	CREATE TABLE BuildEvents
	  (
		 Id				INT IDENTITY NOT NULL,
		 EventLogId		INT NOT NULL,
		 EventDate		DATETIME NOT NULL,
		 SolutionName	VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_BuildEvents_Id PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_BuildEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#BuildEvents') IS NOT NULL
    DROP TABLE #BuildEvents

SELECT a.[Id],a.[EventLogId],a.[EventDate],a.[SolutionName]
INTO #BuildEvents
FROM [OSBIDE.Helplab].dbo.BuildEvents a
INNER JOIN dbo.EventLogs b on b.Id = a.EventLogId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[BuildEvents] ON
MERGE [dbo].[BuildEvents] AS Target
USING [#BuildEvents] AS Source ON (Target.[Id] = Source.[Id])
	WHEN NOT MATCHED BY Target
	THEN
	INSERT ([Id],[EventLogId],[EventDate],[SolutionName])
	VALUES
	(
		Source.[Id],
		Source.[EventLogId],
		Source.[EventDate],
		Source.[SolutionName]
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[BuildEvents] OFF

