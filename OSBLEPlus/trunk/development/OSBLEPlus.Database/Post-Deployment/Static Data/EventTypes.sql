-- Create the physical EventTypes table if not exits
IF NOT EXISTS(SELECT 1
              FROM   sys.tables
              WHERE  NAME = 'EventTypes')
  BEGIN
      CREATE TABLE [dbo].[EventTypes]
        (
           [EventTypeId]         [INT] IDENTITY(1, 1) NOT NULL,
           [EventTypeName]       [VARCHAR](50) NOT NULL,
           [IsSocialEvent]       [BIT] NOT NULL,
           [IsIDEEvent]          [BIT] NOT NULL,
           [IsFeedEvent]         [BIT] NOT NULL,
           [IsEditEvent]         [BIT] NULL,
           [EventTypeCategoryId] [INT] NULL,
           CONSTRAINT [PK_dbo.EventType] PRIMARY KEY CLUSTERED([EventTypeId] ASC)
        )
  END 

-- Merge data into EventTypes 
IF OBJECT_ID('tempdb..#EventTypes') IS NOT NULL
    DROP TABLE #EventTypes

-- Get existing table schema into a temp table	
SELECT TOP 0 * INTO [#EventTypes] FROM [dbo].[EventTypes]

SET IDENTITY_INSERT [#EventTypes] ON

INSERT INTO [#EventTypes]
([EventTypeId],[EventTypeName],[IsSocialEvent],[IsIDEEvent],[IsFeedEvent],[IsEditEvent],[EventTypeCategoryId])
VALUES
 (1,	'AskForHelpEvent',		1,	0,	1,	0,	1)
,(2,	'BuildEvent',			0,	1,	1,	0,	NULL)
,(3,	'CutCopyPasteEvent',	0,	0,	1,	1,	2)
,(4,	'DebugEvent',			0,	0,	1,	0,	3)
,(5,	'EditorActivityEvent',	0,	0,	1,	1,	2)
,(6,	'ExceptionEvent',		0,	1,	1,	1,	2)
,(7,	'FeedPostEvent',		1,	0,	1,	0,	1)
/* helpful mark and log comment shouldn't be listed as activity feeds, they belong to user profiles */
,(8,	'HelpfulMarkGivenEvent',1,	0,	0,	0,	1)
,(9,	'LogCommentEvent',		1,	0,	0,	0,	1)
/* */
,(10,	'SaveEvent',			0,	0,	1,	1,	2)
,(11,	'SubmitEvent',			1,	0,	1,	1,	2)

SET IDENTITY_INSERT [#EventTypes] OFF

-- Insert, Update or Delete from the target table to make it match the data in the temp table
SET IDENTITY_INSERT [dbo].[EventTypes] ON
MERGE [dbo].[EventTypes] AS Target
USING [#EventTypes] AS Source ON (Target.[EventTypeId] = Source.[EventTypeId])
	WHEN MATCHED
	THEN
		UPDATE SET
		Target.[EventTypeName] = Source.[EventTypeName],
		Target.[IsSocialEvent] = Source.[IsSocialEvent],
		Target.[IsIDEEvent] = Source.[IsIDEEvent],
		Target.[IsFeedEvent] = Source.[IsFeedEvent],
		Target.[IsEditEvent] = Source.[IsEditEvent],
		Target.[EventTypeCategoryId] = Source.[EventTypeCategoryId]
	WHEN NOT MATCHED BY Target
	THEN
	INSERT ([EventTypeId],[EventTypeName],[IsSocialEvent],[IsIDEEvent],[IsFeedEvent],[IsEditEvent],[EventTypeCategoryId])
	VALUES
	(
		Source.[EventTypeId],
		Source.[EventTypeName],
		Source.[IsSocialEvent],
		Source.[IsIDEEvent],
		Source.[IsFeedEvent],
		Source.[IsEditEvent],
		Source.[EventTypeCategoryId]
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[EventTypes] OFF
		
-- Clean up
DROP TABLE [#EventTypes]

PRINT 'Done updating EventTypes table'