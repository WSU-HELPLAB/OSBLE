CREATE TABLE [dbo].[BuildEventErrorListItems] (
    [BuildEventId]    INT NOT NULL,
    [ErrorListItemId] INT NOT NULL,
    CONSTRAINT [PK_BuildEventErrorListItems] PRIMARY KEY CLUSTERED ([BuildEventId] ASC, [ErrorListItemId] ASC)
);

