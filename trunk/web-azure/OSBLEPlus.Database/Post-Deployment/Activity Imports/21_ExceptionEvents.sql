-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE ExceptionEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical ExceptionEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='ExceptionEvents')
BEGIN

	CREATE TABLE ExceptionEvents
	  (
		 Id						INT IDENTITY NOT NULL,
		 EventLogId				INT NOT NULL,
		 EventDate				DATETIME NOT NULL,
		 SolutionName			VARCHAR(MAX) NOT NULL,
		 ExceptionType			VARCHAR(MAX) NOT NULL,
		 ExceptionName			VARCHAR(MAX) NOT NULL,
		 ExceptionCode			INT NOT NULL,
		 ExceptionDescription	VARCHAR(MAX) NOT NULL,
		 ExceptionAction		INT NOT NULL,
		 DocumentName			VARCHAR(MAX) NOT NULL,
		 LineNumber				INT NOT NULL,
		 LineContent			VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_ExceptionEvents PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_ExceptionEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#ExceptionEvents') IS NOT NULL
    DROP TABLE #ExceptionEvents

SELECT	a.Id,
		a.EventLogId,
		a.EventDate,
		a.SolutionName,
		a.ExceptionType,
		a.ExceptionName,
		a.ExceptionCode,
		a.ExceptionDescription,
		a.ExceptionAction,
		a.DocumentName,
		a.LineNumber,
		a.LineContent
INTO #ExceptionEvents
FROM [OSBIDE.Helplab].dbo.ExceptionEvents a
INNER JOIN dbo.EventLogs b ON b.Id = a.EventLogId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[ExceptionEvents] ON
MERGE [dbo].[ExceptionEvents] AS Target
USING [#ExceptionEvents] AS Source ON (Target.Id = Source.Id)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id,
			EventLogId,
			EventDate,
			SolutionName,
			ExceptionType,
			ExceptionName,
			ExceptionCode,
			ExceptionDescription,
			ExceptionAction,
			DocumentName,
			LineNumber,
			LineContent)
	VALUES
	(
			Source.Id,
			Source.EventLogId,
			Source.EventDate,
			Source.SolutionName,
			Source.ExceptionType,
			Source.ExceptionName,
			Source.ExceptionCode,
			Source.ExceptionDescription,
			Source.ExceptionAction,
			Source.DocumentName,
			Source.LineNumber,
			Source.LineContent
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[ExceptionEvents] OFF
