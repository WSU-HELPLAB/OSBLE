CREATE TABLE [dbo].[EventLogs] (
    [Id]           INT      IDENTITY (1, 1) NOT NULL,
    [EventTypeId]  INT      NOT NULL,
    [EventDate]	   DATETIME NOT NULL,
    [CreatedDate]  DATETIME NOT NULL,
    [SenderId]     INT      NOT NULL,
    CONSTRAINT [PK_EventLogs_Id] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_EventLogs_EventTypes] FOREIGN KEY ([EventTypeId]) REFERENCES [dbo].[EventTypes] ([EventTypeId]),
    CONSTRAINT [FK_EventLogs_UserProfiles] FOREIGN KEY ([SenderId]) REFERENCES [dbo].[UserProfiles] ([ID])
);