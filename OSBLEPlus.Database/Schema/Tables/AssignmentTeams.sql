CREATE TABLE [dbo].[AssignmentTeams] (
    [AssignmentID] INT NOT NULL,
    [TeamID]       INT NOT NULL,
    CONSTRAINT [PK_dbo.AssignmentTeams] PRIMARY KEY CLUSTERED ([AssignmentID] ASC, [TeamID] ASC),
    CONSTRAINT [FK_dbo.AssignmentTeams_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.AssignmentTeams_dbo.Teams_TeamID] FOREIGN KEY ([TeamID]) REFERENCES [dbo].[Teams] ([ID]) ON DELETE CASCADE
);

