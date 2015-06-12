CREATE TABLE [dbo].[CourseRubrics] (
    [AbstractCourseID] INT NOT NULL,
    [RubricID]         INT NOT NULL,
    CONSTRAINT [PK_dbo.CourseRubrics] PRIMARY KEY CLUSTERED ([AbstractCourseID] ASC, [RubricID] ASC),
    CONSTRAINT [FK_dbo.CourseRubrics_dbo.AbstractCourses_AbstractCourseID] FOREIGN KEY ([AbstractCourseID]) REFERENCES [dbo].[AbstractCourses] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.CourseRubrics_dbo.Rubrics_RubricID] FOREIGN KEY ([RubricID]) REFERENCES [dbo].[Rubrics] ([ID]) ON DELETE CASCADE
);

