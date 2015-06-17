CREATE TABLE [dbo].[FeedPostUserSettings]
  (
     [ID]                  [INT] IDENTITY(1, 1) NOT NULL,
     [UserID]              [INT] NOT NULL,
     [CourseID]            [INT],
     [CourseRoleID]		   [INT],
     [EventFilterSettings] [INT],
     [SettingsDate]        [DATETIME] NOT NULL,
	 [IsActive]			   [BIT] NOT NULL,
     CONSTRAINT [PK_FeedPostUserSettings] PRIMARY KEY CLUSTERED ([ID] ASC),
     CONSTRAINT [FK_FeedPostUserSettings_UserProfiles] FOREIGN KEY ([UserID]) REFERENCES [dbo].[UserProfiles] ([ID]),
     CONSTRAINT [FK_FeedPostUserSettings_AbstractCourses] FOREIGN KEY ([CourseID]) REFERENCES [dbo].[AbstractCourses] ([ID]),
     CONSTRAINT [FK_FeedPostUserSettings_AbstractRoles] FOREIGN KEY ([CourseRoleID]) REFERENCES [dbo].[AbstractRoles] ([ID])
  ) 
