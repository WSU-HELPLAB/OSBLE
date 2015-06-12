CREATE TABLE [dbo].[CellDescriptions] (
    [CriterionID] INT            NOT NULL,
    [LevelID]     INT            NOT NULL,
    [RubricID]    INT            NOT NULL,
    [Description] NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_dbo.CellDescriptions] PRIMARY KEY CLUSTERED ([CriterionID] ASC, [LevelID] ASC),
    CONSTRAINT [FK_dbo.CellDescriptions_dbo.Criteria_CriterionID] FOREIGN KEY ([CriterionID]) REFERENCES [dbo].[Criteria] ([ID]),
    CONSTRAINT [FK_dbo.CellDescriptions_dbo.Levels_LevelID] FOREIGN KEY ([LevelID]) REFERENCES [dbo].[Levels] ([ID]),
    CONSTRAINT [FK_dbo.CellDescriptions_dbo.Rubrics_RubricID] FOREIGN KEY ([RubricID]) REFERENCES [dbo].[Rubrics] ([ID]) ON DELETE CASCADE
);

