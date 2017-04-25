CREATE TABLE [dbo].[CodeDocuments] (
    [Id]       INT           IDENTITY (1, 1) NOT NULL,
    [FileName] VARCHAR (MAX) NOT NULL,
    [Content]  VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_CodeDocuments_Id] PRIMARY KEY CLUSTERED ([Id] ASC)
);

