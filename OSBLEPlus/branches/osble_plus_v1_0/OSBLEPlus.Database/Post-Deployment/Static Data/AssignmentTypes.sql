IF NOT EXISTS(SELECT 1
              FROM   sys.tables
              WHERE  NAME = 'AssignmentTypes')
  BEGIN
      CREATE TABLE AssignmentTypes
        (
           Id            INT IDENTITY(1, 1) NOT NULL,
           NAME          VARCHAR(100) NOT NULL,
           [Description] VARCHAR(200),
           CONSTRAINT PK_AssignmentTypes PRIMARY KEY(Id)
        )
  END

-- 2. Populate AssignmentTypes static data
PRINT 'Start updating AssignmentTypes table'

IF Object_id('tempdb..#AssignmentTypes') IS NOT NULL
  DROP TABLE #AssignmentTypes

-- Get existing schema of table by selecting into a temp table	
SELECT TOP 0 *
INTO   [#AssignmentTypes]
FROM   [dbo].[AssignmentTypes]

SET IDENTITY_INSERT [#AssignmentTypes] ON

INSERT INTO [#AssignmentTypes]
            ([Id],
             [Name],
             [Description])
VALUES      (1,
             'Basic',
             'Basic'),
            (2,
             'CriticalReview',
             'CriticalReview'),
            (3,
             'DiscussionAssignment',
             'DiscussionAssignment'),
            (4,
             'TeamEvaluation',
             'TeamEvaluation'),
            (5,
             'CriticalReviewDiscussion',
             'CriticalReviewDiscussion'),
            (6,
             'CommitteeDiscussion',
             'CommitteeDiscussion'),
            (7,
             'ReviewOfStudentWork',
             'ReviewOfStudentWork'),
            (8,
             'CommitteeReview',
             'CommitteeReview'),
            (9,
             'AggregateAssessment',
             'AggregateAssessment'),
            (10,
             'AnchoredDiscussion',
             'AnchoredDiscussion')

SET IDENTITY_INSERT [#AssignmentTypes] OFF


-- Insert, Update or Delete from the target table to make it match the data in the temp table
SET IDENTITY_INSERT [dbo].[AssignmentTypes] ON

MERGE [dbo].[AssignmentTypes] AS Target
USING [#AssignmentTypes] AS Source
ON ( Target.[Id] = Source.[Id] )
WHEN MATCHED THEN
  UPDATE SET Target.[Name] = Source.[Name],
             Target.[Description] = Source.[Description]
WHEN NOT MATCHED BY Target THEN
  INSERT ([Id],
          [Name],
          [Description])
  VALUES ( Source.[Id],
           Source.[Name],
           Source.[Description] )
WHEN NOT MATCHED BY Source THEN
  DELETE;

SET IDENTITY_INSERT [dbo].[AssignmentTypes] OFF

-- Clean up
DROP TABLE [#AssignmentTypes]

PRINT 'Done updating AssignmentTypes table'
