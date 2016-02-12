-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE ErrorTypes
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical ErrorTypes table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='ErrorTypes')
BEGIN

	CREATE TABLE ErrorTypes
	  (
		 Id				INT IDENTITY NOT NULL,
		 Name			VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_ErrorTypes PRIMARY KEY CLUSTERED (Id),
	  )
END

TRUNCATE TABLE ErrorTypes

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#ErrorTypes') IS NOT NULL
    DROP TABLE #ErrorTypes

SELECT a.Id, Name
INTO #ErrorTypes
FROM [OSBIDE.Helplab].dbo.ErrorTypes a
INNER JOIN dbo.EventLogs b ON b.Id=a.Id -- not only build event generate build errors

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[ErrorTypes] ON
MERGE [dbo].[ErrorTypes] AS Target
USING [#ErrorTypes] AS Source ON (Target.[Id] = Source.[Id])
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id, Name)
	VALUES (Id, Name)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[ErrorTypes] OFF


