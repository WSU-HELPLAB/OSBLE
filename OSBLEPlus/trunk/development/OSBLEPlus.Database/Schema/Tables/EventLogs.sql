CREATE TABLE [dbo].[EventLogs] (
    [Id]           INT      IDENTITY (1, 1) NOT NULL,
    [EventTypeId]  INT      NOT NULL,
    [EventDate]	   DATETIME NOT NULL,
    [DateReceived] DATETIME NOT NULL,
    [SenderId]     INT      NOT NULL,
    [CourseId]     INT,
    [SolutionName] VARCHAR(2000) NULL, 
	[IsDeleted]	   BIT,
    [EventVisibilityGroups] VARCHAR(3000) NULL, 
    [EventVisibleTo] VARCHAR(3000) NULL, 
    [IsAnonymous] BIT NULL, 
    CONSTRAINT [PK_EventLogs_Id] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [FK_EventLogs_EventTypes] FOREIGN KEY ([EventTypeId]) REFERENCES [dbo].[EventTypes] ([EventTypeId]),
    CONSTRAINT [FK_EventLogs_UserProfiles] FOREIGN KEY ([SenderId]) REFERENCES [dbo].[UserProfiles] ([ID]),
    CONSTRAINT [FK_EventLogs_AbstractCourses] FOREIGN KEY ([CourseId]) REFERENCES [dbo].[AbstractCourses] ([ID])
);
GO
ALTER TABLE [dbo].[EventLogs] ADD CONSTRAINT [DF_EventLogs] DEFAULT GETDATE() FOR [DateReceived]