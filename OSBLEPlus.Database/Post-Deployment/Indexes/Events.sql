CREATE NONCLUSTERED INDEX IX_EventTypes_Feed
  ON [dbo].[EventTypes] ([IsFeedEvent])
  INCLUDE ([EventTypeId])

GO

;
CREATE NONCLUSTERED INDEX IX_EventLogs_DataReceived
  ON [dbo].[EventLogs] ([DateReceived])
  INCLUDE ([Id], [EventTypeId], [SenderId])

GO

;CREATE NONCLUSTERED INDEX IX_EventLogs_CreatedDate_Sender_EventType_Date
  ON [dbo].[EventLogs] ([SenderId], [EventTypeId], [EventDate])
  INCLUDE ([Id])

GO

;
CREATE NONCLUSTERED INDEX IX_EventLogs_EventTypeId_Date
  ON [dbo].[EventLogs] ([EventTypeId], [EventDate])
  INCLUDE ([Id], [SenderId])

GO

;
CREATE NONCLUSTERED INDEX IX_LogCommentEvents_EventLogId_SourceEventLogId
  ON [dbo].[LogCommentEvents] ([EventLogId])
  INCLUDE ([SourceEventLogId])

GO

;
CREATE NONCLUSTERED INDEX IX_LogCommentEvents_SourceEventLogId_EventLogId
  ON [dbo].[LogCommentEvents] ([SourceEventLogId])
  INCLUDE ([EventLogId])

GO

;
CREATE NONCLUSTERED INDEX IX_LogCommentEvents_EventLogId
  ON [dbo].[LogCommentEvents] ([SourceEventLogId])
  INCLUDE ([Id], [EventLogId], [EventDate], [SolutionName], [Content])

GO

;
CREATE NONCLUSTERED INDEX IX_LogCommentEvents_EventLogId_Source
  ON [dbo].[LogCommentEvents] ([EventLogId])
  INCLUDE ([Id], [SourceEventLogId], [EventDate], [SolutionName], [Content])

GO

;
CREATE NONCLUSTERED INDEX IX_BuildEvents_EventLogId
  ON [dbo].[BuildEvents] ([EventLogId])
  INCLUDE ([Id], [EventDate], [SolutionName])

GO

;
CREATE NONCLUSTERED INDEX IX_ExceptionEvents_EventLogId
  ON [dbo].[ExceptionEvents] ([EventLogId])
  INCLUDE ([Id], [EventDate], [SolutionName], [ExceptionType], [ExceptionName], [ExceptionCode], [ExceptionDescription], [ExceptionAction], [DocumentName], [LineNumber], [LineContent])

GO

;
CREATE NONCLUSTERED INDEX IX_FeedPostEvents_EventLogId
  ON [dbo].[FeedPostEvents] ([EventLogId])
  INCLUDE ([Id], [EventDate], [SolutionName], [Comment])

GO

;
CREATE NONCLUSTERED INDEX IX_DebugEvents_EventLogId
  ON [dbo].[DebugEvents] ([EventLogId])
  INCLUDE ([ExecutionAction])

GO

; 
