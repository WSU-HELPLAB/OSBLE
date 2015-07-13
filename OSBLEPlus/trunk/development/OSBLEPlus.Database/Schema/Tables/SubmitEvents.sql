CREATE TABLE [dbo].[SubmitEvents] (
    [Id]           INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]   INT           NOT NULL,
    [EventDate]    DATETIME      NOT NULL,
    [SolutionName] VARCHAR (MAX) NOT NULL,
    [AssignmentId] INT           NOT NULL,
    [SolutionData] IMAGE         NULL,
    CONSTRAINT [PK_SubmitEvents] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_SubmitEvents_EventLogs] FOREIGN KEY ([EventLogId]) REFERENCES [dbo].[EventLogs] ([Id]),
    CONSTRAINT [FK_SubmitEvents_Assignments] FOREIGN KEY ([AssignmentId]) REFERENCES [dbo].[Assignments] ([ID])
);

