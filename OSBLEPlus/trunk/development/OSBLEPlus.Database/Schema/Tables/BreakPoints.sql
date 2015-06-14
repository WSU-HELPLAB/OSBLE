CREATE TABLE [dbo].[BreakPoints] (
    [Id]                   INT           IDENTITY (1, 1) NOT NULL,
    [Condition]            VARCHAR (MAX) NOT NULL,
    [File]                 VARCHAR (MAX) NOT NULL,
    [FileColumn]           INT           NOT NULL,
    [FileLine]             INT           NOT NULL,
    [FunctionColumnOffset] INT           NOT NULL,
    [FunctionLineOffset]   INT           NOT NULL,
    [FunctionName]         VARCHAR (MAX) NOT NULL,
    [Name]                 VARCHAR (MAX) NOT NULL,
    [Enabled]              BIT           NOT NULL,
    CONSTRAINT [PK_BreakPoints] PRIMARY KEY CLUSTERED ([Id] ASC)
);

