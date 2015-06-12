CREATE TABLE [dbo].[AbetSubmissionTags] (
    [ID]                  INT     IDENTITY (1, 1) NOT NULL,
    [AssignmentID]        INT     NOT NULL,
    [CourseUserID]        INT     NOT NULL,
    [SubmissionLevelByte] TINYINT NOT NULL,
    CONSTRAINT [PK_dbo.AbetSubmissionTags] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.AbetSubmissionTags_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.AbetSubmissionTags_dbo.CourseUsers_CourseUserID] FOREIGN KEY ([CourseUserID]) REFERENCES [dbo].[CourseUsers] ([ID]) ON DELETE CASCADE
);

