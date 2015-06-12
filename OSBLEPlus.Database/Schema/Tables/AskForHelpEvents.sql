CREATE TABLE [dbo].[AskForHelpEvents] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]   INT           NOT NULL,
    [EventDate]    DATETIME      NOT NULL,
    [SolutionName] VARCHAR (MAX) NOT NULL,
    [Code]         VARCHAR (MAX) NOT NULL,
    [UserComment]  VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_AskForHelpEvents_Id] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_AskForHelpEvents_EventLogs] FOREIGN KEY ([EventLogId]) REFERENCES [dbo].[EventLogs] ([Id])
);

