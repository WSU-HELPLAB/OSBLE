PRINT 'Start updating AbstractRoles table'

IF Object_id('tempdb..#AbstractRoles') IS NOT NULL
  DROP TABLE #AbstractRoles

-- Get existing schema of table by selecting into a temp table	
SELECT TOP 0 *
INTO   [#AbstractRoles]
FROM   [dbo].[AbstractRoles]

SET IDENTITY_INSERT [#AbstractRoles] ON;

INSERT INTO [#AbstractRoles]
           ([Id]
		   ,[Name]
           ,[CanModify]
           ,[CanSeeAll]
           ,[CanGrade]
           ,[CanSubmit]
           ,[Anonymized]
           ,[CanUploadFiles]
           ,[Discriminator])
     VALUES
		    (1, N'Instructor', 1, 1, 1, 0, 0, 1, N'CourseRole')
		   ,(2, N'TA', 0, 1, 1, 0, 0, 1, N'CourseRole')
		   ,(3, N'Student', 0, 0, 0, 1, 0, 0, N'CourseRole')
		   ,(4, N'Moderator', 0, 0, 0, 0, 0, 0, N'CourseRole')
		   ,(5, N'Observer', 0, 1, 0, 0, 1, 0, N'CourseRole')
		   ,(6, N'Withdrawn', 0, 0, 0, 0, 0, 0, N'CourseRole')
		   ,(7, N'Pending', 0, 0, 0, 0, 0, 0, N'CourseRole')
		   ,(8, N'Leader', 1, 1, 1, 0, 0, 1, N'CommunityRole')
		   ,(9, N'Participant', 0, 1, 1, 0, 0, 0, N'CommunityRole')
		   ,(10, N'TrustedCommunityMember', 0, 1, 1, 0, 0, 1, N'CommunityRole')
		   ,(11, N'Pending', 0, 0, 0, 0, 0, 0, N'CommunityRole')
		   ,(12, N'Assessment Committee Chair', 1, 1, 1, 1, 0, 1, N'AssessmentCommitteeChairRole')
		   ,(13, N'Assessment Committee Member', 0, 1, 1, 1, 0, 0, N'AssessmentCommitteeMemberRole')
		   ,(14, N'ABET Evaluator', 0, 1, 0, 0, 0, 0, N'ABETEvaluatorRole')
		   ,(15, N'Admin', 1, 1, 1, 1, 1, 1, N'System')

SET IDENTITY_INSERT [#AbstractRoles] OFF;

-- Insert, Update or Delete from the target table to make it match the data in the temp table
SET IDENTITY_INSERT [dbo].[AbstractRoles] ON;

MERGE [dbo].[AbstractRoles] AS Target
USING [#AbstractRoles] AS Source
ON ( Target.[Id] = Source.[Id] )
WHEN MATCHED THEN
  UPDATE SET Target.[Name] = Source.[Name]
            ,Target.[CanModify] = Source.[CanModify]
            ,Target.[CanSeeAll] = Source.[CanSeeAll]
            ,Target.[CanGrade] = Source.[CanGrade]
            ,Target.[CanSubmit] = Source.[CanSubmit]
            ,Target.[Anonymized] = Source.[Anonymized]
            ,Target.[CanUploadFiles] = Source.[CanUploadFiles]
            ,Target.[Discriminator] = Source.[Discriminator]
WHEN NOT MATCHED BY Target THEN
  INSERT ([Id]
         ,[Name]
         ,[CanModify]
         ,[CanSeeAll]
         ,[CanGrade]
         ,[CanSubmit]
         ,[Anonymized]
         ,[CanUploadFiles]
         ,[Discriminator])
  VALUES ( Source.[Id]
          ,Source.[Name]
          ,Source.[CanModify]
          ,Source.[CanSeeAll]
          ,Source.[CanGrade]
          ,Source.[CanSubmit]
          ,Source.[Anonymized]
          ,Source.[CanUploadFiles]
          ,Source.[Discriminator] )
WHEN NOT MATCHED BY Source THEN
  DELETE;

SET IDENTITY_INSERT [dbo].[AbstractRoles] OFF;

-- Clean up
DROP TABLE [#AbstractRoles]

PRINT 'Done updating AbstractRoles table'

