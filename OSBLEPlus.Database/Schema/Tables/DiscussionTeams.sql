CREATE TABLE [dbo].[DiscussionTeams] (
    [ID]           INT IDENTITY (1, 1) NOT NULL,
    [AssignmentID] INT NOT NULL,
    [TeamID]       INT NOT NULL,
    [AuthorTeamID] INT NULL,
    CONSTRAINT [PK_dbo.DiscussionTeams] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.DiscussionTeams_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]),
    CONSTRAINT [FK_dbo.DiscussionTeams_dbo.Teams_AuthorTeamID] FOREIGN KEY ([AuthorTeamID]) REFERENCES [dbo].[Teams] ([ID]),
    CONSTRAINT [FK_dbo.DiscussionTeams_dbo.Teams_TeamID] FOREIGN KEY ([TeamID]) REFERENCES [dbo].[Teams] ([ID])
);


GO
CREATE TRIGGER [dbo].[DiscussionTeamDelete]
 ON [dbo].[DiscussionTeams]
 INSTEAD OF DELETE
 AS
 BEGIN;
     DELETE FROM DiscussionPosts WHERE DiscussionTeamID IN (SELECT ID FROM DELETED);
     DELETE FROM DiscussionTeams WHERE ID IN (SELECT ID FROM DELETED);
 END;