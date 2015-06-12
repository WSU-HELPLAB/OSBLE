CREATE TABLE [dbo].[RubricEvaluations] (
    [ID]            INT             IDENTITY (1, 1) NOT NULL,
    [EvaluatorID]   INT             NOT NULL,
    [RecipientID]   INT             NOT NULL,
    [AssignmentID]  INT             NOT NULL,
    [IsPublished]   BIT             NOT NULL,
    [DatePublished] DATETIME        NULL,
    [GlobalComment] NVARCHAR (4000) NULL,
    CONSTRAINT [PK_dbo.RubricEvaluations] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.RubricEvaluations_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]),
    CONSTRAINT [FK_dbo.RubricEvaluations_dbo.CourseUsers_EvaluatorID] FOREIGN KEY ([EvaluatorID]) REFERENCES [dbo].[CourseUsers] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.RubricEvaluations_dbo.Teams_RecipientID] FOREIGN KEY ([RecipientID]) REFERENCES [dbo].[Teams] ([ID])
);

