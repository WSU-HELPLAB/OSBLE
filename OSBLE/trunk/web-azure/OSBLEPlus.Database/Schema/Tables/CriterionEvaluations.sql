CREATE TABLE [dbo].[CriterionEvaluations] (
    [CriterionID]        INT             NOT NULL,
    [ID]                 INT             IDENTITY (1, 1) NOT NULL,
    [RubricEvaluationID] INT             NOT NULL,
    [Score]              INT             NULL,
    [Comment]            NVARCHAR (4000) NULL,
    CONSTRAINT [PK_dbo.CriterionEvaluations] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.CriterionEvaluations_dbo.Criteria_CriterionID] FOREIGN KEY ([CriterionID]) REFERENCES [dbo].[Criteria] ([ID]) ON DELETE CASCADE,
    CONSTRAINT [FK_dbo.CriterionEvaluations_dbo.RubricEvaluations_RubricEvaluationID] FOREIGN KEY ([RubricEvaluationID]) REFERENCES [dbo].[RubricEvaluations] ([ID]) ON DELETE CASCADE
);

