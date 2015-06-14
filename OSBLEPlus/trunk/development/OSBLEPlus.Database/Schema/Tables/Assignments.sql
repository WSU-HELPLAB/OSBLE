﻿CREATE TABLE [dbo].[Assignments] (
    [ID]                        INT            IDENTITY (1, 1) NOT NULL,
    [AssignmentTypeID]          INT            NOT NULL,
    [AssignmentName]            NVARCHAR (MAX) NOT NULL,
    [AssignmentDescription]     NVARCHAR (MAX) NOT NULL,
    [CourseID]                  INT            NULL,
    [ReleaseDate]               DATETIME       NOT NULL,
    [DueDate]                   DATETIME       NOT NULL,
    [IsAnnotatable]             BIT            NOT NULL,
    [HoursLateWindow]           INT            NOT NULL,
    [DeductionPerUnit]          FLOAT (53)     NOT NULL,
    [HoursPerDeduction]         FLOAT (53)     NOT NULL,
    [IsDraft]                   BIT            NOT NULL,
    [RubricID]                  INT            NULL,
    [StudentRubricID]           INT            NULL,
    [CommentCategoryID]         INT            NULL,
    [PrecededingAssignmentID]   INT            NULL,
    [AssociatedEventID]         INT            NULL,
    [CriticalReviewPublishDate] DATETIME       NULL,
    [ABETDepartment]            NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.Assignments] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.Assignments_dbo.AbstractCourses_CourseID] FOREIGN KEY ([CourseID]) REFERENCES [dbo].[AbstractCourses] ([ID]),
    CONSTRAINT [FK_dbo.Assignments_dbo.Assignments_PrecededingAssignmentID] FOREIGN KEY ([PrecededingAssignmentID]) REFERENCES [dbo].[Assignments] ([ID]),
    CONSTRAINT [FK_dbo.Assignments_dbo.CommentCategoryConfigurations_CommentCategoryID] FOREIGN KEY ([CommentCategoryID]) REFERENCES [dbo].[CommentCategoryConfigurations] ([ID]),
    CONSTRAINT [FK_dbo.Assignments_dbo.Events_AssociatedEventID] FOREIGN KEY ([AssociatedEventID]) REFERENCES [dbo].[Events] ([ID]),
    CONSTRAINT [FK_dbo.Assignments_dbo.Rubrics_RubricID] FOREIGN KEY ([RubricID]) REFERENCES [dbo].[Rubrics] ([ID]),
    CONSTRAINT [FK_dbo.Assignments_dbo.Rubrics_StudentRubricID] FOREIGN KEY ([StudentRubricID]) REFERENCES [dbo].[Rubrics] ([ID])
);

