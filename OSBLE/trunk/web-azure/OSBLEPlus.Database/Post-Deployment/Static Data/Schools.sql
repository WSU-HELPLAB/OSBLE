PRINT 'Start updating Schools table'

IF Object_id('tempdb..#Schools') IS NOT NULL
  DROP TABLE #Schools

-- Get existing schema of table by selecting into a temp table	
SELECT TOP 0 *
INTO   [#Schools]
FROM   [dbo].[Schools]

SET IDENTITY_INSERT [#Schools] ON;

INSERT INTO [#Schools]([ID], [Name])
VALUES (1, N'Washington State University')
	  ,(2, N'Samford University')
	  ,(3, N'Auburn University')
	  ,(4, N'Centre College')
	  ,(5, N'University of South Alabama')
	  ,(6, N'University of California-Santa Barbara')
	  ,(7, N'University of Hawaii-Hilo')
	  ,(8, N'University of Hawaii-Manoa')
	  ,(9, N'California State University San Bernadino')
	  ,(10, N'Florida A & M University')
	  ,(11, N'North Carolina State University')
	  ,(12, N'New Jersey Institute of Technology')
	  ,(13, N'Manhattan College')
	  ,(14, N'Worcester Polytechnic Institute')
	  ,(15, N'Ohio University')
	  ,(16, N'Oklahoma State University')
	  ,(17, N'University of Washington')
	  ,(18, N'Leeward Community College')
	  ,(19, N'San Francisco State University')
	  ,(20, N'University of New Mexico')
	  ,(21, N'University of North Carolina - Asheville')
	  ,(22, N'Metro State College of Denver')
	  ,(25, N'Professional')
	  ,(26, N'Southern Illinois University-Edwardsville')
	  ,(27, N'Washington State Schools for the Blind')
	  ,(28, N'Indiana Schools for the Blind and Visually Impaired')
	  ,(29, N'Texas Schools for the Blind and Visually Impaired')
	  ,(30, N'Carroll Center for the Blind')
	  ,(31, N'Tennessee Schools for the Blind')
	  ,(32, N'University of Alabama-Huntsville')
	  ,(33, N'University of Idaho')
	  ,(34, N'IT University of Copenhagen')
	  ,(35, N'Copenhagen Business Schools')
	  ,(36, N'Georgetown University')
	  ,(37, N'Grand Valley State University')
	  ,(38, N'Dordt College')
	  ,(39, N'Lancaster University')
	  ,(40, N'Saint Joseph''s University')
	  ,(41, N'New Mexico State University')
	  ,(42, N'Maryland Schools for the Blind')
	  ,(45, N'Tufts University')
	  ,(46, N'Rutgers University')
	  ,(47, N'University of Dayton')
	  ,(48, N'Syracuse University')
	  ,(49, N'University of Minnesota Duluth')
	  ,(50, N'McNeese State University')
	  ,(51, N'Louisiana State University')
	  ,(52, N'University of Applied Sciences Konstanz')
	  ,(53, N'University of Wisconsin-Oshkosh')

SET IDENTITY_INSERT [#Schools] OFF;

-- Insert, Update or Delete from the target table to make it match the data in the temp table
SET IDENTITY_INSERT [dbo].[Schools] ON;

MERGE [dbo].[Schools] AS Target
USING [#Schools] AS Source
ON ( Target.[Id] = Source.[Id] )
WHEN MATCHED THEN
  UPDATE SET Target.[Name] = Source.[Name]
WHEN NOT MATCHED BY Target THEN
  INSERT ([Id], [Name])
  VALUES ( Source.[Id],Source.[Name] )
WHEN NOT MATCHED BY Source THEN
  DELETE;

SET IDENTITY_INSERT [dbo].[Schools] OFF;

-- Clean up
DROP TABLE [#Schools]

PRINT 'Done updating Schools table'

