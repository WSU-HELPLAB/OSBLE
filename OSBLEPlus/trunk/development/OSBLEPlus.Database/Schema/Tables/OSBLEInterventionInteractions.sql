CREATE TABLE [dbo].[OSBLEInterventionInteractions](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OSBLEInterventionId] [int] NULL,
	[UserProfileId] [int] NULL,
	[InteractionDateTime] [datetime] NOT NULL,
	[InteractionDetails] [varchar](500) NOT NULL,
	[InterventionFeedback] [varchar](5000) NULL,
	[InterventionDetailBefore] [varchar](5000) NULL,
	[InterventionDetailAfter] [varchar](5000) NULL,
	[AdditionalActionDetails] [varchar](5000) NULL,
 CONSTRAINT [PK_OSBLEInterventionInteractions] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]