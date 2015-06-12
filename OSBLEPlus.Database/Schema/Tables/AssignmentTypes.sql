CREATE TABLE [dbo].[AssignmentTypes] (
    [Id]          INT           IDENTITY (1, 1) NOT NULL,
    [NAME]        VARCHAR (100) NOT NULL,
    [Description] VARCHAR (200) NULL,
    CONSTRAINT [PK_AssignmentTypes] PRIMARY KEY CLUSTERED ([Id] ASC)
);

