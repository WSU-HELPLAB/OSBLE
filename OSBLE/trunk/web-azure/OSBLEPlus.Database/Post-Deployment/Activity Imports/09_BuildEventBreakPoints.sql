-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE BuildEventBreakPoints
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical BuildEventBreakPoints table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='BuildEventBreakPoints')
BEGIN

	CREATE TABLE BuildEventBreakPoints
	  (
		 BuildEventId	INT NOT NULL,
		 BreakPointId	INT NOT NULL,
		 CONSTRAINT PK_BuildEventBreakPoints PRIMARY KEY CLUSTERED (BuildEventId, BreakPointId),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#BuildEventBreakPoints') IS NOT NULL
    DROP TABLE #BuildEventBreakPoints

SELECT	a.BuildEventId,
		a.BreakPointId
INTO	#BuildEventBreakPoints
FROM [OSBIDE.Helplab].dbo.BuildEventBreakPoints a
INNER JOIN dbo.BuildEvents b ON b.Id = a.BuildEventId
INNER JOIN dbo.BreakPoints c ON c.Id = a.BreakPointId

-- Merge data into the target table, only insert or delete records
MERGE [dbo].[BuildEventBreakPoints] AS Target
USING [#BuildEventBreakPoints] AS Source ON (Target.BuildEventId = Source.BuildEventId AND Target.BreakPointId = Source.BreakPointId)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (BuildEventId, BreakPointId)
	VALUES (BuildEventId, BreakPointId)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
