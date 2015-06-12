CREATE TABLE [dbo].[ErrorTypes] (
    [Id]   INT           IDENTITY (1, 1) NOT NULL,
    [Name] VARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_ErrorTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
);

