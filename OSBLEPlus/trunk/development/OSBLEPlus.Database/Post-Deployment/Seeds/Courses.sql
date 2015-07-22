
-- Merge data into AbstractCourses 
IF OBJECT_ID('tempdb..#AbstractCourses') IS NOT NULL
    DROP TABLE #AbstractCourses

-- Get existing table schema into a temp table	
SELECT TOP 0 * INTO [#AbstractCourses] FROM [dbo].[AbstractCourses]

SET IDENTITY_INSERT [#AbstractCourses] ON;

INSERT INTO [#AbstractCourses]
           (ID
		   ,[Name]
           ,[IsDeleted]
           ,[AllowDashboardPosts]
           ,[CalendarWindowOfTime]
           ,[Prefix]
           ,[Number]
           ,[Semester]
           ,[Year]
           ,[AllowDashboardReplies]
           ,[AllowEventPosting]
           ,[RequireInstructorApprovalForEventPosting]
           ,[Inactive]
           ,[StartDate]
           ,[EndDate]
           ,[MinutesLateWithNoPenalty]
           ,[PercentPenalty]
           ,[HoursLatePerPercentPenalty]
           ,[HoursLateUntilZero]
           ,[TimeZoneOffset]
           ,[ShowMeetings]
           ,[Description]
           ,[StartDate1]
           ,[Description1]
           ,[Nickname]
           ,[Discriminator])
VALUES
 (1, N'Advanced data structures', 0, 1, 2, N'CptS', N'223', N'Spring', N'2014', 1, 1, 0, 0, CAST(N'2010-12-18' AS DateTime), CAST(N'2026-12-18' AS DateTime), 5, 10, 24, 48, -8, 1, N'Advanced data structures, object oriented programming concepts, concurrency, and program design pri', NULL, NULL, NULL, N'Course')
,(2, N'Data Structures', 0, 1, 2, N'CptS', N'122', N'Summer', N'2014', 1, 1, 0, 0, CAST(N'2010-12-18' AS DateTime), CAST(N'2026-12-18' AS DateTime), 5, 10, 24, 48, -8, 1, N'Data Structures', NULL, NULL, NULL, N'Course')
,(3, N'OSBIDE', 0, 1, 2, N'OSBIDE', N'101', N'Spring', N'2014', 1, 1, 0, 0, CAST(N'2010-12-18' AS DateTime), CAST(N'2026-12-18' AS DateTime), 5, 10, 24, 48, -8, 1, N'Everything you ever wanted to know about OSBIDE.', NULL, NULL, NULL, N'Course')
,(4, N'Modern Structured Language', 0, 1, 2, N'CptS', N'121', N'Spring', N'2014', 1, 1, 0, 0, CAST(N'2010-12-18' AS DateTime), CAST(N'2026-12-18' AS DateTime), 5, 10, 24, 48, -8, 1, N'Formulation of problems and top-down design of programs in a modern structured language for their s', NULL, NULL, NULL, N'Course')
,(5, N'Advanced Programming Techniques', 0, 1, 2, N'CptS', N'122', N'Spring', N'2014', 1, 1, 0, 0, CAST(N'2010-12-18' AS DateTime), CAST(N'2026-12-18' AS DateTime), 5, 10, 24, 48, -8, 1, N'This course is about advanced programming techniques, data structures, recursion, sorting, searchin', NULL, NULL, NULL, N'Course')
,(6, N'Web development', 0, 1, 2, N'CptS', N'483', N'Spring', N'2014', 1, 1, 0, 0, CAST(N'2010-12-18' AS DateTime), CAST(N'2026-12-18' AS DateTime), 5, 10, 24, 48, -8, 1, N'Web development', NULL, NULL, NULL, N'Course')
,(7, N'Introduction to OSBIDE', 0, 1, 2, N'OSBIDE', N'101', N'Summer', N'2013', 1, 1, 0, 0, CAST(N'2013-05-29' AS DateTime), CAST(N'2013-09-18' AS DateTime), 5, 10, 24, 48, -8, 1, NULL, NULL, NULL, NULL, N'Course')
,(8, N'Introduction to OSBLE', 0, 1, 2, N'OSBLE', N'101', N'Spring', N'2013', 1, 1, 0, 0, CAST(N'2013-01-29' AS DateTime), CAST(N'2013-05-18' AS DateTime), 5, 10, 24, 48, -8, 1, NULL, NULL, NULL, NULL, N'Course')
,(9, N'Design Patterns', 0, 1, 2, N'CS', N'200', N'Fall', N'2013', 1, 1, 0, 0, CAST(N'2010-12-18' AS DateTime), CAST(N'2026-12-18' AS DateTime), 5, 10, 24, 48, -8, 1, NULL, NULL, NULL, NULL, N'Course')

SET IDENTITY_INSERT [#AbstractCourses] OFF;

-- Insert, Update or Delete from the target table to make it match the data in the temp table
SET IDENTITY_INSERT [dbo].[AbstractCourses] ON;

MERGE [dbo].[AbstractCourses] AS Target
USING [#AbstractCourses] AS Source ON (Target.[ID] = Source.[ID])
	WHEN MATCHED
	THEN
		UPDATE SET
		 Target.[Name] = Source.[Name]
        ,Target.[IsDeleted] = Source.[IsDeleted]
        ,Target.[AllowDashboardPosts] = Source.[AllowDashboardPosts]
        ,Target.[CalendarWindowOfTime] = Source.[CalendarWindowOfTime]
        ,Target.[Prefix] = Source.[Prefix]
        ,Target.[Number] = Source.[Number]
        ,Target.[Semester] = Source.[Semester]
        ,Target.[Year] = Source.[Year]
        ,Target.[AllowDashboardReplies] = Source.[AllowDashboardReplies]
        ,Target.[AllowEventPosting] = Source.[AllowEventPosting]
        ,Target.[RequireInstructorApprovalForEventPosting] = Source.[RequireInstructorApprovalForEventPosting]
        ,Target.[Inactive] = Source.[Inactive]
        ,Target.[StartDate] = Source.[StartDate]
        ,Target.[EndDate] = Source.[EndDate]
        ,Target.[MinutesLateWithNoPenalty] = Source.[MinutesLateWithNoPenalty]
        ,Target.[PercentPenalty] = Source.[PercentPenalty]
        ,Target.[HoursLatePerPercentPenalty] = Source.[HoursLatePerPercentPenalty]
        ,Target.[HoursLateUntilZero] = Source.[HoursLateUntilZero]
        ,Target.[TimeZoneOffset] = Source.[TimeZoneOffset]
        ,Target.[ShowMeetings] = Source.[ShowMeetings]
        ,Target.[Description] = Source.[Description]
        ,Target.[StartDate1] = Source.[StartDate1]
        ,Target.[Description1] = Source.[Description1]
        ,Target.[Nickname] = Source.[Nickname]
        ,Target.[Discriminator] = Source.[Discriminator]

	WHEN NOT MATCHED BY Target
	THEN
	INSERT (ID
		   ,[Name]
           ,[IsDeleted]
           ,[AllowDashboardPosts]
           ,[CalendarWindowOfTime]
           ,[Prefix]
           ,[Number]
           ,[Semester]
           ,[Year]
           ,[AllowDashboardReplies]
           ,[AllowEventPosting]
           ,[RequireInstructorApprovalForEventPosting]
           ,[Inactive]
           ,[StartDate]
           ,[EndDate]
           ,[MinutesLateWithNoPenalty]
           ,[PercentPenalty]
           ,[HoursLatePerPercentPenalty]
           ,[HoursLateUntilZero]
           ,[TimeZoneOffset]
           ,[ShowMeetings]
           ,[Description]
           ,[StartDate1]
           ,[Description1]
           ,[Nickname]
           ,[Discriminator])
	VALUES
	(
		 Source.[ID]
		,Source.[Name]
        ,Source.[IsDeleted]
        ,Source.[AllowDashboardPosts]
        ,Source.[CalendarWindowOfTime]
        ,Source.[Prefix]
        ,Source.[Number]
        ,Source.[Semester]
        ,Source.[Year]
        ,Source.[AllowDashboardReplies]
        ,Source.[AllowEventPosting]
        ,Source.[RequireInstructorApprovalForEventPosting]
        ,Source.[Inactive]
        ,Source.[StartDate]
        ,Source.[EndDate]
        ,Source.[MinutesLateWithNoPenalty]
        ,Source.[PercentPenalty]
        ,Source.[HoursLatePerPercentPenalty]
        ,Source.[HoursLateUntilZero]
        ,Source.[TimeZoneOffset]
        ,Source.[ShowMeetings]
        ,Source.[Description]
        ,Source.[StartDate1]
        ,Source.[Description1]
        ,Source.[Nickname]
        ,Source.[Discriminator]
	)
	WHEN NOT MATCHED BY Source
	THEN
		DELETE;

SET IDENTITY_INSERT [dbo].[AbstractCourses] OFF;
		
-- Clean up
DROP TABLE [#AbstractCourses]

PRINT 'Done updating AbstractCourses table'