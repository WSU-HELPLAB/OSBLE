CREATE TABLE [dbo].[WhiteTableUsers] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [CourseID]       INT            NOT NULL,
    [SchoolID]       INT            NOT NULL,
    [Identification] NVARCHAR (MAX) NOT NULL,
    [Name1]          NVARCHAR (MAX) NULL,
    [Name2]          NVARCHAR (MAX) NULL,
    [Email]          NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.WhiteTableUsers] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.WhiteTableUsers_dbo.Schools_SchoolID] FOREIGN KEY ([SchoolID]) REFERENCES [dbo].[Schools] ([ID]) ON DELETE CASCADE
);

