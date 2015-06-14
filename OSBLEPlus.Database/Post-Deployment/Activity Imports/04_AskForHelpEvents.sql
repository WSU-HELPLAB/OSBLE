-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE AskForHelpEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical AskForHelpEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='AskForHelpEvents')
BEGIN

	CREATE TABLE AskForHelpEvents
	  (
		 Id				INT IDENTITY NOT NULL,
		 EventLogId		INT NOT NULL,
		 EventDate		DATETIME NOT NULL,
		 SolutionName	VARCHAR(MAX) NOT NULL,
		 Code			VARCHAR(MAX) NOT NULL,
		 UserComment	VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_AskForHelpEvents_Id PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_AskForHelpEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#AskForHelpEvents') IS NOT NULL
    DROP TABLE #AskForHelpEvents

SELECT a.[Id],a.[EventLogId],a.[EventDate],a.[SolutionName],a.[Code],a.[UserComment]
INTO #AskForHelpEvents
FROM [OSBIDE.Helplab].dbo.AskForHelpEvents a
INNER JOIN dbo.EventLogs b on b.Id = a.EventLogId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[AskForHelpEvents] ON
MERGE [dbo].[AskForHelpEvents] AS Target
USING [#AskForHelpEvents] AS Source ON (Target.[Id] = Source.[Id])
	WHEN NOT MATCHED BY Target
	THEN
	INSERT ([Id],[EventLogId],[EventDate],[SolutionName],[Code],[UserComment])
	VALUES
	(
		Source.[Id],
		Source.[EventLogId],
		Source.[EventDate],
		Source.[SolutionName],
		Source.[Code],
		Source.[UserComment]
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[AskForHelpEvents] OFF

