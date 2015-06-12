CREATE TABLE [dbo].[EventTypes] (
    [EventTypeId]         INT          IDENTITY (1, 1) NOT NULL,
    [EventTypeName]       VARCHAR (50) NOT NULL,
    [IsSocialEvent]       BIT          NOT NULL,
    [IsIDEEvent]          BIT          NOT NULL,
    [IsFeedEvent]         BIT          NOT NULL,
    [IsEditEvent]         BIT          NULL,
    [EventTypeCategoryId] INT          NULL,
    CONSTRAINT [PK_dbo.EventType] PRIMARY KEY CLUSTERED ([EventTypeId] ASC)
);

