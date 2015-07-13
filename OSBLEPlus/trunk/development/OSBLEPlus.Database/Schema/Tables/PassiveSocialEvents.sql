CREATE TABLE [dbo].[PassiveSocialEvents](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[ControllerName] [nvarchar](max) NULL,
	[ActionName] [nvarchar](max) NULL,
	[ActionParameter1] [nvarchar](max) NULL,
	[ActionParameter2] [nvarchar](max) NULL,
	[ActionParameter3] [nvarchar](max) NULL,
	[ActionParameters] [nvarchar](max) NULL,
	[EventCode] [nvarchar](8) NULL,
	[AccessDate] [datetime] NOT NULL,
	CONSTRAINT [PK_dbo.ActionRequestLogsSmall] PRIMARY KEY CLUSTERED (	[Id] ASC ),
	CONSTRAINT [FK_ActionRequestLogs_OsbideUsers] FOREIGN KEY([UserId]) REFERENCES [dbo].[UserProfiles] ([ID])
  ) 
