CREATE TABLE [dbo].[LogCommentEvents] (
    [Id]               INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]       INT           NOT NULL,
    [SourceEventLogId] INT           NOT NULL,
    [EventDate]        DATETIME      NOT NULL,
    [SolutionName]     VARCHAR (MAX) NOT NULL,
    [Content]          VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_LogCommentEvents] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_LogCommentEvents_EventLogs] FOREIGN KEY ([EventLogId]) REFERENCES [dbo].[EventLogs] ([Id]),
    CONSTRAINT [FK_LogCommentSourceEvents_EventLogs] FOREIGN KEY ([SourceEventLogId]) REFERENCES [dbo].[EventLogs] ([Id])
);

