CREATE TABLE [dbo].[ActivityLogs] (
    [ID]        INT            IDENTITY (1, 1) NOT NULL,
    [Sender]    NVARCHAR (MAX) NULL,
    [UserID]    INT            NULL,
    [Timestamp] DATETIME       NOT NULL,
    [Message]   NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.ActivityLogs] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.ActivityLogs_dbo.UserProfiles_UserID] FOREIGN KEY ([UserID]) REFERENCES [dbo].[UserProfiles] ([ID])
);

