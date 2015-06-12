-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE EditorActivityEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical EditorActivityEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='EditorActivityEvents')
BEGIN

	CREATE TABLE EditorActivityEvents
	  (
		 Id					INT IDENTITY NOT NULL,
		 EventLogId			INT NOT NULL,
		 EventDate			DATETIME NOT NULL,
		 SolutionName		VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_EditorActivityEvents PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_EditorActivityEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#EditorActivityEvents') IS NOT NULL
    DROP TABLE #EditorActivityEvents

SELECT	a.Id,
		a.EventLogId,
		a.EventDate,
		a.SolutionName
INTO #EditorActivityEvents
FROM [OSBIDE.Helplab].dbo.EditorActivityEvents a
INNER JOIN dbo.EventLogs b ON b.Id = a.EventLogId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[EditorActivityEvents] ON
MERGE [dbo].[EditorActivityEvents] AS Target
USING [#EditorActivityEvents] AS Source ON (Target.Id = Source.Id)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id, EventLogId, EventDate, SolutionName)
	VALUES
	(
		Source.Id,
		Source.EventLogId,
		Source.EventDate,
		Source.SolutionName
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[EditorActivityEvents] OFF
