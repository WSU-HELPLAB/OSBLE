CREATE TABLE [dbo].[AbstractDashboards] (
    [ID]            INT            IDENTITY (1, 1) NOT NULL,
    [Posted]        DATETIME       NOT NULL,
    [CourseUserID]  INT            NOT NULL,
    [Content]       NVARCHAR (MAX) NOT NULL,
    [CanReply]      BIT            NULL,
    [Discriminator] NVARCHAR (128) NOT NULL,
    [Parent_ID]     INT            NULL,
    CONSTRAINT [PK_dbo.AbstractDashboards] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.AbstractDashboards_dbo.AbstractDashboards_Parent_ID] FOREIGN KEY ([Parent_ID]) REFERENCES [dbo].[AbstractDashboards] ([ID]),
    CONSTRAINT [FK_dbo.AbstractDashboards_dbo.CourseUsers_CourseUserID] FOREIGN KEY ([CourseUserID]) REFERENCES [dbo].[CourseUsers] ([ID])
);

