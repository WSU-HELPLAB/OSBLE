-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE DebugEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical DebugEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='DebugEvents')
BEGIN

	CREATE TABLE DebugEvents
	  (
		 Id					INT IDENTITY NOT NULL,
		 EventLogId			INT NOT NULL,
		 EventDate			DATETIME NOT NULL,
		 SolutionName		VARCHAR(MAX) NOT NULL,
		 ExecutionAction	INT NOT NULL,
		 DocumentName		VARCHAR(MAX) NOT NULL,
		 LineNumber			INT NOT NULL,
		 DebugOutput		VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_DebugEvents PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_DebugEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#DebugEvents') IS NOT NULL
    DROP TABLE #DebugEvents

SELECT	a.Id,
		a.EventLogId,
		a.EventDate,
		a.SolutionName,
		a.ExecutionAction,
		a.DocumentName,
		a.LineNumber,
		a.DebugOutput
INTO #DebugEvents
FROM [OSBIDE.Helplab].dbo.DebugEvents a
INNER JOIN dbo.EventLogs b ON b.Id = a.EventLogId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[DebugEvents] ON
MERGE [dbo].[DebugEvents] AS Target
USING [#DebugEvents] AS Source ON (Target.Id = Source.Id)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id, EventLogId, EventDate, SolutionName, ExecutionAction, DocumentName, LineNumber, DebugOutput)
	VALUES
	(
		Source.Id,
		Source.EventLogId,
		Source.EventDate,
		Source.SolutionName,
		Source.ExecutionAction,
		Source.DocumentName,
		Source.LineNumber,
		Source.DebugOutput
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[DebugEvents] OFF
