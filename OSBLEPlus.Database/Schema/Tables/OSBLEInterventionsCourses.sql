CREATE TABLE [dbo].[OSBLEInterventionsCourses](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[CourseId] [int] NOT NULL,
	[InterventionsEnabled] [bit] NOT NULL,
	[IsProgrammingCourse] [bit] NULL,
 CONSTRAINT [PK_OSBLEInterventionsCourses] PRIMARY KEY CLUSTERED 
(
	[CourseId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]