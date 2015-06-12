CREATE TABLE [dbo].[WhiteTables] (
    [ID]               INT IDENTITY (1, 1) NOT NULL,
    [WhiteTableUserID] INT NOT NULL,
    [AbstractCourseID] INT NOT NULL,
    [Section]          INT NOT NULL,
    [Hidden]           BIT NOT NULL,
    CONSTRAINT [PK_dbo.WhiteTables] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.WhiteTables_dbo.AbstractCourses_AbstractCourseID] FOREIGN KEY ([AbstractCourseID]) REFERENCES [dbo].[AbstractCourses] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.WhiteTables_dbo.WhiteTableUsers_WhiteTableUserID] FOREIGN KEY ([WhiteTableUserID]) REFERENCES [dbo].[WhiteTableUsers] ([ID]) ON DELETE CASCADE
);

