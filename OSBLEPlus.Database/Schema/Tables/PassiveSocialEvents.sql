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
	CONSTRAINT [PK_dbo.ActionRequestLogsSmall] PRIMARY KEY CLUSTERED (	[Id] ASC )
 
 ) 
Go
ALTER TABLE [dbo].[PassiveSocialEvents]  WITH CHECK ADD  CONSTRAINT [FK_dbo.ActionRequestLogs_dbo.OsbideUsers_CreatorId_small] FOREIGN KEY([UserId])
REFERENCES [dbo].[UserProfiles] ([Id])
GO

ALTER TABLE [dbo].[PassiveSocialEvents] CHECK CONSTRAINT [FK_dbo.ActionRequestLogs_dbo.OsbideUsers_CreatorId_small]
GO

