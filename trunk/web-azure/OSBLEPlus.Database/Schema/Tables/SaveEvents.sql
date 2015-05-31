CREATE TABLE [dbo].[SaveEvents] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]   INT           NOT NULL,
    [EventDate]    DATETIME      NOT NULL,
    [SolutionName] VARCHAR (MAX) NOT NULL,
    [DocumentId]   INT           NOT NULL,
    CONSTRAINT [PK_SaveEvents] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SaveEvents_CodeDocuments] FOREIGN KEY ([DocumentId]) REFERENCES [dbo].[CodeDocuments] ([Id])
);

