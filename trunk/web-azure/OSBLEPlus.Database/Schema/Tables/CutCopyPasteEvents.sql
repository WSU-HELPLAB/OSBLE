CREATE TABLE [dbo].[CutCopyPasteEvents] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]   INT           NOT NULL,
    [EventDate]    DATETIME      NOT NULL,
    [SolutionName] VARCHAR (MAX) NOT NULL,
    [EventAction]  INT           NOT NULL,
    [DocumentName] VARCHAR (MAX) NOT NULL,
    [Content]      VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_CutCopyPasteEvents] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_CutCopyPasteEvents_EventLogs] FOREIGN KEY ([EventLogId]) REFERENCES [dbo].[EventLogs] ([Id])
);

