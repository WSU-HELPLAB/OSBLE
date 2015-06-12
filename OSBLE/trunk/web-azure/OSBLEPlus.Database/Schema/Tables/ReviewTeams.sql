CREATE TABLE [dbo].[ReviewTeams] (
    [AssignmentID] INT NOT NULL,
    [AuthorTeamID] INT NOT NULL,
    [ReviewTeamID] INT NOT NULL,
    CONSTRAINT [PK_dbo.ReviewTeams] PRIMARY KEY CLUSTERED ([AssignmentID] ASC, [AuthorTeamID] ASC, [ReviewTeamID] ASC),
    CONSTRAINT [FK_dbo.ReviewTeams_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.ReviewTeams_dbo.Teams_AuthorTeamID] FOREIGN KEY ([AuthorTeamID]) REFERENCES [dbo].[Teams] ([ID]),
    CONSTRAINT [FK_dbo.ReviewTeams_dbo.Teams_ReviewTeamID] FOREIGN KEY ([ReviewTeamID]) REFERENCES [dbo].[Teams] ([ID])
);

