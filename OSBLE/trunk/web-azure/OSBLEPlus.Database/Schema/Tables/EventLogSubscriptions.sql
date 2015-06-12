CREATE TABLE [dbo].[EventLogSubscriptions] (
    [UserId] INT NOT NULL,
    [LogId]  INT NOT NULL,
    CONSTRAINT [PK_EventLogSubscriptions] PRIMARY KEY CLUSTERED ([UserId] ASC, [LogId] ASC)
);

