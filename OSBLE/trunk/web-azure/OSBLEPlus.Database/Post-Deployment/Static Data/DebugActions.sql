-- Create the physical DebugActions table if not exits
IF NOT EXISTS(SELECT 1
              FROM   sys.tables
              WHERE  NAME = 'DebugActions')
  BEGIN
      CREATE TABLE [dbo].[DebugActions]
        (
           [Id]         [INT] IDENTITY(1, 1) NOT NULL,
           [Name]       [VARCHAR](50) NOT NULL,
           CONSTRAINT [PK_dbo.DebugAction] PRIMARY KEY CLUSTERED([Id] ASC)
        )
  END 

-- Merge data into DebugActions 
IF OBJECT_ID('tempdb..#DebugActions') IS NOT NULL
    DROP TABLE #DebugActions

-- Get existing table schema into a temp table	
SELECT TOP 0 * INTO [#DebugActions] FROM [dbo].[DebugActions]

SET IDENTITY_INSERT [#DebugActions] ON

INSERT INTO [#DebugActions] ([Id],[Name])
VALUES
	 (1,'Undefined')
	,(2,'Start')
	,(3,'StepOver')
	,(4,'StepInto')
	,(5,'StepOut')
	,(6,'StopDebugging')
	,(7,'StartWithoutDebugging')

SET IDENTITY_INSERT [#DebugActions] OFF

-- Insert, Update or Delete from the target table to make it match the data in the temp table
SET IDENTITY_INSERT [dbo].[DebugActions] ON
MERGE [dbo].[DebugActions] AS Target
USING [#DebugActions] AS Source ON (Target.[Id] = Source.[Id])
	WHEN MATCHED
	THEN
		UPDATE SET
		Target.[Name] = Source.[Name]
	WHEN NOT MATCHED BY Target
	THEN
	INSERT ([Id],[Name])
	VALUES
	(
		Source.[Id],
		Source.[Name]
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[DebugActions] OFF
		
-- Clean up
DROP TABLE [#DebugActions]

PRINT 'Done updating DebugActions table'



