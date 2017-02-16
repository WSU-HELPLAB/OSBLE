CREATE TABLE [dbo].[OSBLEInterventions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserProfileId] [int] NOT NULL,
	[InterventionTrigger] [varchar](50) NULL,
	[InterventionMarkedHelpful] [bit] NULL,
	[InterventionDateTime] [datetime] NOT NULL,
	[InterventionType] [varchar](50) NOT NULL,
	[Icon1] [varchar](50) NOT NULL,
	[Icon2] [varchar](50) NOT NULL,
	[Title] [varchar](50) NOT NULL,
	[Link] [varchar](500) NOT NULL,
	[LinkText] [varchar](500) NOT NULL,
	[ContentFirst] [bit] NOT NULL,
	[ListItemContent] [varchar](500) NOT NULL,
	[InterventionTemplateText] [varchar](500) NULL,
	[InterventionSuggestedCode] [varchar](5000) NULL,
	[IsDismissed] [bit] NULL,
 CONSTRAINT [PK_OSBLEInterventions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]