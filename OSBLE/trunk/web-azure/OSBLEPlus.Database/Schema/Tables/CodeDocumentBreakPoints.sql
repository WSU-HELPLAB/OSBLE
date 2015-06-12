CREATE TABLE [dbo].[CodeDocumentBreakPoints] (
    [CodeFileId]   INT NOT NULL,
    [BreakPointId] INT NOT NULL,
    CONSTRAINT [PK_CodeDocumentBreakPoints] PRIMARY KEY CLUSTERED ([CodeFileId] ASC, [BreakPointId] ASC)
);

