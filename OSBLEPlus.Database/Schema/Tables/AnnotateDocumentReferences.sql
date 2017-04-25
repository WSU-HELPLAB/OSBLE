CREATE TABLE [dbo].[AnnotateDocumentReferences] (
    [ID]                   INT            IDENTITY (1, 1) NOT NULL,
    [OsbleDocumentCode]    NVARCHAR (MAX) NOT NULL,
    [AnnotateDocumentCode] NVARCHAR (MAX) NOT NULL,
    [AnnotateDocumentDate] NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_dbo.AnnotateDocumentReferences] PRIMARY KEY CLUSTERED ([ID] ASC)
);

