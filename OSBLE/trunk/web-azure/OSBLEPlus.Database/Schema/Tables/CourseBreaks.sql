CREATE TABLE [dbo].[CourseBreaks] (
    [ID]        INT           IDENTITY (1, 1) NOT NULL,
    [StartDate] DATETIME      NOT NULL,
    [EndDate]   DATETIME      NOT NULL,
    [Name]      NVARCHAR (50) NULL,
    [Course_ID] INT           NULL,
    CONSTRAINT [PK_dbo.CourseBreaks] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.CourseBreaks_dbo.AbstractCourses_Course_ID] FOREIGN KEY ([Course_ID]) REFERENCES [dbo].[AbstractCourses] ([ID])
);

