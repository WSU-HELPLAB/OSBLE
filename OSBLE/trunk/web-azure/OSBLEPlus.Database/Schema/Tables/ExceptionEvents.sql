CREATE TABLE [dbo].[ExceptionEvents] (
    [Id]                   INT           IDENTITY (1, 1) NOT NULL,
    [EventLogId]           INT           NOT NULL,
    [EventDate]            DATETIME      NOT NULL,
    [SolutionName]         VARCHAR (MAX) NOT NULL,
    [ExceptionType]        VARCHAR (MAX) NOT NULL,
    [ExceptionName]        VARCHAR (MAX) NOT NULL,
    [ExceptionCode]        INT           NOT NULL,
    [ExceptionDescription] VARCHAR (MAX) NOT NULL,
    [ExceptionAction]      INT           NOT NULL,
    [DocumentName]         VARCHAR (MAX) NOT NULL,
    [LineNumber]           INT           NOT NULL,
    [LineContent]          VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_ExceptionEvents] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_ExceptionEvents_EventLogs] FOREIGN KEY ([EventLogId]) REFERENCES [dbo].[EventLogs] ([Id])
);

