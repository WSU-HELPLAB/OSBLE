CREATE TABLE [dbo].[BuildEvents] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]   INT           NOT NULL,
    [EventDate]    DATETIME      NOT NULL,
    [SolutionName] VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_BuildEvents_Id] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_BuildEvents_EventLogs] FOREIGN KEY ([EventLogId]) REFERENCES [dbo].[EventLogs] ([Id])
);

