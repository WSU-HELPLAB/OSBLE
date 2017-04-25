CREATE TABLE [dbo].[EditorActivityEvents] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]   INT           NOT NULL,
    [EventDate]    DATETIME      NOT NULL,
    [SolutionName] VARCHAR (MAX) NOT NULL,
    [Line] INT NULL, 
    CONSTRAINT [PK_EditorActivityEvents] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_EditorActivityEvents_EventLogs] FOREIGN KEY ([EventLogId]) REFERENCES [dbo].[EventLogs] ([Id])
);

