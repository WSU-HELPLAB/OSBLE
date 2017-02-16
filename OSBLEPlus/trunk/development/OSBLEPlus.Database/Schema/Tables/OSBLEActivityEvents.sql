CREATE TABLE [dbo].[OSBLEActivityEvents](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[EventLogId] [int] NOT NULL,
	[EventAction] [varchar](50) NOT NULL,
	[EventData] [varchar](3000) NULL, 
    [EventDataDescription] VARCHAR(300) NULL
) ON [PRIMARY]
