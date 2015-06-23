CREATE TABLE [dbo].[LocalErrorLogs]
(
	[Id] INT IDENTITY(1,1) NOT NULL,
	[SenderId] INT NOT NULL,
	[LogDate] DATETIME NOT NULL,
	[Content] VARCHAR(max) NOT NULL,
	CONSTRAINT [PK_LocalErrorLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_LocalErrorLogs_UserProfiles] FOREIGN KEY ([SenderId]) REFERENCES [dbo].[UserProfiles] ([Id])
)

GO
