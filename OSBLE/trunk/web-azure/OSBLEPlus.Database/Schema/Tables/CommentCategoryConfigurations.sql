CREATE TABLE [dbo].[CommentCategoryConfigurations] (
    [ID]   INT            IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_dbo.CommentCategoryConfigurations] PRIMARY KEY CLUSTERED ([ID] ASC)
);

