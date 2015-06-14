CREATE TABLE [dbo].[Criteria] (
    [ID]             INT            IDENTITY (1, 1) NOT NULL,
    [RubricID]       INT            NOT NULL,
    [CriterionTitle] NVARCHAR (MAX) NOT NULL,
    [Weight]         FLOAT (53)     NOT NULL,
    CONSTRAINT [PK_dbo.Criteria] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.Criteria_dbo.Rubrics_RubricID] FOREIGN KEY ([RubricID]) REFERENCES [dbo].[Rubrics] ([ID]) ON DELETE CASCADE
);

