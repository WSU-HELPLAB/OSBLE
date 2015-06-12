CREATE TABLE [dbo].[CourseUsers] (
    [ID]               INT IDENTITY (1, 1) NOT NULL,
    [UserProfileID]    INT NOT NULL,
    [AbstractCourseID] INT NOT NULL,
    [AbstractRoleID]   INT NOT NULL,
    [Section]          INT NOT NULL,
    [Hidden]           BIT NOT NULL,
    CONSTRAINT [PK_dbo.CourseUsers] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.CourseUsers_dbo.AbstractCourses_AbstractCourseID] FOREIGN KEY ([AbstractCourseID]) REFERENCES [dbo].[AbstractCourses] ([ID]),
    CONSTRAINT [FK_dbo.CourseUsers_dbo.AbstractRoles_AbstractRoleID] FOREIGN KEY ([AbstractRoleID]) REFERENCES [dbo].[AbstractRoles] ([ID]),
    CONSTRAINT [FK_dbo.CourseUsers_dbo.UserProfiles_UserProfileID] FOREIGN KEY ([UserProfileID]) REFERENCES [dbo].[UserProfiles] ([ID])
);


GO
CREATE TRIGGER [dbo].[CourseUserDelete]
 ON [dbo].[CourseUsers]
 INSTEAD OF DELETE
 AS
 BEGIN;
     DELETE FROM AbstractDashboards WHERE CourseUserID IN (SELECT ID FROM DELETED);
     DELETE FROM DiscussionAssignmentMetaInfoes WHERE CourseUserID IN (SELECT ID FROM DELETED);
     DELETE FROM DiscussionPosts WHERE CourseUserID IN (SELECT ID FROM DELETED);
     DELETE FROM Events WHERE PosterID IN (SELECT ID FROM DELETED);
     DELETE FROM Notifications WHERE SenderID IN (SELECT ID FROM DELETED);
     DELETE FROM Notifications WHERE RecipientID IN (SELECT ID FROM DELETED);
     DELETE FROM RubricEvaluations WHERE EvaluatorID IN (SELECT ID FROM DELETED);
     DELETE FROM TeamEvaluations WHERE EvaluatorID IN (SELECT ID FROM DELETED);
     DELETE FROM TeamEvaluations WHERE RecipientID IN (SELECT ID FROM DELETED);
     DELETE FROM TeamMembers WHERE CourseUserID IN (SELECT ID FROM DELETED);
     DELETE FROM CourseUsers WHERE ID IN (SELECT ID FROM DELETED);
 END;