-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE CodeDocumentBreakPoints
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical CodeDocumentBreakPoints table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='CodeDocumentBreakPoints')
BEGIN

	CREATE TABLE CodeDocumentBreakPoints
	  (
		 CodeFileId	INT NOT NULL,
		 BreakPointId	INT NOT NULL,
		 CONSTRAINT PK_CodeDocumentBreakPoints PRIMARY KEY CLUSTERED (CodeFileId, BreakPointId),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#CodeDocumentBreakPoints') IS NOT NULL
    DROP TABLE #CodeDocumentBreakPoints

SELECT	a.CodeFileId,
		a.BreakPointId
INTO	#CodeDocumentBreakPoints
FROM [OSBIDE.Helplab].dbo.CodeDocumentBreakPoints a
INNER JOIN dbo.CodeDocuments b ON b.Id = a.CodeFileId
INNER JOIN dbo.BreakPoints c ON c.Id = a.BreakPointId

-- Merge data into the target table, only insert or delete records
MERGE [dbo].[CodeDocumentBreakPoints] AS Target
USING [#CodeDocumentBreakPoints] AS Source ON (Target.CodeFileId = Source.CodeFileId AND Target.BreakPointId = Source.BreakPointId)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (CodeFileId, BreakPointId)
	VALUES (CodeFileId, BreakPointId)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
