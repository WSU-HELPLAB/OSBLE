CREATE TABLE [dbo].[Schools] (
    [ID]   INT            IDENTITY (1, 1) NOT NULL,
    [Name] NVARCHAR (128) NULL,
    CONSTRAINT [PK_dbo.Schools] PRIMARY KEY CLUSTERED ([ID] ASC)
);

