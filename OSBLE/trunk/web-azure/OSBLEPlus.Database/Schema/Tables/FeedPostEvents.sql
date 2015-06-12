CREATE TABLE [dbo].[FeedPostEvents] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]   INT           NOT NULL,
    [EventDate]    DATETIME      NOT NULL,
    [SolutionName] VARCHAR (MAX) NOT NULL,
    [Comment]      VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_FeedPostEvents] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_FeedPostEvents_EventLogs] FOREIGN KEY ([EventLogId]) REFERENCES [dbo].[EventLogs] ([Id])
);

