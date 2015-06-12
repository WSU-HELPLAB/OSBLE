CREATE TABLE [dbo].[HelpfulMarkGivenEvents] (
    [Id]                INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]        INT           NOT NULL,
    [LogCommentEventId] INT           NOT NULL,
    [EventDate]         DATETIME      NOT NULL,
    [SolutionName]      VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_HelpfulMarkGivenEvents] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_HelpfulMarkGivenEvents_EventLogs] FOREIGN KEY ([EventLogId]) REFERENCES [dbo].[EventLogs] ([Id]),
    CONSTRAINT [FK_HelpfulMarkGivenSourceEvents_LogCommentEvents] FOREIGN KEY ([LogCommentEventId]) REFERENCES [dbo].[LogCommentEvents] ([Id])
);

