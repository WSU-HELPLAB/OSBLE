-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE BuildDocuments
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical BuildDocuments table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='BuildDocuments')
BEGIN

	CREATE TABLE BuildDocuments
	  (
		 BuildId			INT NOT NULL,
		 DocumentId			INT NOT NULL,
		 NumberOfInserted	INT,
		 NumberOfModified	INT,
		 NumberOfDeleted	INT,
		 ModifiedLines		VARCHAR(MAX),
		 UpdatedOn			DATETIME,
		 UpdatedBy			INT,
		 CONSTRAINT PK_BuildDocuments PRIMARY KEY CLUSTERED (BuildId, DocumentId),
		 CONSTRAINT FK_BuildDocuments_BuildEvents FOREIGN KEY (BuildId) REFERENCES BuildEvents(Id),
		 CONSTRAINT FK_BuildDocuments_CodeDocuments FOREIGN KEY (DocumentId) REFERENCES CodeDocuments(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#BuildDocuments') IS NOT NULL
    DROP TABLE #BuildDocuments

SELECT a.[BuildId],a.[DocumentId],a.[NumberOfInserted],a.[NumberOfModified],a.[NumberOfDeleted],a.[ModifiedLines],a.[UpdatedOn],a.[UpdatedBy]
INTO #BuildDocuments
FROM [OSBIDE.Helplab].dbo.BuildDocuments a
INNER JOIN dbo.BuildEvents b ON b.Id = a.BuildId
INNER JOIN dbo.CodeDocuments c ON c.Id = a.DocumentId

-- Merge data into the target table, only insert or delete records
MERGE [dbo].[BuildDocuments] AS Target
USING [#BuildDocuments] AS Source ON (Target.[BuildId] = Source.[BuildId] AND Target.[DocumentId] = Source.[DocumentId])
	WHEN NOT MATCHED BY Target
	THEN
	INSERT ([BuildId],[DocumentId],[NumberOfInserted],[NumberOfModified],[NumberOfDeleted],[ModifiedLines],[UpdatedOn],[UpdatedBy])
	VALUES
	(
		Source.[BuildId],
		Source.[DocumentId],
		Source.[NumberOfInserted],
		Source.[NumberOfModified],
		Source.[NumberOfDeleted],
		Source.[ModifiedLines],
		Source.[UpdatedOn],
		Source.[UpdatedBy]
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
