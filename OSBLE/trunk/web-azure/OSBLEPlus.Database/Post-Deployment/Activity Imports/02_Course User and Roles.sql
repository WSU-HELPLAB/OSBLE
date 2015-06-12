-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE Role values to OSBLE AbstractRoles
-------------------------------------------------------------
-------------------------------------------------------------
--Student = 1 -> 3
--TA = 2,
--Instructor = 4 -> 1
--Admin = 15
IF NOT EXISTS (SELECT 1 FROM [dbo].[AbstractRoles] WHERE Name = 'Admin')
INSERT INTO [dbo].[AbstractRoles]
            (Name, CanModify, CanSeeAll, CanGrade, CanSubmit, Anonymized, CanUploadFiles, Discriminator)
VALUES      ('Admin', 1, 1, 1, 1, 1, 1, 'System') 

-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE Courses to OSBLE AbstractCourses
-------------------------------------------------------------
-------------------------------------------------------------
IF Object_id('tempdb..#TempCourses') IS NOT NULL
  DROP TABLE #TempCourses

SELECT DISTINCT Name = SUBSTRING(c.[Description], 0, 100),
                c.IsDeleted,
                AllowDashboardPosts = 1,
                CalendarWindowOfTime = 2,
                Discriminator = 'Course',
                c.Prefix,
                Number = c.CourseNumber,
                [Description] = SUBSTRING(c.[Description], 0, 100),
                c.[Year],
                Semester = c.Season,
                RequireInstructorApprovalForEventPosting = 0
INTO	#TempCourses
FROM   [OSBIDE.HelpLab].dbo.Courses c
       LEFT JOIN [dbo].[AbstractCourses] ac
              ON ac.Prefix = c.Prefix
                 AND ac.Number = c.CourseNumber
                 AND ac.[Year] = c.[Year]
                 AND ac.Semester = c.Season
WHERE  ac.ID IS NULL 

-- Merge data into the target table, only insert when not exist in target table
MERGE [dbo].[AbstractCourses] AS Target
USING #TempCourses AS Source
ON ( Target.Name = Source.Name
     AND Target.Prefix = Source.Prefix
	 AND Target.Number = Source.Number
	 AND Target.[Year] = Source.[Year]
	 AND Target.Semester = Source.Semester )
WHEN NOT MATCHED BY Target THEN
  INSERT ( Name,
           IsDeleted,
           AllowDashboardPosts,
           CalendarWindowOfTime,
           Discriminator,
           Prefix,
           Number,
           [Description],
           [Year],
           Semester,
           RequireInstructorApprovalForEventPosting)
  VALUES ( Source.Name,
           Source.IsDeleted,
           Source.AllowDashboardPosts,
           Source.CalendarWindowOfTime,
           Source.Discriminator,
           Source.Prefix,
           Source.Number,
           Source.[Description],
           Source.[Year],
           Source.Semester,
           Source.RequireInstructorApprovalForEventPosting ); 

DROP TABLE #TempCourses;

-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE users to OSBLE UserProfiles
-------------------------------------------------------------
-------------------------------------------------------------
IF Object_id('tempdb..#TempUsers') IS NOT NULL
  DROP TABLE #TempUsers

SELECT UserName = a.Email,
       [Password] = p.[Password],
	   AuthenticationHash = 'TBD',
       IsApproved = 1,
       SchoolID = a.SchoolId,
       a.FirstName,
       a.LastName,
       Identification = Cast(a.InstitutionId AS VARCHAR(32)),
       IsAdmin = CASE WHEN a.RoleValue = 8 THEN 1 ELSE 0 END,
       CanCreateCourses = CASE WHEN a.RoleValue = 4 or a.RoleValue = 8 THEN 1 ELSE 0 END,
       DefaultCourse = a.DefaultCourseId,-- convert
       EmailAllNotifications = a.ReceiveNotificationEmails,
       EmailAllActivityPosts = a.ReceiveEmailOnNewFeedPost,
       EmailNewDiscussionPosts = a.ReceiveEmailOnNewAskForHelp
INTO   #TempUsers
FROM   [OSBIDE.HelpLab].dbo.OsbideUsers a
       INNER JOIN [OSBIDE.HelpLab].dbo.UserPasswords p
               ON p.UserId = a.Id
	   INNER JOIN
	   (
				 [OSBIDE.HelpLab].dbo.Courses c
       LEFT JOIN [dbo].[AbstractCourses] ac
              ON ac.Prefix = c.Prefix
                 AND ac.Number = c.CourseNumber
                 AND ac.[Year] = c.[Year]
                 AND ac.Semester = c.Season
	   ) ON c.Id = a.DefaultCourseId AND c.IsDeleted = 0
       LEFT JOIN UserProfiles b
              ON b.FirstName = a.FirstName
                 AND b.LastName = a.LastName
                  OR b.UserName = a.email
                  OR b.Identification = Cast(a.InstitutionID AS VARCHAR(32))
WHERE  b.ID IS NULL 

-- Merge data into the target table, only insert when not exist in target table
MERGE [dbo].[UserProfiles] AS Target
USING #TempUsers AS Source
ON ( Target.FirstName = Source.FirstName
     AND Target.LastName = Source.LastName
	 AND Target.UserName = Source.UserName
	 AND Target.Identification = Source.Identification )
WHEN NOT MATCHED BY Target THEN
  INSERT ( UserName,
           [Password],
           AuthenticationHash,
           IsApproved,
           SchoolID,
           FirstName,
           LastName,
           Identification,
           IsAdmin,
           CanCreateCourses,
           DefaultCourse,
           EmailAllNotifications,
           EmailAllActivityPosts,
           EmailNewDiscussionPosts)
  VALUES ( Source.UserName,
           Source.[Password],
           Source.AuthenticationHash,
           Source.IsApproved,
           Source.SchoolID,
           Source.FirstName,
           Source.LastName,
           Source.Identification,
           Source.IsAdmin,
           Source.CanCreateCourses,
           Source.DefaultCourse,
           Source.EmailAllNotifications,
           Source.EmailAllActivityPosts,
           Source.EmailNewDiscussionPosts ); 

DROP TABLE #TempUsers;

-------------------------------------------------------------
-------------------------------------------------------------
-- OSBIDE UserCourseRelations to OSBLE CourseUsers

-- RoleType		OSBIDE		OSBLE
-- Student		0			3
-- Assistant	1			2
-- Coordinator	2			4
-------------------------------------------------------------
-------------------------------------------------------------
IF Object_id('tempdb..#TempCourseUser') IS NOT NULL
  DROP TABLE #TempCourseUser

SELECT UserProfileID = up.ID,
       AbstractCourseID = ac.ID,
       AbstractRoleID = CASE
							WHEN a.RoleType = 0 THEN 3
							WHEN a.RoleType = 1 THEN 2
							WHEN a.RoleType = 2 THEN 3
						END,
       Section = 0,
       Hidden = 0
INTO	#TempCourseUser
FROM   [OSBIDE.HelpLab].dbo.CourseUserRelationships a
       INNER JOIN [OSBIDE.HelpLab].dbo.Courses c
               ON c.Id = a.UserId
                  AND c.IsDeleted = 0
       INNER JOIN dbo.AbstractCourses ac
               ON ac.Prefix = c.Prefix
                  AND ac.Number = c.CourseNumber
                  AND ac.[Year] = c.[Year]
                  AND ac.Semester = c.Season
       INNER JOIN [OSBIDE.HelpLab].dbo.OsbideUsers u
               ON u.Id = a.UserId
       INNER JOIN dbo.UserProfiles up
               ON up.FirstName = u.FirstName
                  AND up.LastName = u.LastName
                   OR up.UserName = u.email
                   OR up.Identification = Cast(u.InstitutionID AS VARCHAR(32))
WHERE  a.IsActive = 1 AND a.IsApproved = 1

-- Merge data into the target table, only insert when not exist in target table
MERGE [dbo].[CourseUsers] AS Target
USING #TempCourseUser AS Source
ON ( Target.UserProfileID = Source.UserProfileID
     AND Target.AbstractCourseID = Source.AbstractCourseID )
WHEN NOT MATCHED BY Target THEN
  INSERT ( UserProfileID,
           AbstractCourseID,
           AbstractRoleID,
           Section,
           Hidden )
  VALUES ( Source.UserProfileID,
           Source.AbstractCourseID,
           Source.AbstractRoleID,
           Source.Section,
           Source.Hidden ); 

DROP TABLE #TempCourseUser;
