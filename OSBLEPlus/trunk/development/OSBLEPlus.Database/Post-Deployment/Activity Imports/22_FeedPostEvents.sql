-- all scripts are re-runnable !!
-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE FeedPostEvents
-------------------------------------------------------------
-------------------------------------------------------------
-- Create the physical FeedPostEvents table if not exits
IF NOT EXISTS(SELECT 1 FROM sys.tables WHERE name='FeedPostEvents')
BEGIN

	CREATE TABLE FeedPostEvents
	  (
		 Id				INT IDENTITY NOT NULL,
		 EventLogId		INT NOT NULL,
		 EventDate		DATETIME NOT NULL,
		 SolutionName	VARCHAR(MAX) NOT NULL,
		 Comment		VARCHAR(MAX) NOT NULL,
		 CONSTRAINT PK_FeedPostEvents PRIMARY KEY CLUSTERED (Id),
		 CONSTRAINT FK_FeedPostEvents_EventLogs FOREIGN KEY (EventLogId) REFERENCES EventLogs(Id),
	  )
END

-- Select the targe data into a temp table 
IF OBJECT_ID('tempdb..#FeedPostEvents') IS NOT NULL
    DROP TABLE #FeedPostEvents

SELECT	a.Id,
		a.EventLogId,
		a.EventDate,
		a.SolutionName,
		a.Comment
INTO #FeedPostEvents
FROM [OSBIDE.Helplab].dbo.FeedPostEvents a
INNER JOIN dbo.EventLogs b ON b.Id = a.EventLogId

-- Merge data into the target table, only insert or delete records
SET IDENTITY_INSERT [dbo].[FeedPostEvents] ON
MERGE [dbo].[FeedPostEvents] AS Target
USING [#FeedPostEvents] AS Source ON (Target.Id = Source.Id)
	WHEN NOT MATCHED BY Target
	THEN
	INSERT (Id, EventLogId, EventDate, SolutionName, Comment)
	VALUES
	(
		Source.Id,
		Source.EventLogId,
		Source.EventDate,
		Source.SolutionName,
		Source.Comment
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;
SET IDENTITY_INSERT [dbo].[FeedPostEvents] OFF


-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE UserFeedSettings to OSBLE FeedPostUserSettings
-------------------------------------------------------------
-------------------------------------------------------------
IF Object_id('tempdb..#FeedPostUserSettings') IS NOT NULL
  DROP TABLE #FeedPostUserSettings

SELECT [UserID] = au.ID,
       [CourseID] = CASE
                      WHEN ac.[ID] < 1 THEN NULL
                      ELSE ac.[ID]
                    END,
       [CourseRoleID]= CASE
                         WHEN a.CourseRoleFilter = 0 THEN 3
                         WHEN a.CourseRoleFilter = 1 THEN 2
                         WHEN a.CourseRoleFilter = 2 THEN 4
                       END,
       a.[EventFilterSettings],
       a.[SettingsDate],
       [IsActive]=Cast(0 AS BIT)
INTO   #FeedPostUserSettings
FROM   [OSBIDE.HelpLab].dbo.UserFeedSettings a
       INNER JOIN [OSBIDE.HelpLab].dbo.OsbideUsers u
               ON u.Id = a.UserId
       INNER JOIN [dbo].[UserProfiles] au
               ON au.FirstName = u.FirstName
                  AND au.LastName = u.LastName
                  AND au.UserName = u.Email
                  AND au.Identification = Cast(u.institutionId AS VARCHAR(32))
       INNER JOIN [OSBIDE.HelpLab].dbo.Courses c
               ON c.Id = a.CourseFilter
       INNER JOIN [dbo].[AbstractCourses] ac
               ON ac.Prefix = c.Prefix
                  AND ac.Number = c.CourseNumber
                  AND ac.[Year] = c.[Year]
                  AND ac.Semester = c.Season

UPDATE fs
SET    IsActive = 1
FROM   #FeedPostUserSettings fs
       INNER JOIN (SELECT UserId,
                          SettingsDate=Max(settingsDate)
                   FROM   #FeedPostUserSettings
                   GROUP  BY UserId) a
               ON a.UserId = fs.UserId
                  AND a.SettingsDate = fs.SettingsDate

-- Merge data into the target table, only insert when not exist in target table
MERGE [dbo].[FeedPostUserSettings] AS Target
USING #FeedPostUserSettings AS Source
ON ( Target.UserID = Source.UserID
     AND Target.CourseID = Source.CourseID
     AND Target.CourseRoleID = Source.CourseRoleID
     AND Target.EventFilterSettings = Source.EventFilterSettings
     AND Target.SettingsDate = Source.SettingsDate
     AND Target.IsActive = Source.IsActive )
WHEN NOT MATCHED BY Target THEN
  INSERT ([UserID],
          [CourseID],
          [CourseRoleID],
          [EventFilterSettings],
          [SettingsDate],
          [IsActive])
  VALUES (Source.UserID,
          Source.CourseID,
          Source.CourseRoleID,
          Source.EventFilterSettings,
          Source.SettingsDate,
          Source.IsActive);

DROP TABLE #FeedPostUserSettings 

