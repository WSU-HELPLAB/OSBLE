CREATE TABLE [dbo].[ErrorListItems] (
    [Id]          INT           IDENTITY (1, 1) NOT NULL,
    [Column]      INT           NOT NULL,
    [Line]        INT           NOT NULL,
    [File]        VARCHAR (MAX) NOT NULL,
    [Project]     VARCHAR (MAX) NOT NULL,
    [Description] VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_ErrorListItems] PRIMARY KEY CLUSTERED ([Id] ASC)
);

