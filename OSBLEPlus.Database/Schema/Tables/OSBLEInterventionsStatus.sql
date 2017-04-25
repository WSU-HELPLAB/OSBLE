CREATE TABLE [dbo].[OSBLEInterventionsStatus](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserProfileId] [int] NOT NULL,
	[RefreshInterventions] [bit] NOT NULL,
	[LastRefresh] [datetime] NULL,
	[RefreshInterventionsDashboard] BIT NULL,
 CONSTRAINT [PK_OSBLEInterventionsStatus] PRIMARY KEY CLUSTERED 
(
	[UserProfileId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]