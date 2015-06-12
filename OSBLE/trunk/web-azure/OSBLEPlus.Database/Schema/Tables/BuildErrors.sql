CREATE TABLE [dbo].[BuildErrors] (
    [LogId]            INT NOT NULL,
    [BuildErrorTypeId] INT NOT NULL,
    CONSTRAINT [PK_BuildErrors] PRIMARY KEY CLUSTERED ([LogId] ASC, [BuildErrorTypeId] ASC)
);

