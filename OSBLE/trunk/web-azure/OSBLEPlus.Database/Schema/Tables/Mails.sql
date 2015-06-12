CREATE TABLE [dbo].[Mails] (
    [ID]                INT            IDENTITY (1, 1) NOT NULL,
    [ThreadID]          INT            NOT NULL,
    [ContextID]         INT            NOT NULL,
    [FromUserProfileID] INT            NOT NULL,
    [ToUserProfileID]   INT            NOT NULL,
    [Read]              BIT            NOT NULL,
    [Posted]            DATETIME       NOT NULL,
    [Subject]           NVARCHAR (100) NOT NULL,
    [Message]           NVARCHAR (MAX) NOT NULL,
    [DeleteFromOutbox]  BIT            NOT NULL,
    [DeleteFromInbox]   BIT            NOT NULL,
    CONSTRAINT [PK_dbo.Mails] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.Mails_dbo.AbstractCourses_ContextID] FOREIGN KEY ([ContextID]) REFERENCES [dbo].[AbstractCourses] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.Mails_dbo.UserProfiles_FromUserProfileID] FOREIGN KEY ([FromUserProfileID]) REFERENCES [dbo].[UserProfiles] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.Mails_dbo.UserProfiles_ToUserProfileID] FOREIGN KEY ([ToUserProfileID]) REFERENCES [dbo].[UserProfiles] ([ID])
);

