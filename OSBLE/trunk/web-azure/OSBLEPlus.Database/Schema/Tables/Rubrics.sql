CREATE TABLE [dbo].[Rubrics] (
    [ID]                  INT            IDENTITY (1, 1) NOT NULL,
    [Description]         NVARCHAR (MAX) NOT NULL,
    [HasCriteriaComments] BIT            NOT NULL,
    [HasGlobalComments]   BIT            NOT NULL,
    CONSTRAINT [PK_dbo.Rubrics] PRIMARY KEY CLUSTERED ([ID] ASC)
);

