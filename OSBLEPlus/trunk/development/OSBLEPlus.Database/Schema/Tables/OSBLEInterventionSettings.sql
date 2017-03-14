CREATE TABLE [dbo].[OSBLEInterventionSettings](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserProfileId] [int] NOT NULL,
	[ShowInIDESuggestions] [bit] NOT NULL CONSTRAINT [DF_OSBLEInterventionSettings_ShowInIDESuggestions]  DEFAULT ((1)),
	[RefreshThreshold] [int] NOT NULL CONSTRAINT [DF_OSBLEInterventionSettings_RefreshThreshold]  DEFAULT ((10)),
 CONSTRAINT [PK_OSBLEInterventionSettings] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]