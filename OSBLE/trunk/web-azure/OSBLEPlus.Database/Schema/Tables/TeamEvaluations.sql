CREATE TABLE [dbo].[TeamEvaluations] (
    [ID]                         INT            IDENTITY (1, 1) NOT NULL,
    [EvaluatorID]                INT            NOT NULL,
    [RecipientID]                INT            NOT NULL,
    [TeamEvaluationAssignmentID] INT            NOT NULL,
    [AssignmentUnderReviewID]    INT            NOT NULL,
    [Points]                     INT            NOT NULL,
    [CommentID]                  INT            NOT NULL,
    [Comment]                    NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.TeamEvaluations] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.TeamEvaluations_dbo.Assignments_AssignmentUnderReviewID] FOREIGN KEY ([AssignmentUnderReviewID]) REFERENCES [dbo].[Assignments] ([ID]),
    CONSTRAINT [FK_dbo.TeamEvaluations_dbo.Assignments_TeamEvaluationAssignmentID] FOREIGN KEY ([TeamEvaluationAssignmentID]) REFERENCES [dbo].[Assignments] ([ID]),
    CONSTRAINT [FK_dbo.TeamEvaluations_dbo.CourseUsers_EvaluatorID] FOREIGN KEY ([EvaluatorID]) REFERENCES [dbo].[CourseUsers] ([ID]),
    CONSTRAINT [FK_dbo.TeamEvaluations_dbo.CourseUsers_RecipientID] FOREIGN KEY ([RecipientID]) REFERENCES [dbo].[CourseUsers] ([ID]),
    CONSTRAINT [FK_dbo.TeamEvaluations_dbo.TeamEvaluationComments_CommentID] FOREIGN KEY ([CommentID]) REFERENCES [dbo].[TeamEvaluationComments] ([ID]) ON DELETE CASCADE
);

