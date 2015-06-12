-- Merge data into CourseUsers 
IF OBJECT_ID('tempdb..#CourseUsers') IS NOT NULL
    DROP TABLE #CourseUsers

-- Get existing table schema into a temp table	
SELECT TOP 0 * INTO [#CourseUsers] FROM [dbo].[CourseUsers]

SET IDENTITY_INSERT [#CourseUsers] ON;

INSERT INTO [#CourseUsers]
           ([ID]
		   ,[UserProfileID]
           ,[AbstractCourseID]
           ,[AbstractRoleID]
           ,[Section]
           ,[Hidden])
		VALUES
		    (1, 4, 4, 3, 0, 0)
		   ,(2, 5, 4, 3, 0, 0)
		   ,(3, 1, 4, 1, 0, 0)
		   ,(4, 6, 4, 3, 0, 0)
		   ,(5, 7, 4, 3, 0, 0)
		   ,(6, 8, 4, 3, 0, 0)
		   ,(7, 9, 4, 3, 0, 0)
		   ,(8, 10, 4, 3, 0, 0)
		   ,(9, 11, 4, 3, 0, 0)
		   ,(10, 12, 4, 3, 0, 0)
		   ,(11, 13, 4, 3, 0, 0)
		   ,(12, 14, 4, 3, 0, 0)
		   ,(13, 15, 4, 3, 0, 0)
		   ,(14, 16, 4, 3, 0, 0)
		   ,(15, 1, 1, 1, 0, 0)
		   ,(16, 1, 2, 5, 0, 0)
		   ,(17, 2, 1, 3, 1, 0)
		   ,(18, 3, 2, 1, 0, 0)
		   ,(19, 2, 2, 3, 2, 0)
		   ,(20, 1, 3, 8, 0, 0)
		   ,(21, 2, 3, 9, 0, 0)
		   ,(22, 17, 1, 1, 0, 0)
		   ,(23, 18, 1, 1, 0, 0)
		   ,(24, 19, 1, 1, 0, 0)

SET IDENTITY_INSERT [#CourseUsers] OFF;

-- Insert or Update the target table to make it match the data in the temp table
SET IDENTITY_INSERT [dbo].[CourseUsers] ON;

MERGE [dbo].[CourseUsers] AS Target
USING [#CourseUsers] AS Source ON (Target.[ID] = Source.[ID])
	WHEN MATCHED
	THEN
		UPDATE SET
		  Target.[UserProfileID] = Source.[UserProfileID]
         ,Target.[AbstractCourseID] = Source.[AbstractCourseID]
         ,Target.[AbstractRoleID] = Source.[AbstractRoleID]
         ,Target.[Section] = Source.[Section]
         ,Target.[Hidden] = Source.[Hidden]

	WHEN NOT MATCHED BY Target
	THEN
	INSERT ([ID]
		   ,[UserProfileID]
           ,[AbstractCourseID]
           ,[AbstractRoleID]
           ,[Section]
           ,[Hidden])
	VALUES
	(
		    Source.[ID]
		   ,Source.[UserProfileID]
           ,Source.[AbstractCourseID]
           ,Source.[AbstractRoleID]
           ,Source.[Section]
           ,Source.[Hidden]
	);

SET IDENTITY_INSERT [dbo].[CourseUsers] OFF;
		
-- Clean up
DROP TABLE [#CourseUsers]

PRINT 'Done updating CourseUsers table'
