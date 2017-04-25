CREATE TABLE [dbo].[TeamMembers] (
    [TeamID]       INT NOT NULL,
    [CourseUserID] INT NOT NULL,
    CONSTRAINT [PK_dbo.TeamMembers] PRIMARY KEY CLUSTERED ([TeamID] ASC, [CourseUserID] ASC),
    CONSTRAINT [FK_dbo.TeamMembers_dbo.CourseUsers_CourseUserID] FOREIGN KEY ([CourseUserID]) REFERENCES [dbo].[CourseUsers] ([ID]),
    CONSTRAINT [FK_dbo.TeamMembers_dbo.Teams_TeamID] FOREIGN KEY ([TeamID]) REFERENCES [dbo].[Teams] ([ID]) ON DELETE CASCADE
);

