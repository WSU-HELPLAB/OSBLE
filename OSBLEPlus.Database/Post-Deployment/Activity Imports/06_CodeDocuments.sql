-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE CodeDocuments
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical CodeDocuments table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='CodeDocuments')
BEGIN

	CREATE TABLE CodeDocuments
	  (
		 Id				INT IDENTITY NOT NULL,
		 FileName		VARCHAR(MAX) NOT NULL,
		 Content		VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_CodeDocuments_Id PRIMARY KEY CLUSTERED (Id)
	  )
END

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[CodeDocuments] ON
MERGE [dbo].[CodeDocuments] AS Target
USING [OSBIDE.Helplab].dbo.CodeDocuments AS Source ON (Target.[Id] = Source.[Id])
	WHEN NOT MATCHED BY Target
	THEN
	INSERT ([Id],[FileName],[Content])
	VALUES
	(
		Source.[Id],
		Source.[FileName],
		Source.[Content]
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[CodeDocuments] OFF

