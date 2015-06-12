-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE CutCopyPasteEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical CutCopyPasteEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='CutCopyPasteEvents')
BEGIN

	CREATE TABLE CutCopyPasteEvents
	  (
		 Id				INT IDENTITY NOT NULL,
		 EventLogId		INT NOT NULL,
		 EventDate		DATETIME NOT NULL,
		 SolutionName	VARCHAR(MAX) NOT NULL,
		 EventAction	INT NOT NULL,
		 DocumentName	VARCHAR(MAX) NOT NULL,
		 Content		VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_CutCopyPasteEvents PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_CutCopyPasteEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#CutCopyPasteEvents') IS NOT NULL
    DROP TABLE #CutCopyPasteEvents

SELECT	a.Id,
		a.EventLogId,
		a.EventDate,
		a.SolutionName,
		a.EventAction,
		a.DocumentName,
		a.Content
INTO #CutCopyPasteEvents
FROM [OSBIDE.Helplab].dbo.CutCopyPasteEvents a
INNER JOIN dbo.EventLogs b ON b.Id = a.EventLogId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[CutCopyPasteEvents] ON
MERGE [dbo].[CutCopyPasteEvents] AS Target
USING [#CutCopyPasteEvents] AS Source ON (Target.Id = Source.Id)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id, EventLogId, EventDate, SolutionName, EventAction, DocumentName,	Content)
	VALUES
	(
		Source.Id,
		Source.EventLogId,
		Source.EventDate,
		Source.SolutionName,
		Source.EventAction,
		Source.DocumentName,
		Source.Content
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[CutCopyPasteEvents] OFF
