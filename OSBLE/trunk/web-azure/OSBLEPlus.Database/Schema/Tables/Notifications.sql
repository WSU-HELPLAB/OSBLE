CREATE TABLE [dbo].[Notifications] (
    [ID]          INT             IDENTITY (1, 1) NOT NULL,
    [RecipientID] INT             NOT NULL,
    [SenderID]    INT             NOT NULL,
    [Read]        BIT             NOT NULL,
    [Posted]      DATETIME        NOT NULL,
    [ItemType]    NVARCHAR (MAX)  NULL,
    [ItemID]      INT             NOT NULL,
    [Data]        NVARCHAR (1000) NULL,
    CONSTRAINT [PK_dbo.Notifications] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.Notifications_dbo.CourseUsers_RecipientID] FOREIGN KEY ([RecipientID]) REFERENCES [dbo].[CourseUsers] ([ID]),
    CONSTRAINT [FK_dbo.Notifications_dbo.CourseUsers_SenderID] FOREIGN KEY ([SenderID]) REFERENCES [dbo].[CourseUsers] ([ID])
);

