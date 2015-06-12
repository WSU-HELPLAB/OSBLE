
-- Merge data into UserProfiles 
IF OBJECT_ID('tempdb..#UserProfiles') IS NOT NULL
    DROP TABLE #UserProfiles

-- Get existing table schema into a temp table	
SELECT TOP 0 * INTO [#UserProfiles] FROM [dbo].[UserProfiles]

SET IDENTITY_INSERT [#UserProfiles] ON;

INSERT INTO [#UserProfiles]
           (ID
		   ,[UserName]
           ,[Password]
           ,[AuthenticationHash]
           ,[IsApproved]
           ,[SchoolID]
           ,[FirstName]
           ,[LastName]
           ,[Identification]
           ,[IsAdmin]
           ,[CanCreateCourses]
           ,[DefaultCourse]
           ,[EmailAllNotifications]
           ,[EmailAllActivityPosts]
           ,[EmailNewDiscussionPosts])
     VALUES
		    (1, N'bob@smith.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Bob', N'Smith', N'1', 1, 1, 0, 0, 0, 0)
		   ,(2, N'stu@dent.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Stu', N'Dent', N'2', 0, 0, 0, 0, 0, 0)
		   ,(3, N'me@me.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Ad', N'Min', N'3', 1, 1, 0, 0, 0, 0)
		   ,(4, N'John@Morgan.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'John', N'Morgan', N'4', 0, 0, 0, 0, 0, 0)
		   ,(5, N'Margaret@Bailey.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Margaret', N'Bailey', N'5', 0, 0, 0, 0, 0, 0)
		   ,(6, N'Carol@Jackson.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Carol', N'Jackson', N'6', 0, 0, 0, 0, 0, 0)
		   ,(7, N'Donald@Robinson.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Donald', N'Robinson', N'7', 0, 0, 0, 0, 0, 0)
		   ,(8, N'Paul@Sanders.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Paul', N'Sanders', N'8', 0, 0, 0, 0, 0, 0)
		   ,(9, N'Anthony@Stewart.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Anthony', N'Stewart', N'9', 0, 0, 0, 0, 0, 0)
		   ,(10, N'Paul@Harris.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Paul', N'Harris', N'10', 0, 0, 0, 0, 0, 0)
		   ,(11, N'Donald@White.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Donald', N'White', N'12', 0, 0, 0, 0, 0, 0)
		   ,(12, N'Christopher@Sanders.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Christopher', N'Sanders', N'13', 0, 0, 0, 0, 0, 0)
		   ,(13, N'Robert@Wright.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Robert', N'Wright', N'14', 0, 0, 0, 0, 0, 0)
		   ,(14, N'Betty@Rogers.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Betty', N'Rogers', N'15', 0, 0, 0, 0, 0, 0)
		   ,(15, N'Nancy@Russell.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Nancy', N'Russell', N'16', 0, 0, 0, 0, 0, 0)
		   ,(16, N'Jason@Robinson.com', N'`�f~��;�W(5�?x' /*123123*/, N'', 1, 1, N'Jason', N'Robinson', N'17', 0, 0, 0, 0, 0, 0)
		   ,(17, N'user1@seed.com', N't6����/,��K�Cݺ' /*abc123$%*/, N'EMNHDFTFZMVSKJWSKVJXBLJECNUCKYZWLRIKUWOZ', 1, 1, N'User1', N'Seed', N'111111111', 1, 1, 1, 1,1,1)
		   ,(18, N'user2@seed.com', N't6����/,��K�Cݺ' /*abc123$%*/, N'YGPVIJDPETCIJMDLSALZYYLENQMIAZAPCVVUWIED', 1, 1, N'User2', N'Seed', N'222222222', 1, 1, 1, 1,1,1)
		   ,(19, N'user3@seed.com', N't6����/,��K�Cݺ' /*abc123$%*/, N'SFHQIAPZKLVDKKMTSLFYUBEWVCJPRZAIIGFAYGMY', 1, 1, N'User3', N'Seed', N'333333333', 1, 1, 1, 1,1,1)

SET IDENTITY_INSERT [#UserProfiles] OFF;

-- Insert, Update or Delete from the target table to make it match the data in the temp table
SET IDENTITY_INSERT [dbo].[UserProfiles] ON;

MERGE [dbo].[UserProfiles] AS Target
USING [#UserProfiles] AS Source ON (Target.[ID] = Source.[ID])
	WHEN MATCHED
	THEN
		UPDATE SET
		  Target.[UserName] = Source.[UserName]
         ,Target.[Password] = Source.[Password]
         ,Target.[AuthenticationHash] = Source.[AuthenticationHash]
         ,Target.[IsApproved] = Source.[IsApproved]
         ,Target.[SchoolID] = Source.[SchoolID]
         ,Target.[FirstName] = Source.[FirstName]
         ,Target.[LastName] = Source.[LastName]
         ,Target.[Identification] = Source.[Identification]
         ,Target.[IsAdmin] = Source.[IsAdmin]
         ,Target.[CanCreateCourses] = Source.[CanCreateCourses]
         ,Target.[DefaultCourse] = Source.[DefaultCourse]
         ,Target.[EmailAllNotifications] = Source.[EmailAllNotifications]
         ,Target.[EmailAllActivityPosts] = Source.[EmailAllActivityPosts]
         ,Target.[EmailNewDiscussionPosts] = Source.[EmailNewDiscussionPosts]

	WHEN NOT MATCHED BY Target
	THEN
	INSERT (ID
		   ,[UserName]
           ,[Password]
           ,[AuthenticationHash]
           ,[IsApproved]
           ,[SchoolID]
           ,[FirstName]
           ,[LastName]
           ,[Identification]
           ,[IsAdmin]
           ,[CanCreateCourses]
           ,[DefaultCourse]
           ,[EmailAllNotifications]
           ,[EmailAllActivityPosts]
           ,[EmailNewDiscussionPosts])
	VALUES
	(
		  Source.[ID]
		 ,Source.[UserName]
         ,Source.[Password]
         ,Source.[AuthenticationHash]
         ,Source.[IsApproved]
         ,Source.[SchoolID]
         ,Source.[FirstName]
         ,Source.[LastName]
         ,Source.[Identification]
         ,Source.[IsAdmin]
         ,Source.[CanCreateCourses]
         ,Source.[DefaultCourse]
         ,Source.[EmailAllNotifications]
         ,Source.[EmailAllActivityPosts]
         ,Source.[EmailNewDiscussionPosts]
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;

SET IDENTITY_INSERT [dbo].[UserProfiles] OFF;
		
-- Clean up
DROP TABLE [#UserProfiles]

PRINT 'Done updating UserProfiles table'