-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE ErrorListItems
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical ErrorListItems table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='ErrorListItems')
BEGIN

	CREATE TABLE ErrorListItems
	  (
		 Id				INT IDENTITY NOT NULL,
		 [Column]		INT NOT NULL,
		 Line			INT NOT NULL,
		 [File]			VARCHAR(MAX) NOT NULL,
		 Project		VARCHAR(MAX) NOT NULL,
		 [Description]	VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_ErrorListItems PRIMARY KEY CLUSTERED (Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#ErrorListItems') IS NOT NULL
    DROP TABLE #ErrorListItems

SELECT	Id,
		[Column],
		Line,
		[File],
		Project,
		[Description]
INTO #ErrorListItems
FROM [OSBIDE.Helplab].dbo.ErrorListItems

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[ErrorListItems] ON

MERGE [dbo].[ErrorListItems] AS Target
USING [#ErrorListItems] AS Source ON (Target.[Id] = Source.[Id])
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id,
			[Column],
			Line,
			[File],
			Project,
			[Description]
			)
	VALUES
	(
		Id,
		[Column],
		Line,
		[File],
		Project,
		[Description]
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;

SET IDENTITY_INSERT [dbo].[ErrorListItems] OFF
