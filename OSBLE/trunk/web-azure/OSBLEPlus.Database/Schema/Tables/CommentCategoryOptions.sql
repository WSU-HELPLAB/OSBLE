CREATE TABLE [dbo].[CommentCategoryOptions] (
    [ID]                 INT            IDENTITY (1, 1) NOT NULL,
    [Name]               NVARCHAR (MAX) NOT NULL,
    [CommentCategory_ID] INT            NULL,
    CONSTRAINT [PK_dbo.CommentCategoryOptions] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_dbo.CommentCategoryOptions_dbo.CommentCategories_CommentCategory_ID] FOREIGN KEY ([CommentCategory_ID]) REFERENCES [dbo].[CommentCategories] ([ID])
);

