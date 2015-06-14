CREATE TABLE [dbo].[CodeDocumentErrorListItems] (
    [CodeFileId]      INT NOT NULL,
    [ErrorListItemId] INT NOT NULL,
    CONSTRAINT [PK_CodeDocumentErrorListItems] PRIMARY KEY CLUSTERED ([CodeFileId] ASC, [ErrorListItemId] ASC)
);

