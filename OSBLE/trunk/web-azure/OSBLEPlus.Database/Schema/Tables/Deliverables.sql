CREATE TABLE [dbo].[Deliverables] (
    [AssignmentID] INT            NOT NULL,
    [Name]         NVARCHAR (128) NOT NULL,
    [Type]         INT            NOT NULL,
    CONSTRAINT [PK_dbo.Deliverables] PRIMARY KEY CLUSTERED ([AssignmentID] ASC, [Name] ASC),
    CONSTRAINT [FK_dbo.Deliverables_dbo.Assignments_AssignmentID] FOREIGN KEY ([AssignmentID]) REFERENCES [dbo].[Assignments] ([ID]) ON DELETE CASCADE
);

