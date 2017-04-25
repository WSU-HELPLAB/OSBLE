CREATE TABLE [dbo].[CommentCategories] (
    [ID]                              INT            IDENTITY (1, 1) NOT NULL,
    [Name]                            NVARCHAR (MAX) NOT NULL,
    [CommentCategoryConfiguration_ID] INT            NULL,
    CONSTRAINT [PK_dbo.CommentCategories] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.CommentCategories_dbo.CommentCategoryConfigurations_CommentCategoryConfiguration_ID] FOREIGN KEY ([CommentCategoryConfiguration_ID]) REFERENCES [dbo].[CommentCategoryConfigurations] ([ID])
);

