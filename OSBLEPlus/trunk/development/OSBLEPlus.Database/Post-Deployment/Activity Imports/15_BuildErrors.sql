-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE BuildErrors
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical BuildErrors table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='BuildErrors')
BEGIN

	CREATE TABLE BuildErrors
	  (
		 LogId				INT NOT NULL,
		 BuildErrorTypeId	INT NOT NULL,
		 CONSTRAINT PK_BuildErrors PRIMARY KEY CLUSTERED (LogId, BuildErrorTypeId),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#BuildErrors') IS NOT NULL
    DROP TABLE #BuildErrors

SELECT	a.LogId,
		a.BuildErrorTypeId
INTO	#BuildErrors
FROM [OSBIDE.Helplab].dbo.BuildErrors a
INNER JOIN dbo.ErrorTypes b ON b.Id = a.BuildErrorTypeId
INNER JOIN dbo.EventLogs c ON c.Id = a.LogId

-- Merge data into the target table, only insert or delete records
MERGE [dbo].[BuildErrors] AS Target
USING [#BuildErrors] AS Source ON (Target.LogId = Source.LogId AND Target.BuildErrorTypeId = Source.BuildErrorTypeId)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (LogId, BuildErrorTypeId)
	VALUES (LogId, BuildErrorTypeId)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
