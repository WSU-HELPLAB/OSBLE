-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE CodeDocumentErrorListItems
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical CodeDocumentErrorListItems table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='CodeDocumentErrorListItems')
BEGIN

	CREATE TABLE CodeDocumentErrorListItems
	  (
		 CodeFileId			INT NOT NULL,
		 ErrorListItemId	INT NOT NULL,
		 CONSTRAINT PK_CodeDocumentErrorListItems PRIMARY KEY CLUSTERED (CodeFileId, ErrorListItemId),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#CodeDocumentErrorListItems') IS NOT NULL
    DROP TABLE #CodeDocumentErrorListItems

SELECT	a.CodeFileId,
		a.ErrorListItemId
INTO	#CodeDocumentErrorListItems
FROM [OSBIDE.Helplab].dbo.CodeDocumentErrorListItems a
INNER JOIN dbo.CodeDocuments b ON b.Id = a.CodeFileId
INNER JOIN dbo.ErrorListItems c ON c.Id = a.ErrorListItemId

-- Merge data into the target table, only insert or delete records
MERGE [dbo].[CodeDocumentErrorListItems] AS Target
USING [#CodeDocumentErrorListItems] AS Source ON (Target.CodeFileId = Source.CodeFileId AND Target.ErrorListItemId = Source.ErrorListItemId)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (CodeFileId, ErrorListItemId)
	VALUES (CodeFileId, ErrorListItemId)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
