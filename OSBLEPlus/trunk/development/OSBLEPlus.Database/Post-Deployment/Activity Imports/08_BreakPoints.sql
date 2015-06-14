-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE BreakPoints
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical BreakPoints table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='BreakPoints')
BEGIN

	CREATE TABLE BreakPoints
	  (
		 Id						INT IDENTITY NOT NULL,
		 Condition				VARCHAR(MAX) NOT NULL,
		 [File]					VARCHAR(MAX) NOT NULL,
		 FileColumn				INT NOT NULL,
		 FileLine				INT NOT NULL,
		 FunctionColumnOffset	INT NOT NULL,
		 FunctionLineOffset		INT NOT NULL,
		 FunctionName			VARCHAR(MAX) NOT NULL,
		 Name					VARCHAR(MAX) NOT NULL,
		 [Enabled]				BIT NOT NULL,
		 CONSTRAINT PK_BreakPoints PRIMARY KEY CLUSTERED (Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#BreakPoints') IS NOT NULL
    DROP TABLE #BreakPoints

SELECT	Id,
		Condition,
		[File],
		FileColumn,
		FileLine,
		FunctionColumnOffset,
		FunctionLineOffset,
		FunctionName,
		Name,
		[Enabled]
INTO #BreakPoints
FROM [OSBIDE.Helplab].dbo.BreakPoints

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[BreakPoints] ON
MERGE [dbo].[BreakPoints] AS Target
USING [#BreakPoints] AS Source ON (Target.[Id] = Source.[Id])
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id,
			Condition,
			[File],
			FileColumn,
			FileLine,
			FunctionColumnOffset,
			FunctionLineOffset,
			FunctionName,
			Name,
			[Enabled])
	VALUES
	(
		Id,
		Condition,
		[File],
		FileColumn,
		FileLine,
		FunctionColumnOffset,
		FunctionLineOffset,
		FunctionName,
		Name,
		[Enabled]
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[BreakPoints] OFF
