CREATE TABLE [dbo].[BuildDocuments] (
    [BuildId]          INT           NOT NULL,
    [DocumentId]       INT           NOT NULL,
    [NumberOfInserted] INT           NULL,
    [NumberOfModified] INT           NULL,
    [NumberOfDeleted]  INT           NULL,
    [ModifiedLines]    VARCHAR (MAX) NULL,
    [UpdatedOn]        DATETIME      NULL,
    [UpdatedBy]        INT           NULL,
    CONSTRAINT [PK_BuildDocuments] PRIMARY KEY CLUSTERED ([BuildId] ASC, [DocumentId] ASC),
    CONSTRAINT [FK_BuildDocuments_BuildEvents] FOREIGN KEY ([BuildId]) REFERENCES [dbo].[BuildEvents] ([Id]),
    CONSTRAINT [FK_BuildDocuments_CodeDocuments] FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[CodeDocuments] ([Id])
);

