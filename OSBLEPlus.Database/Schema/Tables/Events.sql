CREATE TABLE [dbo].[Events] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [PosterID]    INT            NOT NULL,
    [StartDate]   DATETIME       NOT NULL,
    [EndDate]     DATETIME       NULL,
    [Title]       NVARCHAR (100) NOT NULL,
    [Description] NVARCHAR (500) NULL,
    [Approved]    BIT            NOT NULL,
    CONSTRAINT [PK_dbo.Events] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.Events_dbo.CourseUsers_PosterID] FOREIGN KEY ([PosterID]) REFERENCES [dbo].[CourseUsers] ([ID])
);

