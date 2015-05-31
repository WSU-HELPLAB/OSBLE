CREATE TABLE [dbo].[DiscussionPosts] (
    [ID]               INT            IDENTITY (1, 1) NOT NULL,
    [Posted]           DATETIME       NOT NULL,
    [CourseUserID]     INT            NOT NULL,
    [Content]          NVARCHAR (MAX) NOT NULL,
    [AssignmentID]     INT            NOT NULL,
    [ParentPostID]     INT            NULL,
    [DiscussionTeamID] INT            NOT NULL,
    CONSTRAINT [PK_dbo.DiscussionPosts] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.DiscussionPosts_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.DiscussionPosts_dbo.CourseUsers_CourseUserID] FOREIGN KEY ([CourseUserID]) REFERENCES [dbo].[CourseUsers] ([ID]),
    CONSTRAINT [FK_dbo.DiscussionPosts_dbo.DiscussionPosts_ParentPostID] FOREIGN KEY ([ParentPostID]) REFERENCES [dbo].[DiscussionPosts] ([ID]),
    CONSTRAINT [FK_dbo.DiscussionPosts_dbo.DiscussionTeams_DiscussionTeamID] FOREIGN KEY ([DiscussionTeamID]) REFERENCES [dbo].[DiscussionTeams] ([ID])
);

