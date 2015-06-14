CREATE TABLE [dbo].[BuildEventBreakPoints] (
    [BuildEventId] INT NOT NULL,
    [BreakPointId] INT NOT NULL,
    CONSTRAINT [PK_BuildEventBreakPoints] PRIMARY KEY CLUSTERED ([BuildEventId] ASC, [BreakPointId] ASC)
);

