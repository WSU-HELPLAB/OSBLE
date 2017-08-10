CREATE TABLE [dbo].[SubmitEventProperties](
	[EventLogId] [int] NOT NULL,
	[IsWebpageSubmit] [bit] NOT NULL,
	[IsPluginSubmit] [bit] NOT NULL,
 CONSTRAINT [PK_SubmitEventProperties] PRIMARY KEY CLUSTERED 
(
	[EventLogId] ASC,
	[IsWebpageSubmit] ASC,
	[IsPluginSubmit] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
