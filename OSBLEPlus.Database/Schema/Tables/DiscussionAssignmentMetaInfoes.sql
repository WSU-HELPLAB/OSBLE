CREATE TABLE [dbo].[DiscussionAssignmentMetaInfoes] (
    [DiscussionTeamID] INT      NOT NULL,
    [CourseUserID]     INT      NOT NULL,
    [LastVisit]        DATETIME NOT NULL,
    CONSTRAINT [PK_dbo.DiscussionAssignmentMetaInfoes] PRIMARY KEY CLUSTERED ([DiscussionTeamID] ASC, [CourseUserID] ASC),
    CONSTRAINT [FK_dbo.DiscussionAssignmentMetaInfoes_dbo.CourseUsers_CourseUserID] FOREIGN KEY ([CourseUserID]) REFERENCES [dbo].[CourseUsers] ([ID]),
    CONSTRAINT [FK_dbo.DiscussionAssignmentMetaInfoes_dbo.DiscussionTeams_DiscussionTeamID] FOREIGN KEY ([DiscussionTeamID]) REFERENCES [dbo].[DiscussionTeams] ([ID]) ON DELETE CASCADE
);

