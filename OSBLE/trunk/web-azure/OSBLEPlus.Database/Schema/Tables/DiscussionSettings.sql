CREATE TABLE [dbo].[DiscussionSettings] (
    [AssignmentID]           INT      NOT NULL,
    [AnonymitySettings]      TINYINT  NOT NULL,
    [AssociatedEventID]      INT      NULL,
    [InitialPostDueDate]     DATETIME NOT NULL,
    [MinimumFirstPostLength] INT      NOT NULL,
    [MaximumFirstPostLength] INT      NOT NULL,
    CONSTRAINT [PK_dbo.DiscussionSettings] PRIMARY KEY CLUSTERED ([AssignmentID] ASC),
    CONSTRAINT [FK_dbo.DiscussionSettings_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.DiscussionSettings_dbo.Events_AssociatedEventID] FOREIGN KEY ([AssociatedEventID]) REFERENCES [dbo].[Events] ([ID])
);

