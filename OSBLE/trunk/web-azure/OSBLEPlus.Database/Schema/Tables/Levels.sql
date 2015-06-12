CREATE TABLE [dbo].[Levels] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [RubricID]    INT            NOT NULL,
    [PointSpread] INT            NOT NULL,
    [LevelTitle]  NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_dbo.Levels] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.Levels_dbo.Rubrics_RubricID] FOREIGN KEY ([RubricID]) REFERENCES [dbo].[Rubrics] ([ID]) ON DELETE CASCADE
);

