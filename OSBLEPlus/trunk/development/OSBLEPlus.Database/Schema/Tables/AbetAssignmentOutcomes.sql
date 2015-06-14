CREATE TABLE [dbo].[AbetAssignmentOutcomes] (
    [AssignmentID] INT            NOT NULL,
    [Outcome]      NVARCHAR (128) NOT NULL,
    CONSTRAINT [PK_dbo.AbetAssignmentOutcomes] PRIMARY KEY CLUSTERED ([AssignmentID] ASC, [Outcome] ASC),
    CONSTRAINT [FK_dbo.AbetAssignmentOutcomes_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]) ON DELETE CASCADE
);

