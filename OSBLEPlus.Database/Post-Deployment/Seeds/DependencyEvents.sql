---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- AskForHelpEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[AskForHelpEvents])
BEGIN
	INSERT INTO [dbo].[AskForHelpEvents]
			   ([EventLogId]
			   ,[EventDate]
			   ,[SolutionName]
			   ,[Code]
			   ,[UserComment])
	SELECT l.Id, l.EventDate, '', '', ''
	FROM [dbo].[EventLogs] l
	WHERE l.EventTypeId=1
END

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- BuildEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[BuildEvents])
BEGIN
	INSERT INTO [dbo].[BuildEvents]
			   ([EventLogId]
			   ,[EventDate]
			   ,[SolutionName])
	SELECT l.Id, l.EventDate, ''
	FROM [dbo].[EventLogs] l
	WHERE l.EventTypeId=2
END

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- CutCopyPasteEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[CutCopyPasteEvents])
BEGIN
	INSERT INTO [dbo].[CutCopyPasteEvents]
           ([EventLogId]
           ,[EventDate]
           ,[SolutionName]
           ,[EventAction]
           ,[DocumentName]
           ,[Content])
	SELECT l.Id, l.EventDate, '', (ROW_NUMBER() OVER(ORDER By l.Id) % 3), '', ''
	FROM [dbo].[EventLogs] l
	WHERE l.EventTypeId=3
END

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- DebugEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[DebugEvents])
BEGIN
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
END

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- EditorActivityEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[EditorActivityEvents])
BEGIN
	INSERT INTO [dbo].[EditorActivityEvents]
			   ([EventLogId]
			   ,[EventDate]
			   ,[SolutionName])
	SELECT l.Id, l.EventDate, ''
	FROM [dbo].[EventLogs] l
	WHERE l.EventTypeId=5
END

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- ExceptionEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[ExceptionEvents])
BEGIN
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
END

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- FeedPostEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[FeedPostEvents])
BEGIN
	INSERT INTO [dbo].[FeedPostEvents]
			   ([EventLogId]
			   ,[EventDate]
			   ,[SolutionName]
			   ,[Comment])
	SELECT l.Id, l.EventDate, '',''
	FROM [dbo].[EventLogs] l
	WHERE l.EventTypeId=7
END

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- HelpfulMarkGivenEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[HelpfulMarkGivenEvents])
BEGIN
	INSERT INTO [dbo].[HelpfulMarkGivenEvents]
           ([EventLogId]
           ,[LogCommentEventId]
           ,[EventDate]
           ,[SolutionName])
	SELECT l.Id, c.Id, l.EventDate, ''
	FROM [dbo].[EventLogs] l
	CROSS APPLY [dbo].[LogCommentEvents] c
	WHERE l.EventTypeId=8 AND c.Id BETWEEN CAST(LEFT(l.Id, 1) AS INT) AND CAST(RIGHT(l.Id, 1) AS INT)
END

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- LogCommentEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[LogCommentEvents])
BEGIN
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
END

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- SaveEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[SaveEvents])
BEGIN
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
END

---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
-- SaveEvents
---------------------------------------------------------------------------------
---------------------------------------------------------------------------------
IF NOT EXISTS(SELECT 1 FROM [dbo].[SaveEvents])
BEGIN
	DECLARE @fakeAssignment INT

	INSERT INTO [dbo].[Assignments]
			   ([AssignmentTypeID]
			   ,[AssignmentName]
			   ,[AssignmentDescription]
			   ,[ReleaseDate]
			   ,[DueDate]
			   ,[IsAnnotatable]
			   ,[HoursLateWindow]
			   ,[DeductionPerUnit]
			   ,[HoursPerDeduction]
			   ,[IsDraft])
		 VALUES
			   (1, 'Dummy Assignment', 'Dummy', GetDate(), GetDate() + 10, 1, 1, 1,1,0)

	SELECT @fakeAssignment=SCOPE_IDENTITY()

	INSERT INTO [dbo].[SubmitEvents]
			   ([EventLogId]
			   ,[EventDate]
			   ,[SolutionName]
			   ,[AssignmentId])
	SELECT l.Id, l.EventDate, '', @fakeAssignment
	FROM [dbo].[EventLogs] l
	WHERE l.EventTypeId=11
END



