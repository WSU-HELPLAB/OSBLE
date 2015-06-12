CREATE TABLE [dbo].[CourseMeetings] (
    [ID]             INT           IDENTITY (1, 1) NOT NULL,
    [Sunday]         BIT           NOT NULL,
    [Monday]         BIT           NOT NULL,
    [Tuesday]        BIT           NOT NULL,
    [Wednesday]      BIT           NOT NULL,
    [Thursday]       BIT           NOT NULL,
    [Friday]         BIT           NOT NULL,
    [Saturday]       BIT           NOT NULL,
    [Name]           NVARCHAR (50) NULL,
    [TimeZoneOffset] INT           NULL,
    [StartTime]      DATETIME      NOT NULL,
    [EndTime]        DATETIME      NOT NULL,
    [Location]       NVARCHAR (50) NULL,
    [Course_ID]      INT           NULL,
    CONSTRAINT [PK_dbo.CourseMeetings] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.CourseMeetings_dbo.AbstractCourses_Course_ID] FOREIGN KEY ([Course_ID]) REFERENCES [dbo].[AbstractCourses] ([ID])
);

