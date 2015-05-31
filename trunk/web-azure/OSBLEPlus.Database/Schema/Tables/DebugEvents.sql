CREATE TABLE [dbo].[DebugEvents] (
    [Id]              INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]      INT           NOT NULL,
    [EventDate]       DATETIME      NOT NULL,
    [SolutionName]    VARCHAR (MAX) NOT NULL,
    [ExecutionAction] INT           NOT NULL,
    [DocumentName]    VARCHAR (MAX) NOT NULL,
    [LineNumber]      INT           NOT NULL,
    [DebugOutput]     VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_DebugEvents] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_DebugEvents_EventLogs] FOREIGN KEY ([EventLogId]) REFERENCES [dbo].[EventLogs] ([Id])
);

