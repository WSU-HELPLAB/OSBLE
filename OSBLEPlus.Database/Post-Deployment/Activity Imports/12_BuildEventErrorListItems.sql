-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE BuildEventErrorListItems
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical BuildEventErrorListItems table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='BuildEventErrorListItems')
BEGIN

	CREATE TABLE BuildEventErrorListItems
	  (
		 BuildEventId		INT NOT NULL,
		 ErrorListItemId	INT NOT NULL,
		 CONSTRAINT PK_BuildEventErrorListItems PRIMARY KEY CLUSTERED (BuildEventId, ErrorListItemId),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#BuildEventErrorListItems') IS NOT NULL
    DROP TABLE #BuildEventErrorListItems

SELECT	a.BuildEventId,
		a.ErrorListItemId
INTO	#BuildEventErrorListItems
FROM [OSBIDE.Helplab].dbo.BuildEventErrorListItems a
INNER JOIN dbo.BuildEvents b ON b.Id = a.BuildEventId
INNER JOIN dbo.ErrorListItems c ON c.Id = a.ErrorListItemId

-- Merge data into the target table, only insert or delete records
MERGE [dbo].[BuildEventErrorListItems] AS Target
USING [#BuildEventErrorListItems] AS Source ON (Target.BuildEventId = Source.BuildEventId AND Target.ErrorListItemId = Source.ErrorListItemId)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (BuildEventId, ErrorListItemId)
	VALUES (BuildEventId, ErrorListItemId)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
