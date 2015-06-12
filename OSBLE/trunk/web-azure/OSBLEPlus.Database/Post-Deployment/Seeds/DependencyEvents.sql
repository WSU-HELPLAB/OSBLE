---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- AskForHelpEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
INSERT INTO [dbo].[AskForHelpEvents]
           ([EventLogId]
           ,[EventDate]
           ,[SolutionName]
           ,[Code]
           ,[UserComment])
SELECT l.Id, l.EventDate, '', '', ''
FROM [dbo].[EventLogs] l
WHERE l.EventTypeId=1

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- BuildEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
INSERT INTO [dbo].[BuildEvents]
           ([EventLogId]
           ,[EventDate]
           ,[SolutionName])
SELECT l.Id, l.EventDate, ''
FROM [dbo].[EventLogs] l
WHERE l.EventTypeId=2

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- DebugEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
INSERT INTO [dbo].[DebugEvents]
           ([EventLogId]
           ,[EventDate]
           ,[SolutionName]
		   ,[ExecutionAction]
		   ,[DocumentName]
		   ,[LineNumber]
		   ,[DebugOutput])
SELECT l.Id, l.EventDate, '',(ROW_NUMBER() OVER(ORDER By l.Id) % 7),'',(ROW_NUMBER() OVER(ORDER By l.Id)%200),''
FROM [dbo].[EventLogs] l
WHERE l.EventTypeId=4

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- ExceptionEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
INSERT INTO [dbo].[ExceptionEvents]
           ([EventLogId]
           ,[EventDate]
           ,[SolutionName]
		   ,[ExceptionType]
		   ,[ExceptionName]
		   ,[ExceptionCode]
		   ,[ExceptionDescription]
		   ,[ExceptionAction]
		   ,[DocumentName]
		   ,[LineNumber]
		   ,[LineContent])
SELECT l.Id, l.EventDate, '','','',CHECKSUM(NewId()) % 300,'',1,'',(ROW_NUMBER() OVER(ORDER By l.Id)%200),''
FROM [dbo].[EventLogs] l
WHERE l.EventTypeId=6

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- FeedPostEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
INSERT INTO [dbo].[FeedPostEvents]
           ([EventLogId]
           ,[EventDate]
           ,[SolutionName]
		   ,[Comment])
SELECT l.Id, l.EventDate, '',''
FROM [dbo].[EventLogs] l
WHERE l.EventTypeId=7

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- LogCommentEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
INSERT INTO [dbo].[LogCommentEvents]
           ([EventLogId]
		   ,[SourceEventLogId]
           ,[EventDate]
           ,[SolutionName]
		   ,[Content])
SELECT l.Id, c.Id, l.EventDate, '',''
FROM [dbo].[EventLogs] l
INNER JOIN [dbo].[EventLogs] c ON c.Id+100=l.Id
WHERE l.EventTypeId=9

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- SaveEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
DECLARE @fakeDoc INT
INSERT INTO CodeDocuments ([FileName], Content) VALUES ('Test', '')
SELECT @fakeDoc=SCOPE_IDENTITY()

INSERT INTO [dbo].[SaveEvents]
           ([EventLogId]
           ,[EventDate]
           ,[SolutionName]
		   ,[DocumentId])
SELECT l.Id, l.EventDate, '', @fakeDoc
FROM [dbo].[EventLogs] l
INNER JOIN [dbo].[EventLogs] c ON c.Id+100=l.Id
WHERE l.EventTypeId=10




