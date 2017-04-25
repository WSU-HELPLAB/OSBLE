CREATE TABLE [dbo].[TeamEvaluationComments] (
    [ID]      INT            IDENTITY (1, 1) NOT NULL,
    [Comment] NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_dbo.TeamEvaluationComments] PRIMARY KEY CLUSTERED ([ID] ASC)
);

