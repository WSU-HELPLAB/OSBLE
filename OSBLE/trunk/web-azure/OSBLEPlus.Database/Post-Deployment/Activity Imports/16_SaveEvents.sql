-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE SaveEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical SaveEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='SaveEvents')
BEGIN

	CREATE TABLE SaveEvents
	  (
		 Id				INT IDENTITY NOT NULL,
		 EventLogId		INT NOT NULL,
		 EventDate		DATETIME NOT NULL,
		 SolutionName	VARCHAR(MAX) NOT NULL,
		 DocumentId		INT NOT NULL,
		 CONSTRAINT PK_SaveEvents PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_SaveEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
		 CONSTRAINT FK_SaveEvents_CodeDocuments FOREIGN KEY (DocumentId) REFERENCES CodeDocuments(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#SaveEvents') IS NOT NULL
    DROP TABLE #SaveEvents

SELECT	a.Id,
		a.EventLogId,
		a.EventDate,
		a.SolutionName,
		a.DocumentId
INTO #SaveEvents
FROM [OSBIDE.Helplab].dbo.SaveEvents a
INNER JOIN dbo.EventLogs b ON b.Id = a.EventLogId
INNER JOIN dbo.CodeDocuments c ON c.Id = a.DocumentId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[SaveEvents] ON
MERGE [dbo].[SaveEvents] AS Target
USING [#SaveEvents] AS Source ON (Target.Id = Source.Id)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id, EventLogId, EventDate, SolutionName, DocumentId)
	VALUES
	(
		Source.Id,
		Source.EventLogId,
		Source.EventDate,
		Source.SolutionName,
		Source.DocumentId
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[SaveEvents] OFF
