CREATE TABLE [dbo].[CriticalReviewSettings] (
    [AssignmentID]   INT     NOT NULL,
    [ReviewSettings] TINYINT NOT NULL,
    CONSTRAINT [PK_dbo.CriticalReviewSettings] PRIMARY KEY CLUSTERED ([AssignmentID] ASC),
    CONSTRAINT [FK_dbo.CriticalReviewSettings_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]) ON DELETE CASCADE
);

