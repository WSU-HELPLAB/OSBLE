CREATE TABLE [dbo].[TeamEvaluationSettings] (
    [AssignmentID]          INT        NOT NULL,
    [MaximumMultiplier]     FLOAT (53) NOT NULL,
    [RequiredCommentLength] INT        NOT NULL,
    [DiscrepancyCheckSize]  INT        NOT NULL,
    CONSTRAINT [PK_dbo.TeamEvaluationSettings] PRIMARY KEY CLUSTERED ([AssignmentID] ASC),
    CONSTRAINT [FK_dbo.TeamEvaluationSettings_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]) ON DELETE CASCADE
);

