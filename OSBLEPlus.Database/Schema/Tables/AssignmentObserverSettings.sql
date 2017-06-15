CREATE TABLE [dbo].[AssignmentObserverSettings](
	[AssignmentID] [int] NOT NULL,
	[IsObservable] [bit] NOT NULL,
 CONSTRAINT [PK_AssignmentObserverSettings] PRIMARY KEY CLUSTERED 
(
	[AssignmentID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]